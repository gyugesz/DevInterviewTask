using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Todo.Application.Contracts;
using Todo.Domain;
using Todo.Infrastructure;

namespace Todo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TodoController : ControllerBase
{
    private readonly TodoDbContext _db;
    public TodoController(TodoDbContext db) => _db = db;

    [HttpPost]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTodoRequest req, CancellationToken ct)
    {
        var entity = new TodoItem
        {
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description!.Trim(),
            Priority = (Priority)req.Priority
        };
        _db.TodoItems.Add(entity);
        await _db.SaveChangesAsync(ct);

        var dto = new TodoResponse(entity.Id, entity.Name, entity.Description, (byte)entity.Priority, entity.CreatedAt, entity.IsDone);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var t = await _db.TodoItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        return Ok(new TodoResponse(t.Id, t.Name, t.Description, (byte)t.Priority, t.CreatedAt, t.IsDone));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TodoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool undoneOnly = true, CancellationToken ct = default)
    {
        var q = _db.TodoItems.AsNoTracking().AsQueryable();
        if (undoneOnly) q = q.Where(x => !x.IsDone);

        var items = await q
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new TodoResponse(x.Id, x.Name, x.Description, (byte)x.Priority, x.CreatedAt, x.IsDone))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPatch("{id:int}/done")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkDone([FromRoute] int id, CancellationToken ct)
    {
        var entity = await _db.TodoItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!entity.IsDone)
        {
            entity.MarkDone();
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }
}
