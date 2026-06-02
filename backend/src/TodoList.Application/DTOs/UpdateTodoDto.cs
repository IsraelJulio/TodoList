using TodoList.Domain.Enums;

namespace TodoList.Application.DTOs;

public record UpdateTodoDto(string? Title, string? Description, bool ClearDescription, TodoStatus? Status);
