using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Options;
using Search.Application.Abstractions;
using Search.Application.Products;
using Search.Domain;
using Search.Infrastructure.Configuration;
using System.Linq.Expressions;

namespace Search.Infrastructure.Search;

public sealed class ElasticsearchProductSearchRepository
    : IProductSearchRepository
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _elasticsearchOptions;
    private readonly ProductSearchOptions _searchOptions;

    public ElasticsearchProductSearchRepository(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> elasticsearchOptions,
        IOptions<ProductSearchOptions> searchOptions)
    {
        _client = client;
        _elasticsearchOptions = elasticsearchOptions.Value;
        _searchOptions = searchOptions.Value;
    }

    public async Task<ProductSearchResult> SearchAsync(
       ProductSearchRequest request,
       CancellationToken cancellationToken = default)
    {
        var from = (request.Page - 1) * request.PageSize;

        var response = await _client.SearchAsync<Product>(
            s => s
                .Indices(_elasticsearchOptions.ProductIndex).Sort(GetSortOptions(request.Sort))
                .From(from)
                .Size(request.PageSize)
                .Query(q => q.Bool(b => b
                    .Must(BuildSearchQuery(request))
                    .Filter(GetFilters(request)))),
            cancellationToken);

        return new ProductSearchResult
        {
            Items = response.Documents.ToList(),
            Total = response.Total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
    private Query BuildSearchQuery(ProductSearchRequest request)
    {
        return new MultiMatchQuery
        {
            Fields = GetSearchFields(),
            Query = request.Query
        };
    }

    private Fields GetSearchFields()
    {
        return Fields.FromField(
                new Field(
                    (Expression<Func<Product, object?>>)(p => p.Name),
                    _searchOptions.NameBoost))
            .And(
                new Field(
                    (Expression<Func<Product, object?>>)(p => p.Description),
                    _searchOptions.DescriptionBoost));
    }

    private ICollection<SortOptions>? GetSortOptions(ProductSort sort)
    {
        return sort switch
        {
            ProductSort.Relevance => null,

            ProductSort.PriceAsc =>
            [
                new SortOptions
            {
                Field = new FieldSort(
                    (Field)(Expression<Func<Product, object?>>)(p => p.Price))
                {
                    Order = SortOrder.Asc
                }
            }
            ],

            ProductSort.PriceDesc =>
            [
                new SortOptions
            {
                Field = new FieldSort(
                    (Field)(Expression<Func<Product, object?>>)(p => p.Price))
                {
                    Order = SortOrder.Desc
                }
            }
            ],

            _ => null
        };
    }

    private ICollection<Query> GetFilters(ProductSearchRequest request)
    {
        var filters = new List<Query>();

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            filters.Add(
                  new TermQuery(
                      new Field((Expression<Func<Product, object?>>)(p => p.Category)),
                      request.Category));
        }

        if (request.MinPrice.HasValue)
        {
            filters.Add(
            new NumberRangeQuery(
                (Field)(Expression<Func<Product, object?>>)(p => p.Price))
            {
                Gte = (Number)request.MinPrice.Value
            });
        }

        if (request.MaxPrice.HasValue)
        {
            filters.Add(
            new NumberRangeQuery(
                (Field)(Expression<Func<Product, object?>>)(p => p.Price))
            {
                Lte = (Number)request.MaxPrice.Value
            });
        }

        return filters;
    }
}