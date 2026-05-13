using Microsoft.AspNetCore.Identity;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;

namespace SubTaskerBackend.Tests.Api
{
    public class UserApiTest : IClassFixture<ApiTestFactory>, IAsyncLifetime
    {
        private readonly ApiTestFactory _factory;

        private readonly HttpClient _client;

        public UserApiTest(ApiTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            _client.Dispose();
            return Task.CompletedTask;
        }
    }
}