using MediatR;

namespace TodoList.Application.UseCases.DeleteTodo;

public record DeleteTodoCommand(Guid Id) : IRequest;
