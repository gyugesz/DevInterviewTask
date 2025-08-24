namespace Todo.Application.Contracts;

public sealed record TodoResponse
(
    int Id,
    string Name,
    string? Description,
    byte Priority,
    DateTime CreatedAt,
    bool IsDone
);
