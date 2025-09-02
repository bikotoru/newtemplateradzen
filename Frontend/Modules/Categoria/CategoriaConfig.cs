using Shared.Models.Entities;

namespace Frontend.Modules.Categoria;

public static class CategoriaConfig
{
    /// <summary>
    /// Configuraciones específicas para Categoria
    /// </summary>
    public static class Settings
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const string DefaultSortField = "FechaCreacion";
        public const bool DefaultSortDescending = true;
    }

    /// <summary>
    /// Configuraciones para búsqueda
    /// </summary>
    public static class Search
    {
        public static readonly string[] DefaultSearchFields = { "Nombre", "Descripcion" };
        public const int MinSearchLength = 2;
        public const int SearchDelay = 300; // ms
    }

    /// <summary>
    /// Configuraciones para formularios
    /// </summary>
    public static class Forms
    {
        public const string RequiredFieldMessage = "Este campo es obligatorio";
        public const string SaveSuccessMessage = "Categoría guardada exitosamente";
        public const string DeleteSuccessMessage = "Categoría eliminada exitosamente";
        public const string ErrorGenericMessage = "Ha ocurrido un error inesperado";
    }

    /// <summary>
    /// Configuraciones para exportación
    /// </summary>
    public static class Export
    {
        public const string DefaultFileName = "Categorias";
    }
}