using AutoMapper;
using MediatR;
using TodoList.Application.DTOs;
using TodoList.Application.Exceptions;
using TodoList.Application.Interfaces;
using TodoList.Domain.Interfaces.Repositories;
using TodoList.Domain.ValueObjects;

namespace TodoList.Application.UseCases.UpdateTodo;

public class UpdateTodoHandler(ITodoRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateTodoCommand, TodoItemDto>
{
    public async Task<TodoItemDto> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("TodoItem", request.Id);

        if (request.Title is not null)
            item.UpdateTitle(Title.Create(request.Title));

        if (request.ClearDescription || request.Description is not null)
            item.UpdateDescription(request.ClearDescription ? null : request.Description);

        if (request.Status is not null)
        {
            if (!Enum.IsDefined(request.Status.Value))
                throw new ArgumentException($"Status inválido: {(int)request.Status.Value}", nameof(request.Status));

            item.ChangeStatus(request.Status.Value);
        }

        repository.Update(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<TodoItemDto>(item);
    }
}
