using Frontend.Services;
using Shared.Models.Entities;

namespace Frontend.Modules.Categoria
{
    public class CategoriaService
    {
        private readonly APIService _apiService;
        private readonly ILogger<CategoriaService> _logger;
        private const string ENDPOINT_BASE = "api/categoria";

        public CategoriaService(APIService apiService, ILogger<CategoriaService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        #region CRUD Operations

        public async Task<List<Shared.Models.Entities.Categoria>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Getting all categorias");
                return await _apiService.GetListAsync<Shared.Models.Entities.Categoria>(ENDPOINT_BASE);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categorias");
                throw new ServiceException("Error al obtener las categorías", ex);
            }
        }

        public async Task<Shared.Models.Entities.Categoria?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting categoria by ID: {Id}", id);
                return await _apiService.GetAsync<Shared.Models.Entities.Categoria>($"{ENDPOINT_BASE}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categoria by ID: {Id}", id);
                throw new ServiceException($"Error al obtener la categoría con ID {id}", ex);
            }
        }

        public async Task<Shared.Models.Entities.Categoria?> CreateAsync(CreateCategoriaRequest request)
        {
            try
            {
                _logger.LogInformation("Creating categoria: {Nombre}", request.Nombre);
                return await _apiService.PostAsync<Shared.Models.Entities.Categoria>(ENDPOINT_BASE, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating categoria: {Nombre}", request.Nombre);
                throw new ServiceException("Error al crear la categoría", ex);
            }
        }

        public async Task<Shared.Models.Entities.Categoria?> UpdateAsync(Guid id, UpdateCategoriaRequest request)
        {
            try
            {
                _logger.LogInformation("Updating categoria: {Id} - {Nombre}", id, request.Nombre);
                return await _apiService.PutAsync<Shared.Models.Entities.Categoria>($"{ENDPOINT_BASE}/{id}", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating categoria: {Id}", id);
                throw new ServiceException("Error al actualizar la categoría", ex);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting categoria: {Id}", id);
                return await _apiService.DeleteAsync($"{ENDPOINT_BASE}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting categoria: {Id}", id);
                throw new ServiceException("Error al eliminar la categoría", ex);
            }
        }

        #endregion

        #region Business Logic Methods

        public async Task<List<Shared.Models.Entities.Categoria>> SearchAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllAsync();

                _logger.LogInformation("Searching categorias: {SearchTerm}", searchTerm);
                
                // Por ahora filtramos en el cliente, pero idealmente sería en el servidor
                var allCategorias = await GetAllAsync();
                return allCategorias.Where(c => 
                    c.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (c.Descripcion?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching categorias: {SearchTerm}", searchTerm);
                throw new ServiceException("Error al buscar categorías", ex);
            }
        }

        public async Task<bool> ValidateAsync(CreateCategoriaRequest request)
        {
            try
            {
                // Validaciones de negocio
                if (string.IsNullOrWhiteSpace(request.Nombre))
                {
                    throw new ValidationException("El nombre es requerido");
                }

                if (request.Nombre.Length > 255)
                {
                    throw new ValidationException("El nombre no puede exceder 255 caracteres");
                }

                // Aquí podrías agregar más validaciones como verificar nombres duplicados
                return true;
            }
            catch (Exception ex) when (!(ex is ValidationException))
            {
                _logger.LogError(ex, "Error validating categoria request");
                throw new ServiceException("Error al validar la categoría", ex);
            }
        }

        public async Task<bool> ValidateAsync(Guid id, UpdateCategoriaRequest request)
        {
            try
            {
                // Validaciones de negocio
                if (string.IsNullOrWhiteSpace(request.Nombre))
                {
                    throw new ValidationException("El nombre es requerido");
                }

                if (request.Nombre.Length > 255)
                {
                    throw new ValidationException("El nombre no puede exceder 255 caracteres");
                }

                return true;
            }
            catch (Exception ex) when (!(ex is ValidationException))
            {
                _logger.LogError(ex, "Error validating categoria update request");
                throw new ServiceException("Error al validar la actualización de categoría", ex);
            }
        }

        #endregion

        #region UI Helper Methods

        public string GetDisplayName(Shared.Models.Entities.Categoria categoria)
        {
            return categoria?.Nombre ?? "Sin nombre";
        }

        public string GetDisplayDescription(Shared.Models.Entities.Categoria categoria)
        {
            return categoria?.Descripcion ?? "Sin descripción";
        }

        public string GetStatusText(Shared.Models.Entities.Categoria categoria)
        {
            return categoria?.Active == true ? "Activo" : "Inactivo";
        }

        public string GetStatusClass(Shared.Models.Entities.Categoria categoria)
        {
            return categoria?.Active == true ? "badge-success" : "badge-danger";
        }

        #endregion
    }

    #region DTOs - Mirror from Backend

    public class CreateCategoriaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public Guid? OrganizationId { get; set; }
    }

    public class UpdateCategoriaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Active { get; set; } = true;
    }

    #endregion

    #region Custom Exceptions

    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message) { }
        public ServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    #endregion
}