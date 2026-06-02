using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoList.Application.DTOs;
using TodoList.Application.UseCases.CreateTodo;
using TodoList.Application.UseCases.DeleteTodo;
using TodoList.Application.UseCases.GetTodoById;
using TodoList.Application.UseCases.GetTodos;
using TodoList.Application.UseCases.UpdateTodo;

namespace TodoList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TodoItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodosQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodoByIdQuery(id), cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTodoDto dto, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTodoCommand(dto.Title, dto.Description), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTodoDto dto, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateTodoCommand(id, dto.Title, dto.Description, dto.ClearDescription, dto.Status), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTodoCommand(id), cancellationToken);
        return NoContent();
    }
}
