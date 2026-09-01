using Search.Application.Products;
using Search.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddElasticsearch(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<SearchProducts>();

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
{
    return Results.Ok(new
    {
        status = "ok"
    });
});

app.MapControllers();

app.Run();

public partial class Program
{
}
