using AutoMapper;
using MediatR;
using TodoList.Application.DTOs;
using TodoList.Domain.Interfaces.Repositories;

namespace TodoList.Application.UseCases.GetTodos;

public class GetTodosHandler(ITodoRepository repository, IMapper mapper)
    : IRequestHandler<GetTodosQuery, IEnumerable<TodoItemDto>>
{
    public async Task<IEnumerable<TodoItemDto>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        var items = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<TodoItemDto>>(items);
    }
}
