using AutoMapper;
using MediatR;
using TodoList.Application.DTOs;
using TodoList.Application.Exceptions;
using TodoList.Domain.Entities;
using TodoList.Domain.Interfaces.Repositories;

namespace TodoList.Application.UseCases.GetTodoById;

public class GetTodoByIdHandler(ITodoRepository repository, IMapper mapper)
    : IRequestHandler<GetTodoByIdQuery, TodoItemDto>
{
    public async Task<TodoItemDto> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        return mapper.Map<TodoItemDto>(item);
    }
}
