using Microsoft.Extensions.Logging;

namespace Frontend.Services
{
    /// <summary>
    /// Servicio genérico para entidades que no tienen servicio específico
    /// </summary>
    public class GenericEntityService<T> : BaseApiService<T> where T : class
    {
        public GenericEntityService(API api, ILogger<BaseApiService<T>> logger)
            : base(api, logger, GetBaseUrlForEntity())
        {
        }

        /// <summary>
        /// Obtiene la URL base para una entidad basándose en su tipo
        /// </summary>
        private static string GetBaseUrlForEntity()
        {
            var entityName = typeof(T).Name.ToLower();

            // Mapeo de entidades a endpoints
            var entityEndpoints = new Dictionary<string, string>
            {
                { "region", "/api/region" },
                { "comuna", "/api/comuna" },
                { "pais", "/api/pais" }
            };

            return entityEndpoints.TryGetValue(entityName, out var endpoint)
                ? endpoint
                : $"/api/{entityName}";
        }
    }
}