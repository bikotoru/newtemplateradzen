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
    public class SimplifiedCRUDTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly AppDbContext _context;
        private readonly JsonSerializerOptions _jsonOptions;

        public SimplifiedCRUDTests(WebApplicationFactory<Program> factory)
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

                    // Agregar contexto InMemory con nombre fijo para compartir datos
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
            
            // Obtener contexto del DI container
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

        #region CRUD Simple Tests (Sin Builders)

        [Fact]
        public async Task Frontend_Create_Simple_Categoria_Should_Work()
        {
            // Arrange - Simular request simple del frontend
            var categoria = new Categoria
            {
                Nombre = "Electrónicos Simple",
                Descripcion = "Test simple",
                Active = true
            };

            var createRequest = new CreateRequest<Categoria>
            {
                Entity = categoria
            };

            // Act - Simular POST del frontend
            var response = await PostAsync<ApiResponse<Categoria>>("/api/categoria/create", createRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Nombre.Should().Be("Electrónicos Simple");
            response.Data.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Frontend_Update_Simple_Categoria_Should_Work()
        {
            // Arrange - Crear categoria inicial
            var categoriaId = await CreateTestCategoriaAsync("Original");
            
            var updatedCategoria = new Categoria
            {
                Id = categoriaId,
                Nombre = "Actualizada Simple",
                Descripcion = "Test actualización simple",
                Active = true
            };

            var updateRequest = new UpdateRequest<Categoria>
            {
                Entity = updatedCategoria
            };

            // Act
            var response = await PutAsync<ApiResponse<Categoria>>("/api/categoria/update", updateRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Nombre.Should().Be("Actualizada Simple");
        }

        [Fact]
        public async Task Frontend_GetAll_With_Pagination_Should_Work()
        {
            // Arrange - Crear datos de prueba
            await CreateTestCategoriaAsync("Test 1", true);
            await CreateTestCategoriaAsync("Test 2", true);
            await CreateTestCategoriaAsync("Test 3", false);

            // Act - Simular GET paginado del frontend
            var response = await GetAsync<ApiResponse<PagedResponse<Categoria>>>("/api/categoria/all?page=1&pageSize=2");

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Data.Count.Should().BeLessThanOrEqualTo(2);
            response.Data.Page.Should().Be(1);
            response.Data.PageSize.Should().Be(2);
        }

        [Fact]
        public async Task Frontend_GetAll_Without_Pagination_Should_Work()
        {
            // Arrange
            await CreateTestCategoriaAsync("All 1", true);
            await CreateTestCategoriaAsync("All 2", true);

            // Act - Simular GET all del frontend
            var response = await GetAsync<ApiResponse<List<Categoria>>>("/api/categoria/all?all=true");

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Count.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task Frontend_GetById_Should_Work()
        {
            // Arrange
            var categoriaId = await CreateTestCategoriaAsync("Específica");

            // Act - Simular GET by ID del frontend
            var response = await GetAsync<ApiResponse<Categoria>>($"/api/categoria/{categoriaId}");

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Id.Should().Be(categoriaId);
            response.Data.Nombre.Should().Be("Específica");
        }

        [Fact]
        public async Task Frontend_Delete_Should_Work()
        {
            // Arrange
            var categoriaId = await CreateTestCategoriaAsync("Para Eliminar");

            // Act - Simular DELETE del frontend
            var response = await DeleteAsync<ApiResponse<bool>>($"/api/categoria/{categoriaId}");

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().BeTrue();

            // Verificar que ya no existe
            var getResponse = await GetRawResponseAsync($"/api/categoria/{categoriaId}");
            getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        #endregion

        #region Query Simple Tests

        [Fact]
        public async Task Frontend_Query_Simple_Should_Work()
        {
            // Arrange
            await CreateTestCategoriaAsync("QueryTest1", true);
            await CreateTestCategoriaAsync("QueryTest2", false);

            // Simular query simple del frontend
            var queryRequest = new QueryRequest
            {
                Filter = "Active == true",
                Take = 10
            };

            // Act
            var response = await PostAsync<ApiResponse<List<Categoria>>>("/api/categoria/query", queryRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.All(c => c.Active).Should().BeTrue();
        }

        [Fact]
        public async Task Frontend_Search_Simple_Should_Work()
        {
            // Arrange
            await CreateTestCategoriaAsync("Búsqueda Test", true);
            await CreateTestCategoriaAsync("Otro Test", true);
            await CreateTestCategoriaAsync("Sin Match", true);

            // Simular búsqueda del frontend
            var searchRequest = new SearchRequest
            {
                SearchTerm = "test",
                Take = 10
            };

            // Act
            var response = await PostAsync<ApiResponse<List<Categoria>>>("/api/categoria/search", searchRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Count.Should().BeGreaterThanOrEqualTo(2);
        }

        #endregion

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

        private async Task<T?> DeleteAsync<T>(string endpoint)
        {
            var response = await _client.DeleteAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"DELETE {endpoint} failed: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
        }

        private async Task<HttpResponseMessage> GetRawResponseAsync(string endpoint)
        {
            return await _client.GetAsync(endpoint);
        }

        private async Task<Guid> CreateTestCategoriaAsync(string nombre, bool active = true)
        {
            var categoria = new Categoria
            {
                Id = Guid.NewGuid(),
                Nombre = nombre,
                Descripcion = $"Descripción para {nombre}",
                Active = active,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();
            
            return categoria.Id;
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _client?.Dispose();
        }
    }
}