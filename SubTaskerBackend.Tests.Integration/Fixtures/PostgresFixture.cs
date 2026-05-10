using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Data;
using Testcontainers.PostgreSql;

namespace SubTaskerBackend.Tests.Integration.Fixtures
{
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("subtasker_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            await ResetDatabaseAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
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