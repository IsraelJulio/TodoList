using MediatR;
using TodoList.Application.DTOs;

namespace TodoList.Application.UseCases.GetTodos;

public record GetTodosQuery : IRequest<IEnumerable<TodoItemDto>>;
