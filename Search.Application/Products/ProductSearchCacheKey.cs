using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Search.Domain;

namespace Search.Application.Products;

public static class ProductSearchCacheKey
{
    public static string Create(ProductSearchRequest request)
    {
        var json = JsonSerializer.Serialize(request);

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(json));

        var hashString = Convert.ToHexString(hash);

        return $"search:products:{hashString}";
    }
}