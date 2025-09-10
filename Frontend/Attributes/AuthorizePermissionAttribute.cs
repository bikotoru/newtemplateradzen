using Microsoft.AspNetCore.Components;

namespace Frontend.Attributes
{
    /// <summary>
    /// Atributo para autorizar páginas Blazor basado en permisos
    /// Uso: [AuthorizePermission("permission1", "permission2", ...)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AuthorizePermissionAttribute : Attribute
    {
        /// <summary>
        /// Lista de permisos requeridos. El usuario debe tener al menos uno para acceder.
        /// </summary>
        public string[] RequiredPermissions { get; }

        /// <summary>
        /// Constructor para especificar permisos requeridos
        /// </summary>
        /// <param name="permissions">Permisos requeridos. El usuario necesita al menos uno.</param>
        public AuthorizePermissionAttribute(params string[] permissions)
        {
            RequiredPermissions = permissions ?? Array.Empty<string>();
        }
    }
}