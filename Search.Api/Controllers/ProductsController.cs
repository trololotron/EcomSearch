using Microsoft.AspNetCore.Mvc;
using Search.Application.Products;
using Search.Domain;

namespace Search.Api.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    private readonly SearchProducts _searchProducts;
    private readonly UpdateProduct _updateProduct;

    public ProductsController(
        SearchProducts searchProducts,
        UpdateProduct updateProduct)
    {
        _searchProducts = searchProducts;
        _updateProduct = updateProduct;
    }

    [HttpGet]
    public async Task<ActionResult<ProductSearchResult>> Get(
        [FromQuery] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var products = await _searchProducts.ExecuteAsync(
            request,
            cancellationToken);

        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Update(
    [FromBody] Product product,
    CancellationToken cancellationToken)
    {
        await _updateProduct.ExecuteAsync(
            product,
            cancellationToken);

        return Ok();
    }
}