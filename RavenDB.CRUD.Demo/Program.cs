using System.Text.Json.Serialization;
using RavenDB.CRUD.Demo.Endpoints;
using RavenDB.CRUD.Demo.Indexes;
using RavenDB.CRUD.Demo.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<RavenDBService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

var dbService = app.Services.GetRequiredService<RavenDBService>();
await dbService.ExecuteIndexAsync(new Products_ByCategory());

app.UseHttpsRedirection();

app.MapProductEndpoints();
app.MapRelationshipEndpoints();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();


