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
using Shared.Models.Builders;

namespace Backend.Tests.Controllers
{
    public class RealisticCRUDTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly AppDbContext _context;
        private readonly JsonSerializerOptions _jsonOptions;

        public RealisticCRUDTests(WebApplicationFactory<Program> factory)
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
                        options.UseInMemoryDatabase("RealisticTestDb");
                        // Las advertencias de transacciones se ignoran automáticamente
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

        #region Tests Realistas (Como realmente funcionaría en frontend)

        [Fact]
        public async Task Frontend_Create_With_Realistic_Builder_Should_Work()
        {
            // Arrange - Como realmente construiría el frontend (sin WithFields que no se serializa)
            var categoria = new Categoria
            {
                Nombre = "Electrónicos Premium",
                Descripcion = "Productos electrónicos de alta gama", 
                Active = true
            };

            // El frontend construiría el request así (sin expressions que no se serializan)
            var createRequest = new CreateRequest<Categoria>
            {
                Entity = categoria,
                // Campos específicos como strings (serializables)
                CreateFields = null // null significa usar todos los campos
            };

            // Act
            var response = await PostAsync<ApiResponse<Categoria>>("/api/categoria/create", createRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Nombre.Should().Be("Electrónicos Premium");
            response.Data.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Frontend_Update_With_Realistic_Builder_Should_Work()
        {
            // Arrange - Crear categoria inicial
            var categoriaId = await CreateTestCategoriaAsync("Original Premium");
            
            var updatedCategoria = new Categoria
            {
                Id = categoriaId,
                Nombre = "Premium Actualizada",
                Descripcion = "Descripción actualizada premium",
                Active = false
            };

            // El frontend construiría el update request así
            var updateRequest = new UpdateRequest<Categoria>
            {
                Entity = updatedCategoria,
                UpdateFields = null // null significa usar todos los campos modificados
            };

            // Act
            var response = await PutAsync<ApiResponse<Categoria>>("/api/categoria/update", updateRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Nombre.Should().Be("Premium Actualizada");
            response.Data.Active.Should().BeFalse();
        }

        [Fact]
        public async Task Frontend_Batch_Create_Realistic_Should_Work()
        {
            // Arrange - Como realmente crearía el frontend múltiples registros
            var categorias = new[]
            {
                new Categoria { Nombre = "Batch Ropa", Descripcion = "Ropa de marca", Active = true },
                new Categoria { Nombre = "Batch Calzado", Descripcion = "Calzado deportivo", Active = true },
                new Categoria { Nombre = "Batch Accesorios", Descripcion = "Accesorios varios", Active = false }
            };

            // Crear requests realistas sin expressions
            var createRequests = categorias.Select(c => 
                new CreateRequest<Categoria>
                {
                    Entity = c,
                    CreateFields = null // El frontend no usaría expressions complejas
                }
            ).ToList();

            var batchRequest = new CreateBatchRequest<Categoria>
            {
                Requests = createRequests,
                ContinueOnError = true,
                UseTransaction = false // InMemory DB no soporta transacciones
            };

            // Act
            var response = await PostAsync<ApiResponse<BatchResponse<Categoria>>>("/api/categoria/create-batch", batchRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.SuccessCount.Should().Be(3);
            response.Data.AllSuccessful.Should().BeTrue();
            response.Data.SuccessfulItems.Should().HaveCount(3);
        }

        [Fact]
        public async Task Frontend_Batch_Update_Realistic_Should_Work()
        {
            // Arrange - Crear categorías existentes
            var id1 = await CreateTestCategoriaAsync("Para Actualizar Batch 1", true);
            var id2 = await CreateTestCategoriaAsync("Para Actualizar Batch 2", true);

            var updatedCategorias = new[]
            {
                new Categoria { Id = id1, Nombre = "Batch Actualizada 1", Descripcion = "Nueva desc batch 1", Active = false },
                new Categoria { Id = id2, Nombre = "Batch Actualizada 2", Descripcion = "Nueva desc batch 2", Active = true }
            };

            // Crear update requests realistas
            var updateRequests = updatedCategorias.Select(c =>
                new UpdateRequest<Categoria>
                {
                    Entity = c,
                    UpdateFields = null // Sin expressions complejas
                }
            ).ToList();

            var batchRequest = new UpdateBatchRequest<Categoria>
            {
                Requests = updateRequests,
                ContinueOnError = true,
                UseTransaction = false // InMemory DB no soporta transacciones
            };

            // Act
            var response = await PutAsync<ApiResponse<BatchResponse<Categoria>>>("/api/categoria/update-batch", batchRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.SuccessCount.Should().Be(2);
            response.Data.AllSuccessful.Should().BeTrue();
        }

        [Fact]
        public async Task Frontend_Query_With_String_Filters_Should_Work()
        {
            // Arrange - Crear datos de prueba
            await CreateTestCategoriaAsync("Query Activa 1", true);
            await CreateTestCategoriaAsync("Query Activa 2", true);
            await CreateTestCategoriaAsync("Query Inactiva", false);

            // Como realmente haría el frontend - con filtros string simples
            var queryRequest = new QueryRequest
            {
                Filter = "Active == true", // String filter que SÍ se serializa
                OrderBy = "Nombre",
                Take = 10
            };

            // Act
            var response = await PostAsync<ApiResponse<List<Categoria>>>("/api/categoria/query", queryRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Should().HaveCount(2); // Solo las activas
            response.Data.All(c => c.Active).Should().BeTrue();
        }

        [Fact]
        public async Task Frontend_Complex_Query_Should_Work()
        {
            // Arrange
            await CreateTestCategoriaAsync("Premium Electronics", true);
            await CreateTestCategoriaAsync("Premium Fashion", true);
            await CreateTestCategoriaAsync("Basic Electronics", true);
            await CreateTestCategoriaAsync("Inactive Premium", false);

            // Query compleja como la haría el frontend
            var queryRequest = new QueryRequest
            {
                Filter = "Active == true && Nombre.Contains(\"Premium\")",
                OrderBy = "Nombre desc",
                Skip = 0,
                Take = 5
            };

            // Act
            var response = await PostAsync<ApiResponse<List<Categoria>>>("/api/categoria/query", queryRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Should().HaveCount(2); // Premium Electronics y Premium Fashion
            response.Data.All(c => c.Active && c.Nombre!.Contains("Premium")).Should().BeTrue();
        }

        [Fact]
        public async Task Frontend_Search_With_Fields_Should_Work()
        {
            // Arrange
            await CreateTestCategoriaAsync("Smartphone Apple", true);
            await CreateTestCategoriaAsync("Laptop Samsung", true);
            await CreateTestCategoriaAsync("Tablet Microsoft", true);
            await CreateTestCategoriaAsync("Clothes Nike", true);

            // Como realmente buscaría el frontend
            var searchRequest = new SearchRequest
            {
                SearchTerm = "apple",
                SearchFields = new[] { "Nombre", "Descripcion" }, // Campos como strings
                Take = 10
            };

            // Act
            var response = await PostAsync<ApiResponse<List<Categoria>>>("/api/categoria/search", searchRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Should().HaveCount(1); // Solo "Smartphone Apple"
            response.Data.First().Nombre.Should().Contain("Apple");
        }

        [Fact]
        public async Task Frontend_Paged_Query_Should_Work()
        {
            // Arrange - Crear muchas categorías
            for (int i = 1; i <= 10; i++)
            {
                await CreateTestCategoriaAsync($"Paged Categoria {i:00}", i % 2 == 0);
            }

            // Query paginada realista
            var queryRequest = new QueryRequest
            {
                Filter = "Active == true",
                OrderBy = "Nombre",
                Skip = 2, // Saltar las primeras 2
                Take = 3  // Tomar 3
            };

            // Act
            var response = await PostAsync<ApiResponse<PagedResult<Categoria>>>("/api/categoria/paged", queryRequest);

            // Assert
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Data.Should().HaveCount(3);
            response.Data.TotalCount.Should().Be(5); // 5 activas de 10 totales
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