using Microsoft.AspNetCore.Components;
using Frontend.Services;
using Frontend.Attributes;
using System.Reflection;

namespace Frontend.Components.Auth
{
    /// <summary>
    /// Clase base para páginas que requieren autorización por permisos
    /// Las páginas deben heredar de esta clase e incluir el atributo [AuthorizePermission]
    /// </summary>
    public abstract class AuthorizedPageBase : ComponentBase
    {
        [Inject] protected AuthService AuthService { get; set; } = null!;
        [Inject] protected NavigationManager Navigation { get; set; } = null!;

        protected bool IsCheckingPermissions { get; private set; } = true;
        protected bool HasRequiredPermissions { get; private set; } = false;
        protected string[] RequiredPermissions { get; private set; } = Array.Empty<string>();

        protected override async Task OnInitializedAsync()
        {
            await CheckPagePermissionsAsync();
            await base.OnInitializedAsync();
        }

        /// <summary>
        /// Verifica si el usuario tiene los permisos requeridos para esta página
        /// </summary>
        private async Task CheckPagePermissionsAsync()
        {
            try
            {
                // Obtener el atributo AuthorizePermission de la página actual
                var pageType = GetType();
                var authorizeAttribute = pageType.GetCustomAttribute<AuthorizePermissionAttribute>();

                if (authorizeAttribute != null)
                {
                    RequiredPermissions = authorizeAttribute.RequiredPermissions;

                    // Asegurar que el AuthService esté inicializado
                    await AuthService.EnsureInitializedAsync();

                    // Verificar si está autenticado
                    if (!await AuthService.IsAuthenticatedAsync())
                    {
                        // Redirigir al login si no está autenticado
                        Navigation.NavigateTo("/login");
                        return;
                    }

                    // Verificar permisos
                    if (RequiredPermissions.Any())
                    {
                        HasRequiredPermissions = await AuthService.HasAnyPermissionAsync(RequiredPermissions);

                        if (!HasRequiredPermissions)
                        {
                            // Redirigir a página de no autorizado con información de permisos
                            var permissionsQuery = string.Join("&", RequiredPermissions.Select(p => $"Permissions={Uri.EscapeDataString(p)}"));
                            Navigation.NavigateTo($"/not-authorized?{permissionsQuery}");
                            return;
                        }
                    }
                    else
                    {
                        // Si no hay permisos específicos, solo verificar autenticación
                        HasRequiredPermissions = true;
                    }
                }
                else
                {
                    // Si no hay atributo, permitir acceso (página pública)
                    HasRequiredPermissions = true;
                }
            }
            catch (Exception ex)
            {
                // En caso de error, denegar acceso por seguridad
                Console.WriteLine($"Error checking page permissions: {ex.Message}");
                HasRequiredPermissions = false;
                Navigation.NavigateTo("/not-authorized");
            }
            finally
            {
                IsCheckingPermissions = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Método que pueden sobrescribir las páginas derivadas para lógica adicional después de la verificación de permisos
        /// </summary>
        protected virtual async Task OnPermissionsVerifiedAsync()
        {
            // Implementación por defecto vacía
            await Task.CompletedTask;
        }

        /// <summary>
        /// Renderiza el contenido de la página solo si tiene permisos
        /// </summary>
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (IsCheckingPermissions)
            {
                // Mostrar indicador de carga mientras se verifican permisos
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "permission-checking-overlay");
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", "permission-checking-content");
                builder.OpenComponent<Radzen.Blazor.RadzenProgressBarCircular>(4);
                builder.AddAttribute(5, "ShowValue", false);
                builder.AddAttribute(6, "Mode", Radzen.ProgressBarMode.Indeterminate);
                builder.CloseComponent();
                builder.AddMarkupContent(7, "<p>Verificando permisos...</p>");
                builder.CloseElement();
                builder.CloseElement();

                // Agregar estilos CSS inline
                builder.OpenElement(8, "style");
                builder.AddMarkupContent(9, @"
                    .permission-checking-overlay {
                        position: fixed;
                        top: 0;
                        left: 0;
                        width: 100%;
                        height: 100%;
                        background: rgba(255, 255, 255, 0.9);
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        z-index: 9999;
                    }
                    .permission-checking-content {
                        text-align: center;
                    }
                    .permission-checking-content p {
                        margin-top: 1rem;
                        color: var(--rz-text-secondary-color);
                    }
                ");
                builder.CloseElement();
            }
            else if (HasRequiredPermissions)
            {
                // Renderizar el contenido normal de la página
                base.BuildRenderTree(builder);
            }
            // Si no tiene permisos, la redirección ya se manejó en CheckPagePermissionsAsync
        }
    }
}