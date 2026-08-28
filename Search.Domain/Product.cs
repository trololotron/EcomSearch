namespace Search.Domain;

public sealed class Product
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string Category { get; init; } = string.Empty;
}