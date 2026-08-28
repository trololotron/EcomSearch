using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Search.Application.Products;

namespace Search.Application.Tests.Products;

public sealed class ProductsApiTests
{
    [Fact]
    public async Task GetProducts_ReturnsProducts()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/products?query=iphone");

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ProductSearchResult>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
        Assert.Equal(129999, result.Items[0].Price);
        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetProducts_WhenNothingFound_ReturnsEmptyResult()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/products?query=samsung");

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ProductSearchResult>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }
}