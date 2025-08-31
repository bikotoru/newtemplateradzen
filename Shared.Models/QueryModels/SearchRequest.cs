namespace Shared.Models.QueryModels
{
    public class SearchRequest
    {
        /// <summary>
        /// Término a buscar (funciona con strings, números, decimales)
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Campos donde buscar (si está vacío usa campos por defecto de la entidad)
        /// </summary>
        public string[] SearchFields { get; set; } = Array.Empty<string>();

        /// <summary>
        /// QueryRequest base que sirve como filtro principal (el SearchTerm se agrega como condición adicional)
        /// </summary>
        public QueryRequest? BaseQuery { get; set; }

        /// <summary>
        /// Ordenamiento opcional (si no se especifica y BaseQuery tampoco, usa por defecto)
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Incluir relaciones (se combinan con las de BaseQuery si existen)
        /// </summary>
        public string[]? Include { get; set; }

        /// <summary>
        /// Saltar registros (para paginación)
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Tomar registros (para paginación)
        /// </summary>
        public int? Take { get; set; }
    }
}