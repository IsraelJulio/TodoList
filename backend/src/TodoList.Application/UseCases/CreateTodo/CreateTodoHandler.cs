using AutoMapper;
using MediatR;
using TodoList.Application.DTOs;
using TodoList.Application.Interfaces;
using TodoList.Domain.Entities;
using TodoList.Domain.Interfaces.Repositories;
using TodoList.Domain.ValueObjects;

namespace TodoList.Application.UseCases.CreateTodo;

public class CreateTodoHandler(ITodoRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateTodoCommand, TodoItemDto>
{
    public async Task<TodoItemDto> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var title = Title.Create(request.Title);
        var item = TodoItem.Create(title, request.Description);

        await repository.AddAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<TodoItemDto>(item);
    }
}
