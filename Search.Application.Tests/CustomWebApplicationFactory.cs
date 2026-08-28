using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Search.Application.Abstractions;
using Search.Application.Tests.Fakes;

namespace Search.Application.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {

            services.RemoveAll<IProductSearchRepository>();

            services.AddScoped<IProductSearchRepository, FakeProductSearchRepository>();
        });
    }
}