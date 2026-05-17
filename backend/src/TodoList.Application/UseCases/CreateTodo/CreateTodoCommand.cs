using MediatR;
using TodoList.Application.DTOs;

namespace TodoList.Application.UseCases.CreateTodo;

public record CreateTodoCommand(string Title, string? Description) : IRequest<TodoItemDto>;
