using Frontend.Components.Base.Tables;
using Frontend.Services;
using Shared.Models.Entities;

namespace Frontend.Modules.Categoria.Models
{
    public class CategoriaViewConfig
    {
        /// <summary>
        /// Nombre descriptivo de la vista/configuración
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// QueryBuilder que define la consulta base con Select incluido
        /// </summary>
        public QueryBuilder<Shared.Models.Entities.Categoria> QueryBuilder { get; set; }

        /// <summary>
        /// Configuración de columnas que se mostrarán SOLO si no están definidas en el RenderFragment Columns
        /// Las columnas se ordenarán por su índice Order SI O SI
        /// </summary>
        public List<ColumnConfig<Shared.Models.Entities.Categoria>>? ColumnConfigs { get; set; }

        public CategoriaViewConfig()
        {
            // QueryBuilder se inicializará cuando se tenga acceso al API service
            QueryBuilder = null!;
        }

        public CategoriaViewConfig(string displayName, QueryBuilder<Shared.Models.Entities.Categoria> queryBuilder, List<ColumnConfig<Shared.Models.Entities.Categoria>>? columnConfigs = null)
        {
            DisplayName = displayName;
            QueryBuilder = queryBuilder;
            ColumnConfigs = columnConfigs;
        }
    }
}