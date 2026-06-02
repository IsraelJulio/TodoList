using TodoList.Domain.Enums;
using TodoList.Domain.ValueObjects;

namespace TodoList.Domain.Entities;

public class TodoItem
{
    public Guid Id { get; private set; }
    public Title Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TodoStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TodoItem() { }  // EF Core

    public static TodoItem Create(Title title, string? description = null)
    {
        return new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Status = TodoStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateTitle(Title title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TodoStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException($"Status inválido: {(int)status}", nameof(status));

        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
