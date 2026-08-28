namespace Search.Infrastructure.Configuration;

public sealed class ProductSearchOptions
{
    public const string SectionName = "ProductSearch";

    public double NameBoost { get; init; } = 3;

    public double DescriptionBoost { get; init; } = 1;

    public double CategoryBoost { get; init; } = 1;
}