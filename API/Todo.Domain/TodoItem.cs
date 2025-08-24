namespace Todo.Domain;

public enum Priority : byte { Low = 1, Medium = 2, High = 3 }

public sealed class TodoItem
{
    public int Id { get; init; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Low;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsDone { get; private set; }

    public void MarkDone() => IsDone = true;
}
