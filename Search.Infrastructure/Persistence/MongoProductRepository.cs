using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Search.Application.Abstractions;
using Search.Domain;
using Search.Infrastructure.Configuration;

namespace Search.Infrastructure.Persistence;

public sealed class MongoProductRepository
    : IProductRepository
{
    private readonly IMongoCollection<Product> _products;

    public MongoProductRepository(
        IMongoClient mongoClient,
        IOptions<MongoOptions> options)
    {
        var database = mongoClient.GetDatabase(
            options.Value.DatabaseName);

        _products = database.GetCollection<Product>(
            options.Value.ProductsCollection);
    }

    public async Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Eq(
            x => x.Id,
            product.Id);

        await _products.ReplaceOneAsync(
            filter,
            product,
            new ReplaceOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        return await _products
            .Find(product => product.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}