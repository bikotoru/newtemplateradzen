using Frontend.Services;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

namespace Frontend.Pages.AdvancedQuery
{
    public class SavedQueryService : BaseApiService<SavedQueryDto>
    {
        public SavedQueryService(API api, ILogger<SavedQueryService> logger) 
            : base(api, logger, "api/saved-queries", BackendType.FormBackend)
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<SavedQueryDto>)
        // - UpdateAsync(UpdateRequest<SavedQueryDto>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<SavedQueryDto>)
        // - UpdateBatchAsync(UpdateBatchRequest<SavedQueryDto>)
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Name).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Métodos específicos para SavedQueries

        /// <summary>
        /// Obtener búsquedas guardadas con filtros específicos
        /// </summary>
        public async Task<SavedQueriesListResponse> GetSavedQueriesAsync(
            string? entityName = null,
            bool includePublic = true,
            bool includeShared = true,
            int skip = 0,
            int take = 50)
        {
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(entityName))
                    queryParams["entityName"] = entityName;
                queryParams["includePublic"] = includePublic.ToString();
                queryParams["includeShared"] = includeShared.ToString();
                queryParams["skip"] = skip.ToString();
                queryParams["take"] = take.ToString();

                var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                var response = await _api.GetAsync<SavedQueriesListResponse>($"{_baseUrl}?{queryString}", BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting saved queries with filters");
                throw;
            }
        }

        /// <summary>
        /// Duplicar una búsqueda guardada
        /// </summary>
        public async Task<SavedQueryResponse> DuplicateSavedQueryAsync(Guid id, string newName)
        {
            try
            {
                var request = new { NewName = newName };
                var response = await _api.PostAsync<SavedQueryResponse>($"{_baseUrl}/{id}/duplicate", request, BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating saved query {SavedQueryId}", id);
                throw;
            }
        }

        // Métodos para gestión de compartidos
        public async Task<SavedQuerySharesListResponse> GetSharesAsync(Guid savedQueryId)
        {
            try
            {
                var response = await _api.GetAsync<SavedQuerySharesListResponse>($"{_baseUrl}/{savedQueryId}/shares", BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shares for saved query {SavedQueryId}", savedQueryId);
                throw;
            }
        }

        public async Task<SavedQueryShareResponse> ShareWithUserAsync(Guid savedQueryId, Guid targetUserId, CreateShareRequest permissions)
        {
            try
            {
                var response = await _api.PostAsync<SavedQueryShareResponse>($"{_baseUrl}/{savedQueryId}/shares/users/{targetUserId}", permissions, BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing saved query {SavedQueryId} with user {UserId}", savedQueryId, targetUserId);
                throw;
            }
        }

        public async Task<SavedQueryShareResponse> ShareWithRoleAsync(Guid savedQueryId, Guid targetRoleId, CreateShareRequest permissions)
        {
            try
            {
                var response = await _api.PostAsync<SavedQueryShareResponse>($"{_baseUrl}/{savedQueryId}/shares/roles/{targetRoleId}", permissions, BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing saved query {SavedQueryId} with role {RoleId}", savedQueryId, targetRoleId);
                throw;
            }
        }

        public async Task<SavedQueryShareResponse> UpdateShareAsync(Guid savedQueryId, Guid shareId, UpdateShareRequest permissions)
        {
            try
            {
                var response = await _api.PutAsync<SavedQueryShareResponse>($"{_baseUrl}/{savedQueryId}/shares/{shareId}", permissions, BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share {ShareId} for saved query {SavedQueryId}", shareId, savedQueryId);
                throw;
            }
        }

        public async Task<SavedQueryShareResponse> RevokeShareAsync(Guid savedQueryId, Guid shareId)
        {
            try
            {
                var response = await _api.DeleteAsync<SavedQueryShareResponse>($"{_baseUrl}/{savedQueryId}/shares/{shareId}");
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share {ShareId} for saved query {SavedQueryId}", shareId, savedQueryId);
                throw;
            }
        }

        public async Task<AvailableUsersResponse> GetAvailableUsersAsync(Guid savedQueryId)
        {
            try
            {
                var response = await _api.GetAsync<AvailableUsersResponse>($"{_baseUrl}/{savedQueryId}/shares/available-users", BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available users for saved query {SavedQueryId}", savedQueryId);
                throw;
            }
        }

        public async Task<AvailableRolesResponse> GetAvailableRolesAsync(Guid savedQueryId)
        {
            try
            {
                var response = await _api.GetAsync<AvailableRolesResponse>($"{_baseUrl}/{savedQueryId}/shares/available-roles", BackendType.FormBackend);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available roles for saved query {SavedQueryId}", savedQueryId);
                throw;
            }
        }
    }

    // DTOs matching the backend SavedQueriesController DTOs
    public class SavedQueryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string EntityName { get; set; } = "";
        public string SelectedFields { get; set; } = "";
        public string? FilterConfiguration { get; set; }
        public byte LogicalOperator { get; set; }
        public int TakeLimit { get; set; }
        public bool IsPublic { get; set; }
        public bool IsTemplate { get; set; }
        public Guid? CreadorId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public bool CanEdit { get; set; }
        public bool CanShare { get; set; }
        public int SharedCount { get; set; }
        
        // Additional properties for frontend compatibility
        public bool Active { get; set; } = true;
    }

    // Response DTOs matching backend
    public class SavedQueryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public SavedQueryDto? Data { get; set; }
    }

    public class SavedQueriesListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<SavedQueryDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
    }

    // Share DTOs
    public class SavedQueryShareDto
    {
        public Guid Id { get; set; }
        public Guid SavedQueryId { get; set; }
        public Guid? SharedWithUserId { get; set; }
        public Guid? SharedWithRoleId { get; set; }
        public Guid? SharedWithOrganizationId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanExecute { get; set; }
        public bool CanShare { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string SharedWithName { get; set; } = "";
    }

    public class CreateShareRequest
    {
        public bool CanView { get; set; } = true;
        public bool CanEdit { get; set; } = false;
        public bool CanExecute { get; set; } = true;
        public bool CanShare { get; set; } = false;
    }

    public class UpdateShareRequest
    {
        public bool CanView { get; set; } = true;
        public bool CanEdit { get; set; } = false;
        public bool CanExecute { get; set; } = true;
        public bool CanShare { get; set; } = false;
    }

    public class AvailableUserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class AvailableRoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public class SavedQueryShareResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public SavedQueryShareDto? Data { get; set; }
    }

    public class SavedQuerySharesListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<SavedQueryShareDto> Data { get; set; } = new();
    }

    public class AvailableUsersResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<AvailableUserDto> Data { get; set; } = new();
    }

    public class AvailableRolesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<AvailableRoleDto> Data { get; set; } = new();
    }
}