using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
		// This will ensure that enums are serialized as their string representation in JSON
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

app.MapGet("/", () => "Hello World!");

app.Run();
