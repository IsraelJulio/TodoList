namespace TodoList.Application.DTOs;

public record TodoItemDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
