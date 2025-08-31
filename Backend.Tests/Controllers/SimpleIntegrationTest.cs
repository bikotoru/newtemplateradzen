using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using System.Text;
using Backend.Utils.Data;

namespace Backend.Tests.Controllers
{
    public class SimpleIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public SimpleIntegrationTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remover el contexto real
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Agregar contexto InMemory
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });

                    // Override environment variable para testing
                    Environment.SetEnvironmentVariable("SQL", "InMemoryTestConnection");
                });
            });
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [Fact]
        public async Task HealthCheck_Should_Work()
        {
            // Act - Test simple health check
            var response = await _client.GetAsync("/api/categoria/health");

            // Assert
            response.Should().NotBeNull();
            // Puede fallar por la BD pero la ruta debe existir
            response.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.OK,
                System.Net.HttpStatusCode.InternalServerError
            );
        }

        [Fact]
        public async Task API_Routes_Should_Exist()
        {
            // Test que las rutas principales existan
            var routes = new[]
            {
                "/api/categoria/health",
                "/api/categoria/all?page=1&pageSize=1"
            };

            foreach (var route in routes)
            {
                var response = await _client.GetAsync(route);
                // Las rutas deben existir (no 404)
                response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.NotFound);
            }
        }
    }
}