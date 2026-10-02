using Search.Api.Configuration;
using Search.Api.Endpoints;
using Search.Application.Events;
using Search.Application.Products;
using Search.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services
    .AddProductCatalogGrpcClient(builder.Configuration);

builder.Services.AddElasticsearch(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddMongo(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddScoped<SearchProducts>();
builder.Services.AddScoped<UpdateProduct>();
builder.Services.AddScoped<GetProductById>();

builder.Services.AddScoped<
    IProductUpdatedHandler,
    ProductUpdatedHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "ok"
    }));

app.MapProductCatalogEndpoints();

app.MapControllers();

app.Run();

public partial class Program
{
}