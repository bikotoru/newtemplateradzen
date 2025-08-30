using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Reflection;
using Shared.Models.QueryModels;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly ILogger<QueryController> _logger;

        public QueryController(ILogger<QueryController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{entityName}/query")]
        public async Task<ActionResult> Query(string entityName, [FromBody] QueryRequest request)
        {
            try
            {
                _logger.LogInformation("Executing query for entity: {EntityName}", entityName);
                
                // Por ahora retornamos un resultado dummy
                // Aquí se implementará la lógica real cuando tengamos el DbContext configurado
                var result = new List<object>
                {
                    new { Id = Guid.NewGuid(), Name = $"Sample {entityName}" }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query for {EntityName}", entityName);
                return StatusCode(500, $"Error executing query: {ex.Message}");
            }
        }

        [HttpPost("{entityName}/select")]
        public async Task<ActionResult> Select(string entityName, [FromBody] QueryRequest request)
        {
            try
            {
                _logger.LogInformation("Executing select query for entity: {EntityName}", entityName);

                // Por ahora retornamos un resultado dummy
                var result = new List<object>
                {
                    new { Id = Guid.NewGuid(), Name = $"Selected {entityName}" }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing select query for {EntityName}", entityName);
                return StatusCode(500, $"Error executing select query: {ex.Message}");
            }
        }

        [HttpPost("{entityName}/paged")]
        public async Task<ActionResult<Shared.Models.QueryModels.PagedResult<object>>> QueryPaged(string entityName, [FromBody] QueryRequest request)
        {
            try
            {
                _logger.LogInformation("Executing paged query for entity: {EntityName}", entityName);

                // Por ahora retornamos un resultado dummy
                var data = new List<object>
                {
                    new { Id = Guid.NewGuid(), Name = $"Paged {entityName}" }
                };

                var result = new Shared.Models.QueryModels.PagedResult<object>
                {
                    Data = data,
                    TotalCount = 1,
                    Page = 1,
                    PageSize = request.Take ?? 1
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing paged query for {EntityName}", entityName);
                return StatusCode(500, $"Error executing query: {ex.Message}");
            }
        }

        [HttpPost("{entityName}/select-paged")]
        public async Task<ActionResult<Shared.Models.QueryModels.PagedResult<object>>> SelectPaged(string entityName, [FromBody] QueryRequest request)
        {
            try
            {
                _logger.LogInformation("Executing select paged query for entity: {EntityName}", entityName);

                var data = new List<object>
                {
                    new { Id = Guid.NewGuid(), Name = $"Select Paged {entityName}" }
                };

                var result = new Shared.Models.QueryModels.PagedResult<object>
                {
                    Data = data,
                    TotalCount = 1,
                    Page = 1,
                    PageSize = request.Take ?? 1
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing select paged query for {EntityName}", entityName);
                return StatusCode(500, $"Error executing query: {ex.Message}");
            }
        }
    }
}