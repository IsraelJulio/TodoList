using MediatR;
using TodoList.Application.Exceptions;
using TodoList.Application.Interfaces;
using TodoList.Domain.Entities;
using TodoList.Domain.Interfaces.Repositories;

namespace TodoList.Application.UseCases.DeleteTodo;

public class DeleteTodoHandler(ITodoRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTodoCommand>
{
    public async Task Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        repository.Delete(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
