using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using System.Text;
using Backend.Utils.Data;
using Backend.Utils.Security;
using Backend.Tests.Mocks;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Entities;
using Shared.Models.QueryModels;

namespace Backend.Tests.Controllers
{
    public class DebugTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly AppDbContext _context;
        private readonly JsonSerializerOptions _jsonOptions;

        public DebugTests(WebApplicationFactory<Program> factory)
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

                    // Agregar contexto InMemory con el mismo nombre para compartir datos
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("SharedTestDb");
                    });

                    // Registrar servicios necesarios para los tests
                    services.AddScoped<Backend.Modules.Categoria.CategoriaService>(provider =>
                    {
                        var context = provider.GetRequiredService<AppDbContext>();
                        var logger = provider.GetRequiredService<ILogger<Backend.Modules.Categoria.CategoriaService>>();
                        return new Backend.Modules.Categoria.CategoriaService(context, logger);
                    });

                    // Reemplazar servicios de autenticación con Mocks para tests
                    var permissionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(PermissionService));
                    if (permissionDescriptor != null)
                    {
                        services.Remove(permissionDescriptor);
                    }
                    services.AddScoped<PermissionService, MockPermissionService>();
                    
                    var tokenDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TokenCacheService));
                    if (tokenDescriptor != null)
                    {
                        services.Remove(tokenDescriptor);
                    }
                    services.AddScoped<TokenCacheService, MockTokenCacheService>();
                    
                    var tokenEncryptionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TokenEncryptionService));
                    if (tokenEncryptionDescriptor != null)
                    {
                        services.Remove(tokenEncryptionDescriptor);
                    }
                    services.AddScoped<TokenEncryptionService, MockTokenEncryptionService>();
                });
            });

            _client = _factory.CreateClient();
            
            // Obtener contexto del DI container con el mismo nombre de BD
            var scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Limpiar BD antes de cada test
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task Debug_Create_Then_Get_Should_Work()
        {
            // Arrange - Crear categoria directamente en contexto
            var categoria = new Categoria
            {
                Id = Guid.NewGuid(),
                Nombre = "Debug Test",
                Descripcion = "Testing debug",
                Active = true,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            // Verificar que existe en el contexto
            var existsInContext = await _context.Categoria.FindAsync(categoria.Id);
            existsInContext.Should().NotBeNull();

            // Act - Probar GET por API
            var response = await GetAsync<ApiResponse<Categoria>>($"/api/categoria/{categoria.Id}");

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Id.Should().Be(categoria.Id);
        }

        [Fact]
        public async Task Debug_Create_Via_API_Then_Get_Should_Work()
        {
            // Step 1: Crear via API
            var categoria = new Categoria
            {
                Nombre = "API Create Test",
                Descripcion = "Testing API creation",
                Active = true
            };

            var createRequest = new CreateRequest<Categoria>
            {
                Entity = categoria
            };

            var createResponse = await PostAsync<ApiResponse<Categoria>>("/api/categoria/create", createRequest);
            
            // Verificar creación
            createResponse.Should().NotBeNull();
            createResponse!.Success.Should().BeTrue();
            createResponse.Data.Should().NotBeNull();
            
            var createdId = createResponse.Data!.Id;
            createdId.Should().NotBe(Guid.Empty);

            // Step 2: Verificar que existe en la BD
            var existsInDb = await _context.Categoria.FindAsync(createdId);
            existsInDb.Should().NotBeNull("Entity should exist in database after creation");

            // Step 3: Buscar via API
            var getResponse = await GetAsync<ApiResponse<Categoria>>($"/api/categoria/{createdId}");

            // Assert
            getResponse.Should().NotBeNull();
            getResponse!.Success.Should().BeTrue();
            getResponse.Data.Should().NotBeNull();
            getResponse.Data!.Id.Should().Be(createdId);
        }

        [Fact]
        public async Task Debug_Update_Process_Step_By_Step()
        {
            // Step 1: Crear via API
            var categoria = new Categoria
            {
                Nombre = "Update Original",
                Descripcion = "Original description",
                Active = true
            };

            var createRequest = new CreateRequest<Categoria> { Entity = categoria };
            var createResponse = await PostAsync<ApiResponse<Categoria>>("/api/categoria/create", createRequest);
            
            var createdId = createResponse!.Data!.Id;

            // Step 2: Verificar que existe antes del update
            var beforeUpdate = await _context.Categoria.FindAsync(createdId);
            beforeUpdate.Should().NotBeNull("Entity should exist before update");

            // Step 3: Preparar update
            var updatedCategoria = new Categoria
            {
                Id = createdId,
                Nombre = "Update Modified",
                Descripcion = "Modified description",
                Active = false
            };

            var updateRequest = new UpdateRequest<Categoria> { Entity = updatedCategoria };

            // Step 4: Intentar update
            try
            {
                var updateResponse = await PutAsync<ApiResponse<Categoria>>("/api/categoria/update", updateRequest);
                
                // Si llegamos aquí, el update funcionó
                updateResponse.Should().NotBeNull();
                updateResponse!.Success.Should().BeTrue();
            }
            catch (HttpRequestException ex)
            {
                // Capturar el error específico para debugging
                throw new Exception($"Update failed: {ex.Message}. Created ID: {createdId}");
            }
        }

        [Fact]
        public async Task Debug_Search_Process_Step_By_Step()
        {
            // Step 1: Crear varias categorías via API
            var categorias = new[]
            {
                new Categoria { Nombre = "Electrónicos Samsung", Descripcion = "Productos Samsung", Active = true },
                new Categoria { Nombre = "Electrónicos Apple", Descripcion = "Productos Apple", Active = true },
                new Categoria { Nombre = "Ropa Nike", Descripcion = "Ropa deportiva", Active = true }
            };

            var createdIds = new List<Guid>();
            foreach (var categoria in categorias)
            {
                var createRequest = new CreateRequest<Categoria> { Entity = categoria };
                var createResponse = await PostAsync<ApiResponse<Categoria>>("/api/categoria/create", createRequest);
                createdIds.Add(createResponse!.Data!.Id);
            }

            // Step 2: Verificar que existen en BD
            var countInDb = await _context.Categoria.CountAsync();
            countInDb.Should().Be(3, "Should have 3 categories in database");

            // Step 3: Verificar que se pueden obtener via GetAll
            var getAllResponse = await GetAsync<ApiResponse<List<Categoria>>>("/api/categoria/all?all=true");
            getAllResponse!.Success.Should().BeTrue();
            getAllResponse.Data!.Count.Should().Be(3, "GetAll should return 3 categories");

            // Step 4: Intentar search
            var searchRequest = new SearchRequest
            {
                SearchTerm = "electrónicos",
                SearchFields = new[] { "Nombre", "Descripcion" },
                Take = 10
            };

            try
            {
                var searchResponse = await PostAsync<ApiResponse<List<Categoria>>>("/api/categoria/search", searchRequest);
                
                searchResponse.Should().NotBeNull();
                searchResponse!.Success.Should().BeTrue("Search should succeed");
                searchResponse.Data.Should().NotBeNull("Search should return data");
                searchResponse.Data!.Count.Should().BeGreaterThan(0, "Search should find matching categories");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Search failed: {ex.Message}. DB Count: {countInDb}, Created IDs: {string.Join(", ", createdIds)}");
            }
        }

        #region Helper Methods

        private async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await _client.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"GET {endpoint} failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        private async Task<T?> PostAsync<T>(string endpoint, object? data = null)
        {
            var json = data != null ? JsonSerializer.Serialize(data, _jsonOptions) : "{}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _client.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"POST {endpoint} failed: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
        }

        private async Task<T?> PutAsync<T>(string endpoint, object? data = null)
        {
            var json = data != null ? JsonSerializer.Serialize(data, _jsonOptions) : "{}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _client.PutAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"PUT {endpoint} failed: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _client?.Dispose();
        }
    }
}