using MediatR;
using TodoList.Application.DTOs;

namespace TodoList.Application.UseCases.GetTodoById;

public record GetTodoByIdQuery(Guid Id) : IRequest<TodoItemDto?>;
