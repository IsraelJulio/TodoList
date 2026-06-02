using MediatR;
using TodoList.Application.DTOs;
using TodoList.Domain.Enums;

namespace TodoList.Application.UseCases.UpdateTodo;

public record UpdateTodoCommand(Guid Id, string? Title, string? Description, bool ClearDescription, TodoStatus? Status) : IRequest<TodoItemDto>;
