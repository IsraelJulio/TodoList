# Code Review: TodoList API — Domain, Application, and API Layers

**Reviewed:** 2026-06-02  
**Depth:** Standard (cross-file analysis included)  
**Files Reviewed:** 19  
**Status:** Issues Found

---

## Summary

The overall structure is solid: Clean Architecture dependency directions are respected, CQRS is properly separated between Commands and Queries, domain entities are never exposed directly from the API (DTOs are used throughout), and business validation is correctly placed in the Domain layer via the `Title` value object. MediatR and AutoMapper are wired correctly.

However, there are meaningful defects across all three layers. The most critical is a **silent data-integrity bug** in `UpdateTodoHandler` — clearing a description is impossible due to a null-check guard that was intended as "field not provided" but also blocks intentional nullification. Several medium-severity issues follow: unvalidated enum input reaches domain state transitions, `TodoStatus` is serialized as a raw string with no contract guarantee, `GetTodoByIdQuery` has a non-nullable return type that conflicts with `NotFoundException` semantics, and the controller imports `UpdateTodoDto` but never declares a using statement for it (compilation dependency on wildcard namespace import). There are also lower-severity architecture and quality concerns documented below.

---

## HIGH Severity

### H-01: Description Cannot Be Cleared — Silent Data Loss

**File:** `backend/src/TodoList.Application/UseCases/UpdateTodo/UpdateTodoHandler.cs:23-24`

**Issue:** The guard `if (request.Description is not null)` treats `null` as "field not provided" and skips the update. This means a client that sends `{ "description": null }` to explicitly clear a description will have that change silently ignored. There is no way for a caller to ever remove a description once it has been set. This is a correctness bug that causes silent data loss.

The same asymmetry does not apply to `Title` (since Title is required and cannot be null), but the `Description` field is optional and PATCH-style semantics require a way to clear it.

**Fix:** Introduce a discriminated "patch field" pattern using a wrapper, or accept a dedicated flag. The simplest correct fix without changing the DTO structure is to treat absence differently from null — for example, using `Optional<T>` or by replacing the nullable parameter with a dedicated sentinel:

```csharp
// Option A: use a wrapper type in the command to distinguish "not provided" from "clear"
public record UpdateTodoCommand(Guid Id, string? Title, Optional<string?> Description, TodoStatus? Status)

// Option B (simpler, breaking change to contract): use an explicit boolean flag
public record UpdateTodoCommand(Guid Id, string? Title, string? Description, bool ClearDescription, TodoStatus? Status)

// In handler:
if (request.ClearDescription || request.Description is not null)
    item.UpdateDescription(request.Description);
```

---

### H-02: Unvalidated Enum Value Accepted for Status Transitions

**File:** `backend/src/TodoList.Application/UseCases/UpdateTodo/UpdateTodoHandler.cs:26-27`  
**Also:** `backend/src/TodoList.Application/DTOs/UpdateTodoDto.cs:5`

**Issue:** `UpdateTodoCommand` accepts `TodoStatus? Status` and calls `item.ChangeStatus(request.Status.Value)` without any validation. The `TodoStatus` enum has values `Pending=0`, `InProgress=1`, `Completed=2`. If a client sends an integer outside this range (e.g. `"status": 99`), the deserialized `TodoStatus` value will be `(TodoStatus)99`, which is not a named enum member. This invalid value is passed directly into `ChangeStatus()` on the domain entity with no guard. The domain layer performs no validation either.

Additionally, there is no business-rule guard on status transitions. A `Completed` item can be moved back to `Pending` without restriction, which may or may not be intended, but there is no documentation of the intent.

**Fix:**

```csharp
// In UpdateTodoHandler, before calling ChangeStatus:
if (request.Status is not null)
{
    if (!Enum.IsDefined(request.Status.Value))
        throw new ArgumentException($"Status inválido: {request.Status.Value}");

    item.ChangeStatus(request.Status.Value);
}
```

For transition rules, if they are intentional business constraints, add a guard in `TodoItem.ChangeStatus()` in the Domain layer:

```csharp
public void ChangeStatus(TodoStatus newStatus)
{
    if (Status == TodoStatus.Completed && newStatus != TodoStatus.Completed)
        throw new InvalidOperationException("Itens concluídos não podem ser reabertos.");
    Status = newStatus;
    UpdatedAt = DateTime.UtcNow;
}
```

---

## MEDIUM Severity

### M-01: `GetTodoByIdQuery` Return Type Does Not Express Nullability — Misleads Callers

**File:** `backend/src/TodoList.Application/UseCases/GetTodoById/GetTodoByIdQuery.cs:6`  
**Also:** `backend/src/TodoList.Application/UseCases/GetTodoById/GetTodoByIdHandler.cs:13`

**Issue:** The query is declared as `IRequest<TodoItemDto>` with a non-nullable return type. The handler throws `NotFoundException` when the entity is not found rather than returning null. This is not wrong per se, but the contract is incomplete: the `ProducesResponseType(StatusCodes.Status404NotFound)` attribute on the controller action (controller line 26) relies entirely on the middleware catching the exception. If a caller uses this handler outside the middleware context (e.g., from another handler or a background service), they receive an unhandled exception with no type-safe signal.

A more robust pattern is to use a result type such as `IRequest<TodoItemDto?>` or a `Result<T>` wrapper, making the not-found case explicit in the type signature.

**Fix:**

```csharp
// Option A: return nullable and let the caller (controller/handler) decide response shape
public record GetTodoByIdQuery(Guid Id) : IRequest<TodoItemDto?>;

// Handler:
public async Task<TodoItemDto?> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
{
    var item = await repository.GetByIdAsync(request.Id, cancellationToken);
    return item is null ? null : mapper.Map<TodoItemDto>(item);
}

// Controller:
var result = await sender.Send(new GetTodoByIdQuery(id), cancellationToken);
if (result is null) return NotFound();
return Ok(result);
```

---

### M-02: `TodoStatus` Serialized as String — No API Contract Stability

**File:** `backend/src/TodoList.Application/Mappings/TodoProfile.cs:13`  
**Also:** `backend/src/TodoList.Application/DTOs/TodoItemDto.cs:8`

**Issue:** `Status` in `TodoItemDto` is typed as `string`, and `TodoProfile` maps it with `src.Status.ToString()`. The `ToString()` on an enum with no custom serialization options returns the member name (e.g. `"InProgress"`). This is fragile because:

1. Any rename of the enum member is a breaking API change — there is no `[JsonPropertyName]` or `[EnumMember]` annotation protecting the string value.
2. `UpdateTodoDto` takes `TodoStatus?` as an enum — so the API accepts an enum integer or string (depending on serializer config) on write but always returns a string on read. This asymmetry between the write DTO (enum) and the read DTO (string) is a contract inconsistency.

**Fix:** Either keep the status as the enum in `TodoItemDto` and configure the serializer to emit string names:

```csharp
// In Program.cs:
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// In TodoItemDto: change Status type to TodoStatus
public record TodoItemDto(Guid Id, string Title, string? Description, TodoStatus Status, DateTime CreatedAt, DateTime UpdatedAt);

// Remove the explicit mapping in TodoProfile:
// .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
// AutoMapper will map TodoStatus -> TodoStatus automatically
```

---

### M-03: `UpdateTodoDto` Not Imported via Using in Controller

**File:** `backend/src/TodoList.API/Controllers/TodosController.cs:3,46`

**Issue:** The controller uses `UpdateTodoDto` on line 46 but the `using` directives only include `TodoList.Application.DTOs` (line 3). `UpdateTodoDto` is defined in that namespace, so the project compiles. However, `CreateTodoDto` is in the same namespace and is used directly. The issue is that the review was supplied with a file list that excluded `UpdateTodoDto.cs` from the review scope — this suggests the DTO was added after initial review and the controller was not audited for it. More critically: `UpdateTodoDto` lives in the **Application** layer (`TodoList.Application.DTOs`), yet it is used as a `[FromBody]` binding model for the API. This couples the API input contract directly to an Application-layer DTO.

The project rule states "Do not expose domain entities directly in APIs (use DTOs)" — the same principle applies in reverse: the API input model (what the HTTP request body deserializes into) should ideally be an API-layer model, not an Application-layer DTO. If the Application-layer DTO changes shape for internal reasons, it is a forced breaking change to the API contract.

**Fix:** Define API-layer request models in the API project and map them to commands:

```csharp
// In TodoList.API/Models/UpdateTodoRequest.cs:
public record UpdateTodoRequest(string? Title, string? Description, TodoStatus? Status);

// In controller:
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTodoRequest request, ...)
{
    var result = await sender.Send(
        new UpdateTodoCommand(id, request.Title, request.Description, request.Status), cancellationToken);
    return Ok(result);
}
```

The same observation applies to `CreateTodoDto` — it is used as both the API input model and an Application DTO.

---

### M-04: `ExceptionHandlingMiddleware` Catches `ArgumentException` But Not Derived Types with Different Semantics

**File:** `backend/src/TodoList.API/Middleware/ExceptionHandlingMiddleware.cs:21-23`

**Issue:** The middleware catches `ArgumentException` and returns HTTP 400. `ArgumentNullException` is a subclass of `ArgumentException`, so null-argument errors from the framework or from third-party dependencies will also produce 400 responses. More significantly, `Title.Create()` throws `ArgumentException` — but if any future code in the pipeline throws an `ArgumentException` for a reason unrelated to user input (a programming error, e.g. passing a wrong argument to an internal utility), it will silently appear as a 400 to the client instead of a 500. This masks infrastructure bugs as client errors.

**Fix:** Introduce a dedicated domain validation exception (e.g. `DomainValidationException`) that `Title.Create()` and other domain guards throw, and map only that to 400. Reserve `ArgumentException` catch for cases where you are confident about the source.

```csharp
// In Domain:
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message) { }
}

// In Title.Create():
if (string.IsNullOrWhiteSpace(value))
    throw new DomainValidationException("O título não pode ser vazio.");

// In middleware — only map DomainValidationException to 400:
catch (DomainValidationException ex)
{
    await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message);
}
```

---

### M-05: `TodoItem.ChangeStatus` Accepts Any Integer Cast to `TodoStatus` — No Domain Guard

**File:** `backend/src/TodoList.Domain/Entities/TodoItem.cs:42-46`

**Issue:** `ChangeStatus(TodoStatus status)` applies the new status without validating that it is a defined enum value. This is the domain-side of H-02. Even if the Application layer adds validation (as recommended in H-02), the domain entity itself has no invariant protecting it. A domain entity should enforce its own invariants and not rely on callers to validate inputs before calling its methods.

**Fix:**

```csharp
public void ChangeStatus(TodoStatus status)
{
    if (!Enum.IsDefined(status))
        throw new ArgumentException($"Status inválido: {status}", nameof(status));

    Status = status;
    UpdatedAt = DateTime.UtcNow;
}
```

---

### M-06: `GetTodoByIdHandler` Imports Unused `TodoList.Domain.Entities` Namespace

**File:** `backend/src/TodoList.Application/UseCases/GetTodoById/GetTodoByIdHandler.cs:5`

**Issue:** `using TodoList.Domain.Entities;` is imported but `TodoItem` is only referenced as a string literal in `nameof(TodoItem)` inside the `NotFoundException` constructor call. The actual type `TodoItem` is never used for anything other than this name lookup. This is a minor coupling issue — the Application handler depends on the Domain entity class only to extract its name. A cleaner approach avoids the import.

**Fix:**

```csharp
// Replace:
?? throw new NotFoundException(nameof(TodoItem), request.Id);

// With:
?? throw new NotFoundException("TodoItem", request.Id);

// Or define a constant in the query/command:
?? throw new NotFoundException(nameof(GetTodoByIdQuery), request.Id);
```

The same pattern appears in `UpdateTodoHandler.cs:7` and `DeleteTodoHandler.cs:4`.

---

## LOW Severity

### L-01: Anemic Update — All Three Fields Optional with No Validation That At Least One Is Provided

**File:** `backend/src/TodoList.Application/UseCases/UpdateTodo/UpdateTodoHandler.cs:20-27`  
**Also:** `backend/src/TodoList.Application/UseCases/UpdateTodo/UpdateTodoCommand.cs:7`

**Issue:** `UpdateTodoCommand` accepts all three fields as optional (`string? Title`, `string? Description`, `TodoStatus? Status`). If a client sends an empty JSON body `{}`, all three fields will be null, the handler will skip all three update branches, call `repository.Update(item)` and `unitOfWork.SaveChangesAsync()`, performing a no-op database round-trip while returning HTTP 200 with the unchanged entity. There is no guard requiring at least one field to be present.

**Fix:**

```csharp
// In UpdateTodoHandler, before any update:
if (request.Title is null && request.Description is null && request.Status is null)
    throw new ArgumentException("Pelo menos um campo deve ser fornecido para atualização.");
```

---

### L-02: `UpdatedAt` Updated Even When Title.Create Throws — Inconsistent State Possible

**File:** `backend/src/TodoList.Application/UseCases/UpdateTodo/UpdateTodoHandler.cs:21`

**Issue:** If `request.Title` is provided but fails `Title.Create()` validation (e.g. exceeds 200 chars), `ArgumentException` is thrown before the description or status are applied. This is correct. However, if title succeeds but the enum validation were ever added and failed on status, `UpdatedAt` will have been mutated on the entity in memory by `UpdateTitle()` before the status check throws. The entity is in a partially-updated, dirty in-memory state when the exception propagates. Because `unitOfWork.SaveChangesAsync()` is never called in the exception path, this does not persist — but it is a fragile pattern.

**Fix:** Consider validating all inputs before applying any mutations:

```csharp
// Validate first
var newTitle = request.Title is not null ? Title.Create(request.Title) : null;
if (request.Status is not null && !Enum.IsDefined(request.Status.Value))
    throw new ArgumentException("Status inválido.");

// Then apply
if (newTitle is not null) item.UpdateTitle(newTitle);
if (request.Description is not null) item.UpdateDescription(request.Description);
if (request.Status is not null) item.ChangeStatus(request.Status.Value);
```

---

### L-03: `TodoItemDto.Status` Typed as `string` but `UpdateTodoDto.Status` Typed as `TodoStatus?`

**File:** `backend/src/TodoList.Application/DTOs/TodoItemDto.cs:8`  
**Also:** `backend/src/TodoList.Application/DTOs/UpdateTodoDto.cs:5`

**Issue:** This is the type asymmetry elaborated in M-02. Reads return `string`, writes accept `TodoStatus?`. A frontend client consuming Swagger/OpenAPI will see different types for the same conceptual field depending on whether it is reading or writing. This is a maintenance and client-SDK generation hazard.

---

### L-04: `Title` Value Object Does Not Implement `IEquatable<Title>` Explicitly

**File:** `backend/src/TodoList.Domain/ValueObjects/Title.cs:24-27`

**Issue:** `Title` overrides `Equals(object?)` and `GetHashCode()` but does not implement `IEquatable<Title>`. This means equality via `==` operator on two `Title` instances uses reference equality (the default), not the value-based equality defined in `Equals()`. A developer writing `if (item.Title == someOtherTitle)` will get reference equality silently.

**Fix:**

```csharp
public sealed class Title : IEquatable<Title>
{
    // ...existing code...

    public bool Equals(Title? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Title other && Equals(other);

    public static bool operator ==(Title? left, Title? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Title? left, Title? right) => !(left == right);
}
```

---

### L-05: `ExceptionHandlingMiddleware` Does Not Check `Response.HasStarted` Before Writing

**File:** `backend/src/TodoList.API/Middleware/ExceptionHandlingMiddleware.cs:34-48`

**Issue:** `WriteProblemDetails` sets `context.Response.StatusCode` and writes to the response body without checking `context.Response.HasStarted`. If any middleware earlier in the pipeline has already begun writing a response (e.g., streaming), setting the status code or writing body content will throw an `InvalidOperationException` ("Headers are read-only, response has already started"). This would trigger the catch block in a nested way — but since the exception handler itself is the outermost middleware, it would crash silently or produce a partial response.

**Fix:**

```csharp
private static async Task WriteProblemDetails(HttpContext context, int status, string title, string detail)
{
    if (context.Response.HasStarted)
        return; // Cannot modify response after it has started

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    // ...rest of method
}
```

---

### L-06: `Program.cs` — No CORS Configuration

**File:** `backend/src/TodoList.API/Program.cs`

**Issue:** The API is intended to serve an Angular 19 frontend running on a different port. There is no CORS policy configured. In development the Angular dev server (typically port 4200) will be blocked by browser CORS enforcement when calling the API. The omission means the frontend cannot call the backend without a proxy workaround or browser override.

**Fix:**

```csharp
// In Program.cs, before builder.Build():
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// After app.Build():
app.UseCors("AllowAngular");
```

---

### L-07: `TodoStatus` Enum — No Explicit Integer Values

**File:** `backend/src/TodoList.Domain/Enums/TodoStatus.cs:4-8`

**Issue:** The enum members have no explicit values (`Pending = 0`, `InProgress = 1`, `Completed = 2` are implicit). While this works, adding or reordering a member in the future shifts all subsequent values, which can corrupt persisted integer data in PostgreSQL if stored as integers. Explicit values are a defensive convention for enums backed by a database column.

**Fix:**

```csharp
public enum TodoStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2
}
```

---

### L-08: `GetTodosQuery` Is a Record With No Members — Unnecessary `record` Keyword

**File:** `backend/src/TodoList.Application/UseCases/GetTodos/GetTodosQuery.cs:6`

**Issue:** `public record GetTodosQuery : IRequest<IEnumerable<TodoItemDto>>;` has no properties. Using `record` here provides no benefit over `class` (no structural equality needed, no positional parameters). It is not wrong, but it is inconsistent with intent — empty records are unusual and can confuse readers. A plain `class` or a `struct` would be more idiomatic for a parameterless query object.

**Fix:**

```csharp
public class GetTodosQuery : IRequest<IEnumerable<TodoItemDto>> { }
```

---

## Summary Table

| ID   | Severity | File(s)                                      | Issue                                                                 |
|------|----------|----------------------------------------------|-----------------------------------------------------------------------|
| H-01 | HIGH     | UpdateTodoHandler.cs:23-24                   | Description cannot be cleared — null treated as "not provided"        |
| H-02 | HIGH     | UpdateTodoHandler.cs:26-27, UpdateTodoDto.cs | Unvalidated enum integer reaches ChangeStatus with no domain guard    |
| M-01 | MEDIUM   | GetTodoByIdQuery.cs, GetTodoByIdHandler.cs   | Non-nullable return type hides NotFoundException contract             |
| M-02 | MEDIUM   | TodoProfile.cs:13, TodoItemDto.cs:8          | Status serialized as raw string — no contract stability               |
| M-03 | MEDIUM   | TodosController.cs:46                        | UpdateTodoDto / CreateTodoDto used as API input models (layer leak)   |
| M-04 | MEDIUM   | ExceptionHandlingMiddleware.cs:21-23         | ArgumentException catch masks internal bugs as 400 client errors      |
| M-05 | MEDIUM   | TodoItem.cs:42-46                            | ChangeStatus has no domain-level enum validity guard                  |
| M-06 | MEDIUM   | GetTodoByIdHandler.cs:5                      | Unused Domain entity import — nameof trick                            |
| L-01 | LOW      | UpdateTodoHandler.cs:20-27                   | Empty update body causes no-op DB round-trip with HTTP 200            |
| L-02 | LOW      | UpdateTodoHandler.cs:21                      | Partial mutation before validation throw leaves dirty in-memory state |
| L-03 | LOW      | TodoItemDto.cs:8, UpdateTodoDto.cs:5         | Read DTO uses string status, write DTO uses enum — type asymmetry     |
| L-04 | LOW      | Title.cs:24-27                               | Title missing IEquatable<Title> — == operator uses reference equality |
| L-05 | LOW      | ExceptionHandlingMiddleware.cs:34            | WriteProblemDetails does not check Response.HasStarted                |
| L-06 | LOW      | Program.cs                                   | No CORS policy — Angular frontend will be blocked                     |
| L-07 | LOW      | TodoStatus.cs:4-8                            | Enum members lack explicit integer values — reorder risk              |
| L-08 | LOW      | GetTodosQuery.cs:6                           | Parameterless record where class is more idiomatic                    |

---

_Reviewed: 2026-06-02_  
_Reviewer: Claude Code (adversarial review)_  
_Depth: Standard + cross-file analysis_
