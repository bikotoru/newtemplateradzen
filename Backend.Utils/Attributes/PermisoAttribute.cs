using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Backend.Utils.Security;
using Shared.Models.Responses;

namespace Backend.Utils.Attributes
{
    /// <summary>
    /// Atributo para validar permisos específicos en endpoints
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class PermisoAttribute : ActionFilterAttribute
    {
        private readonly string _permissionKey;

        public PermisoAttribute(string permissionKey)
        {
            _permissionKey = permissionKey ?? throw new ArgumentNullException(nameof(permissionKey));
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var permissionService = context.HttpContext.RequestServices.GetService(typeof(PermissionService)) as PermissionService;
            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            try
            {
                var sessionData = await permissionService.ValidateUserFromHeadersAsync(context.HttpContext.Request.Headers);
                if (sessionData == null)
                {
                    context.Result = new UnauthorizedObjectResult(
                        ApiResponse<object>.ErrorResponse("Usuario no autenticado"));
                    return;
                }

                if (!sessionData.Permisos.Contains(_permissionKey))
                {
                    context.Result = new ObjectResult(
                        ApiResponse<object>.ErrorResponse($"Acceso denegado. Requiere permiso: {_permissionKey}"))
                    {
                        StatusCode = 403
                    };
                    return;
                }

                // Agregar datos de usuario al contexto para uso posterior
                context.HttpContext.Items["SessionData"] = sessionData;
                await next();
            }
            catch (SessionExpiredException)
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse<object>.ErrorResponse("Sesión expirada"));
            }
            catch (Exception)
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse<object>.ErrorResponse("Error validando permisos"));
            }
        }
    }

    /// <summary>
    /// Atributo para validar solo autenticación (sin permiso específico)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AutenticadoAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var permissionService = context.HttpContext.RequestServices.GetService(typeof(PermissionService)) as PermissionService;
            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            try
            {
                var sessionData = await permissionService.ValidateUserFromHeadersAsync(context.HttpContext.Request.Headers);
                if (sessionData == null)
                {
                    context.Result = new UnauthorizedObjectResult(
                        ApiResponse<object>.ErrorResponse("Usuario no autenticado"));
                    return;
                }

                // Agregar datos de usuario al contexto
                context.HttpContext.Items["SessionData"] = sessionData;
                await next();
            }
            catch (SessionExpiredException)
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse<object>.ErrorResponse("Sesión expirada"));
            }
            catch (Exception)
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse<object>.ErrorResponse("Error validando autenticación"));
            }
        }
    }
}