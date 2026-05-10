using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SubTaskerBackend.Data;
using Testcontainers.PostgreSql;

namespace SubTaskerBackend.Tests.Api.Fixtures
{
    public class ApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("subtasker_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public string ConnectionString => _container.GetConnectionString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                Dictionary<string, string?> configValues = new()
                {
                    ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                    ["Auth:Issuer"] = "https://test-issuer",
                    ["Auth:Audience"] = "https://test-audience",
                    ["Auth:SigningKey"] = "supersecrettestkey123456789012345678",
                };

                configBuilder.AddInMemoryCollection(configValues);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<SubTaskerEfCoreDbContext>));

                services.AddDbContext<SubTaskerEfCoreDbContext>(options =>
                {
                    options.UseNpgsql(ConnectionString);
                });
            });
        }

        Task IAsyncLifetime.InitializeAsync()
        {
            return InitializeFactoryAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync().AsTask();
        }

        private async Task InitializeFactoryAsync()
        {
            await _container.StartAsync();
            await ResetDatabaseAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            await _container.DisposeAsync();
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public SubTaskerEfCoreDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<SubTaskerEfCoreDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            return new SubTaskerEfCoreDbContext(options);
        }

        public async Task ResetDatabaseAsync()
        {
            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
    }
}