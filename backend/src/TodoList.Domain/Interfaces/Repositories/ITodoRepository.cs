using TodoList.Domain.Entities;

namespace TodoList.Domain.Interfaces.Repositories;

public interface ITodoRepository
{
    Task<IEnumerable<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);
    void Update(TodoItem item);
    void Delete(TodoItem item);
}
