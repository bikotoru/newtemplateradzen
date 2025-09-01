using Shared.Models.Responses;

namespace Frontend.Services;

/// <summary>
/// Ejemplo de cómo usar el servicio API
/// </summary>
public class APIServiceExample
{
    private readonly API _api;

    public APIServiceExample(API api)
    {
        _api = api;
    }

    /// <summary>
    /// Ejemplo de métodos GET con diferentes tipos de retorno
    /// </summary>
    public async Task ExampleGetMethods()
    {
        // GET que retorna string - Con autenticación
        string stringResult = await _api.GetStringAsync("/api/categories");
        
        // GET que retorna string - Sin autenticación
        string publicData = await _api.GetStringNoAuthAsync("/api/public/info");
        
        // GET que retorna ApiResponse<T> - Con autenticación
        var categoriesResponse = await _api.GetAsync<List<CategoryDto>>("/api/categories");
        if (categoriesResponse.Success)
        {
            var categories = categoriesResponse.Data;
            // Usar las categorías...
        }
        
        // GET que retorna ApiResponse<T> - Sin autenticación
        var publicResponse = await _api.GetNoAuthAsync<PublicInfoDto>("/api/public/info");
        
        // GET que retorna T directamente - Con autenticación
        var category = await _api.GetDirectAsync<CategoryDto>("/api/categories/123");
        if (category != null)
        {
            // Usar la categoría...
        }
        
        // GET que retorna T directamente - Sin autenticación
        var publicInfo = await _api.GetDirectNoAuthAsync<PublicInfoDto>("/api/public/info");
    }

    /// <summary>
    /// Ejemplo de métodos POST con diferentes tipos de retorno
    /// </summary>
    public async Task ExamplePostMethods()
    {
        var newCategory = new CategoryDto
        {
            Name = "Nueva Categoría",
            Description = "Descripción de la nueva categoría"
        };
        
        // POST que retorna string - Con autenticación
        string result = await _api.PostStringAsync("/api/categories", newCategory);
        
        // POST que retorna string - Sin autenticación
        string publicResult = await _api.PostStringNoAuthAsync("/api/public/feedback", 
            new { message = "Mensaje público" });
        
        // POST que retorna ApiResponse<T> - Con autenticación
        var createResponse = await _api.PostAsync<CategoryDto>("/api/categories", newCategory);
        if (createResponse.Success)
        {
            var createdCategory = createResponse.Data;
            // Usar la categoría creada...
        }
        
        // POST que retorna ApiResponse<T> - Sin autenticación
        var publicResponse = await _api.PostNoAuthAsync<PublicResponseDto>("/api/public/contact", 
            new { email = "test@test.com", message = "Hola" });
        
        // POST que retorna T directamente - Con autenticación
        var directResult = await _api.PostDirectAsync<CategoryDto>("/api/categories", newCategory);
        
        // POST que retorna T directamente - Sin autenticación
        var publicDirect = await _api.PostDirectNoAuthAsync<PublicResponseDto>("/api/public/subscribe", 
            new { email = "test@test.com" });
    }

    /// <summary>
    /// Ejemplo de métodos PUT con diferentes tipos de retorno
    /// </summary>
    public async Task ExamplePutMethods()
    {
        var updatedCategory = new CategoryDto
        {
            Id = 123,
            Name = "Categoría Actualizada",
            Description = "Nueva descripción"
        };
        
        // PUT que retorna string - Con autenticación
        string result = await _api.PutStringAsync("/api/categories/123", updatedCategory);
        
        // PUT que retorna ApiResponse<T> - Con autenticación
        var updateResponse = await _api.PutAsync<CategoryDto>("/api/categories/123", updatedCategory);
        if (updateResponse.Success)
        {
            var updated = updateResponse.Data;
            // Usar la categoría actualizada...
        }
        
        // PUT que retorna T directamente - Con autenticación
        var directResult = await _api.PutDirectAsync<CategoryDto>("/api/categories/123", updatedCategory);
    }

    /// <summary>
    /// Ejemplo de métodos DELETE con diferentes tipos de retorno
    /// </summary>
    public async Task ExampleDeleteMethods()
    {
        // DELETE que retorna string - Con autenticación
        string result = await _api.DeleteStringAsync("/api/categories/123");
        
        // DELETE que retorna ApiResponse<T> - Con autenticación
        var deleteResponse = await _api.DeleteAsync<bool>("/api/categories/123");
        if (deleteResponse.Success && deleteResponse.Data)
        {
            // Categoría eliminada exitosamente
        }
        
        // DELETE que retorna T directamente - Con autenticación
        bool deleted = await _api.DeleteDirectAsync<bool>("/api/categories/123");
        
        // DELETE que retorna ApiResponse<T> - Sin autenticación
        var publicDeleteResponse = await _api.DeleteNoAuthAsync<bool>("/api/public/temp-data/123");
    }

    /// <summary>
    /// Ejemplo de manejo de errores
    /// </summary>
    public async Task ExampleErrorHandling()
    {
        try
        {
            var response = await _api.GetAsync<CategoryDto>("/api/categories/999");
            
            if (!response.Success)
            {
                // Manejar error de API
                Console.WriteLine($"Error: {response.Message}");
                
                if (response.Errors?.Any() == true)
                {
                    foreach (var error in response.Errors)
                    {
                        Console.WriteLine($"- {error}");
                    }
                }
            }
            else
            {
                // Procesar datos exitosos
                var category = response.Data;
            }
        }
        catch (Exception ex)
        {
            // Manejar errores de red u otros errores inesperados
            Console.WriteLine($"Error inesperado: {ex.Message}");
        }
    }

    #region DTOs de ejemplo

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class PublicInfoDto
    {
        public string Version { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; }
        public bool IsOnline { get; set; }
    }

    public class PublicResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }

    #endregion

    /// <summary>
    /// Ejemplos de procesamiento de ApiResponse con diferentes estrategias de manejo de errores
    /// </summary>
    public async Task ExampleApiResponseProcessing()
    {
        // 1. Procesar response con callbacks
        var response = await _api.GetAsync<CategoryDto>("/api/categories/123");
        
        await _api.ProcessResponseAsync(
            response,
            onSuccess: async category => 
            {
                // Procesar categoría exitosa
                Console.WriteLine($"Categoría obtenida: {category.Name}");
                await UpdateUI(category);
            },
            onError: async error => 
            {
                // Manejar error específicamente
                Console.WriteLine($"Error: {error.Message}");
                await ShowErrorMessage(error);
            }
        );

        // 2. Obtener datos con valor por defecto
        var defaultCategory = new CategoryDto { Name = "Por defecto", Description = "Categoría por defecto" };
        var category = _api.GetDataOrDefault(response, defaultCategory);
        
        // 3. Obtener datos o lanzar excepción
        try
        {
            var requiredCategory = _api.GetDataOrThrow(response);
            // Usar la categoría que definitivamente existe
        }
        catch (InvalidOperationException ex)
        {
            // Manejar el caso donde no se pudo obtener la categoría
            Console.WriteLine($"No se pudo obtener la categoría: {ex.Message}");
        }

        // 4. Transformar response a otro tipo
        var nameResponse = _api.TransformResponse(response, cat => cat.Name.ToUpper());
        if (nameResponse.Success)
        {
            Console.WriteLine($"Nombre en mayúsculas: {nameResponse.Data}");
        }

        // 5. Transformación asíncrona
        var processedResponse = await _api.TransformResponseAsync(response, async cat =>
        {
            // Procesar la categoría de forma asíncrona
            await Task.Delay(100); // Simular procesamiento
            return $"Processed: {cat.Name}";
        });

        // 6. Combinar múltiples responses
        var response1 = await _api.GetAsync<CategoryDto>("/api/categories/1");
        var response2 = await _api.GetAsync<CategoryDto>("/api/categories/2");
        var response3 = await _api.GetAsync<CategoryDto>("/api/categories/3");
        
        var combinedResponse = _api.CombineResponses(response1, response2, response3);
        if (combinedResponse.Success)
        {
            Console.WriteLine($"Se obtuvieron {combinedResponse.Data?.Count} categorías");
        }

        // 7. Validaciones rápidas
        if (_api.IsSuccessWithData(response))
        {
            // Procesar datos
        }
        
        if (_api.HasErrors(response))
        {
            var allErrors = _api.GetErrorMessages(response);
            Console.WriteLine($"Errores encontrados:\n{allErrors}");
        }

        // 8. Fluent API para encadenar operaciones
        await _api.OnSuccessAsync(response, async category =>
        {
            Console.WriteLine($"Guardando categoría: {category.Name}");
            await SaveToCache(category);
        });

        _api.OnError(response, error =>
        {
            LogError($"Error obteniendo categoría: {error.Message}");
        });

        // 9. Encadenar múltiples operaciones
        var finalResponse = await _api
            .OnSuccessAsync(response, async cat => await ValidateCategory(cat))
            .ContinueWith(async task =>
            {
                var result = await task;
                return await _api.OnErrorAsync(result, async error =>
                {
                    await NotifyUser($"Error: {error.Message}");
                });
            });
    }

    /// <summary>
    /// Ejemplo de manejo de errores avanzado con diferentes estrategias
    /// </summary>
    public async Task ExampleAdvancedErrorHandling()
    {
        // Estrategia 1: Reintentos automáticos
        var response = await _api.GetAsync<CategoryDto>("/api/categories/123");
        int maxRetries = 3;
        int currentRetry = 0;

        while (!response.Success && currentRetry < maxRetries)
        {
            Console.WriteLine($"Intento {currentRetry + 1} falló, reintentando...");
            await Task.Delay(1000 * (currentRetry + 1)); // Backoff exponencial
            response = await _api.GetAsync<CategoryDto>("/api/categories/123");
            currentRetry++;
        }

        // Estrategia 2: Fallback a diferentes fuentes
        var categories = _api.GetDataOrDefault(response);
        if (categories == null)
        {
            // Intentar fuente alternativa
            var fallbackResponse = await _api.GetNoAuthAsync<CategoryDto>("/api/public/categories/123");
            categories = _api.GetDataOrDefault(fallbackResponse);
            
            if (categories == null)
            {
                // Último recurso: datos desde caché local
                categories = await GetFromLocalCache();
            }
        }

        // Estrategia 3: Procesamiento parcial de errores
        var multipleResponses = new[]
        {
            await _api.GetAsync<CategoryDto>("/api/categories/1"),
            await _api.GetAsync<CategoryDto>("/api/categories/2"),
            await _api.GetAsync<CategoryDto>("/api/categories/3")
        };

        var combinedResult = _api.CombineResponses(multipleResponses);
        
        if (combinedResult.Success)
        {
            // Procesar las categorías que sí se obtuvieron
            foreach (var cat in combinedResult.Data!)
            {
                await ProcessCategory(cat);
            }

            // Manejar errores parciales si existen
            if (combinedResult.Errors?.Any() == true)
            {
                Console.WriteLine("Algunos elementos no pudieron procesarse:");
                foreach (var error in combinedResult.Errors)
                {
                    Console.WriteLine($"- {error}");
                }
            }
        }
    }

    /// <summary>
    /// Ejemplo de uso en componentes Blazor
    /// </summary>
    public async Task ExampleBlazorComponentUsage()
    {
        // En un componente Blazor, puedes usar estos métodos para manejar UI
        var response = await _api.GetAsync<List<CategoryDto>>("/api/categories");

        await _api.ProcessResponseAsync(
            response,
            onSuccess: async categories =>
            {
                // Actualizar estado del componente
                Categories = categories;
                IsLoading = false;
                StateHasChanged(); // En Blazor
            },
            onError: async error =>
            {
                // Mostrar mensaje de error
                ErrorMessage = _api.GetErrorMessages(error);
                IsLoading = false;
                IsError = true;
                StateHasChanged(); // En Blazor
            }
        );

        // O usando el patrón más simple
        var categories = _api.GetDataOrDefault(response, new List<CategoryDto>());
        Categories = categories;
        
        if (_api.HasErrors(response))
        {
            ErrorMessage = _api.GetErrorMessages(response);
            IsError = true;
        }
    }

    #region Métodos auxiliares de ejemplo
    
    private async Task UpdateUI(CategoryDto category) => await Task.CompletedTask;
    private async Task ShowErrorMessage<T>(ApiResponse<T> error) => await Task.CompletedTask;
    private async Task SaveToCache(CategoryDto category) => await Task.CompletedTask;
    private async Task ValidateCategory(CategoryDto category) => await Task.CompletedTask;
    private async Task NotifyUser(string message) => await Task.CompletedTask;
    private async Task ProcessCategory(CategoryDto category) => await Task.CompletedTask;
    private async Task<CategoryDto> GetFromLocalCache() => await Task.FromResult(new CategoryDto());
    private void LogError(string message) { }
    private void StateHasChanged() { }
    
    // Propiedades de ejemplo para Blazor
    private List<CategoryDto> Categories { get; set; } = new();
    private bool IsLoading { get; set; }
    private bool IsError { get; set; }
    private string ErrorMessage { get; set; } = string.Empty;

    #endregion
}