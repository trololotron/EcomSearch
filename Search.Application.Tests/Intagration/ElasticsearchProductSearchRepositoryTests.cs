using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using Search.Application.Products;
using Search.Domain;
using Search.Infrastructure.Configuration;
using Search.Infrastructure.Search;

namespace Search.Application.Tests.Integration;

public sealed class ElasticsearchProductSearchRepositoryTests
{
    private readonly ElasticsearchProductSearchRepository _repository;

    public ElasticsearchProductSearchRepositoryTests()
    {
        var client = new ElasticsearchClient(
            new ElasticsearchClientSettings(
                new Uri("http://localhost:9200")));

        var elasticsearchOptions = Options.Create(
            new ElasticsearchOptions
            {
                Url = "http://localhost:9200",
                ProductIndex = "products"
            });

        var searchOptions = Options.Create(
            new ProductSearchOptions
            {
                NameBoost = 1,
                DescriptionBoost = 1,
                CategoryBoost = 1
            });

        _repository = new ElasticsearchProductSearchRepository(
            client,
            elasticsearchOptions,
            searchOptions);
    }

    [Fact]
    public async Task SearchAsync_SearchesProductsInElasticsearch()
    {
        var request = new ProductSearchRequest
        {
            Query = "apple"
        };

        var result = await _repository.SearchAsync(request);

        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task SearchAsync_SortsByPriceAscending()
    {
        var request = new ProductSearchRequest
        {
            Query = "apple",
            Sort = ProductSort.PriceAsc
        };

        var result = await _repository.SearchAsync(request);

        Assert.Equal("Apple Watch", result.Items[0].Name);
        Assert.Equal("iPhone 17 Pro", result.Items[1].Name);
        Assert.Equal("MacBook Pro", result.Items[2].Name);
    }

    [Fact]
    public async Task SearchAsync_FiltersByPrice()
    {
        var request = new ProductSearchRequest
        {
            Query = "apple",
            MinPrice = 100000
        };

        var result = await _repository.SearchAsync(request);

        Assert.Equal(2, result.Total);

        Assert.All(
            result.Items,
            product => Assert.True(product.Price >= 100000));
    }

    [Fact]
    public async Task SearchAsync_FiltersByCategory()
    {
        var request = new ProductSearchRequest
        {
            Query = "apple",
            Category = "Смартфоны"
        };

        var result = await _repository.SearchAsync(request);

        Assert.Equal(1, result.Total);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
    }

    [Fact]
    public async Task SearchAsync_PaginatesResults()
    {
        var request = new ProductSearchRequest
        {
            Query = "apple",
            Sort = ProductSort.PriceAsc,
            Page = 2,
            PageSize = 1
        };

        var result = await _repository.SearchAsync(request);

        Assert.Equal(3, result.Total);
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
    }
}