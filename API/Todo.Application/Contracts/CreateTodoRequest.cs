namespace Todo.Application.Contracts;

public sealed record CreateTodoRequest(string Name, string? Description, byte Priority);
