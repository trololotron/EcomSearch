namespace Search.Application.Events;

public sealed record ProductUpdated(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    string Category,
    DateTimeOffset OccurredAt);