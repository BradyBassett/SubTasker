using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SubTaskerBackend.Data;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Services;
using SubTaskerBackend.Interfaces;
using Microsoft.AspNetCore.Identity;
using SubTaskerBackend.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Auth:Issuer"] ?? throw new InvalidOperationException("Auth:Issuer is not configured."),
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth:Audience"] ?? throw new InvalidOperationException("Auth:Audience is not configured."),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Auth:SigningKey"] ?? throw new InvalidOperationException("Auth:SigningKey is not configured."))),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddHttpContextAccessor();

WebApplication app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Hello World!");
// TODO - remove this once we have actual endpoints
app.MapGet("/api/weather", () =>
{
    string[] summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5)
        .Select(index => new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 56),
            summaries[Random.Shared.Next(summaries.Length)]))
        .ToArray();

    return Results.Ok(forecast);
})
.WithName("GetTestWeather")
.WithTags("Test")
.RequireAuthorization();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
