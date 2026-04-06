using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddDbContext<SubTaskerEfCoreDbContext>(options =>
    {
        // Configure the DbContext to use PostgreSQL with the connection string from configuration (Stored as a secret in development)
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    })
    .AddControllers()
    .AddJsonOptions(options =>
    {
		// This will ensure that enums are serialized as their string representation in JSON
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
