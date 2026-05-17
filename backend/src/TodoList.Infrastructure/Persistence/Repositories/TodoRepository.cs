using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Entities;
using TodoList.Domain.Interfaces.Repositories;

namespace TodoList.Infrastructure.Persistence.Repositories;

public class TodoRepository(AppDbContext context) : ITodoRepository
{
    public async Task<IEnumerable<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.TodoItems.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.TodoItems.FindAsync([id], cancellationToken);

    public async Task AddAsync(TodoItem item, CancellationToken cancellationToken = default)
        => await context.TodoItems.AddAsync(item, cancellationToken);

    public void Update(TodoItem item)
        => context.TodoItems.Update(item);

    public void Delete(TodoItem item)
        => context.TodoItems.Remove(item);
}
