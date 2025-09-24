using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Utils.Extensions
{
    /// <summary>
    /// Context para operaciones Force que bypassen las validaciones de FieldPermission
    /// </summary>
    public class ForceOperationInfo
    {
        public string? Reason { get; }
        public string? InitiatedBy { get; }
        public DateTime Timestamp { get; }

        public ForceOperationInfo(string? reason = null, string? initiatedBy = null)
        {
            Reason = reason;
            InitiatedBy = initiatedBy;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Contexto thread-safe para operaciones Force usando AsyncLocal
    /// </summary>
    public static class ForceOperationContext
    {
        private static readonly AsyncLocal<ForceOperationInfo> _context = new();

        /// <summary>
        /// Información de la operación Force actual (null si no hay operación Force activa)
        /// </summary>
        public static ForceOperationInfo? Current => _context.Value;

        /// <summary>
        /// Indica si hay una operación Force activa en el contexto actual
        /// </summary>
        public static bool IsForced => _context.Value != null;

        /// <summary>
        /// Establece el contexto Force para la operación actual
        /// </summary>
        internal static void SetForce(string? reason, string? initiatedBy)
        {
            _context.Value = new ForceOperationInfo(reason, initiatedBy);
        }

        /// <summary>
        /// Limpia el contexto Force
        /// </summary>
        internal static void ClearForce()
        {
            _context.Value = null;
        }
    }

    /// <summary>
    /// Extension methods para DbContext y IQueryable que permiten operaciones Force
    /// </summary>
    public static class DbContextForceExtensions
    {
        /// <summary>
        /// Ejecuta SaveChangesAsync bypasseando las validaciones de FieldPermission
        /// </summary>
        /// <param name="context">DbContext</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Número de entidades afectadas</returns>
        public static async Task<int> ForceSaveChangesAsync(this DbContext context, string? reason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Establecer contexto Force
                ForceOperationContext.SetForce(reason, GetCurrentUser());

                // Ejecutar SaveChangesAsync normal - los interceptors detectarán el contexto Force
                return await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                // Siempre limpiar el contexto Force
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Versión síncrona de ForceSaveChanges
        /// </summary>
        /// <param name="context">DbContext</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <returns>Número de entidades afectadas</returns>
        public static int ForceSaveChanges(this DbContext context, string? reason = null)
        {
            try
            {
                // Establecer contexto Force
                ForceOperationContext.SetForce(reason, GetCurrentUser());

                // Ejecutar SaveChanges normal
                return context.SaveChanges();
            }
            finally
            {
                // Siempre limpiar el contexto Force
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Obtiene información del usuario actual (simplificado por ahora)
        /// </summary>
        private static string? GetCurrentUser()
        {
            // TODO: Integrar con CurrentUserService si es necesario
            return "System";
        }

        #region Force Query Extensions

        /// <summary>
        /// Ejecuta ToListAsync bypasseando las validaciones de FieldPermission
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Query a ejecutar</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de entidades</returns>
        public static async Task<List<T>> ForceToListAsync<T>(this IQueryable<T> query, string? reason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ForceOperationContext.SetForce(reason, GetCurrentUser());
                return await query.ToListAsync(cancellationToken);
            }
            finally
            {
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Ejecuta ToList bypasseando las validaciones de FieldPermission (versión síncrona)
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Query a ejecutar</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <returns>Lista de entidades</returns>
        public static List<T> ForceToList<T>(this IQueryable<T> query, string? reason = null)
        {
            try
            {
                ForceOperationContext.SetForce(reason, GetCurrentUser());
                return query.ToList();
            }
            finally
            {
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Ejecuta FirstOrDefaultAsync bypasseando las validaciones de FieldPermission
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Query a ejecutar</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Primera entidad o null</returns>
        public static async Task<T?> ForceFirstOrDefaultAsync<T>(this IQueryable<T> query, string? reason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ForceOperationContext.SetForce(reason, GetCurrentUser());
                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            finally
            {
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Ejecuta FirstOrDefault bypasseando las validaciones de FieldPermission (versión síncrona)
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Query a ejecutar</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <returns>Primera entidad o null</returns>
        public static T? ForceFirstOrDefault<T>(this IQueryable<T> query, string? reason = null)
        {
            try
            {
                ForceOperationContext.SetForce(reason, GetCurrentUser());
                return query.FirstOrDefault();
            }
            finally
            {
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Ejecuta SingleOrDefaultAsync bypasseando las validaciones de FieldPermission
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Query a ejecutar</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Única entidad o null</returns>
        public static async Task<T?> ForceSingleOrDefaultAsync<T>(this IQueryable<T> query, string? reason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ForceOperationContext.SetForce(reason, GetCurrentUser());
                return await query.SingleOrDefaultAsync(cancellationToken);
            }
            finally
            {
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Ejecuta AnyAsync bypasseando las validaciones de FieldPermission
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Query a ejecutar</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si existe algún elemento</returns>
        public static async Task<bool> ForceAnyAsync<T>(this IQueryable<T> query, string? reason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ForceOperationContext.SetForce(reason, GetCurrentUser());
                return await query.AnyAsync(cancellationToken);
            }
            finally
            {
                ForceOperationContext.ClearForce();
            }
        }

        /// <summary>
        /// Ejecuta CountAsync bypasseando las validaciones de FieldPermission
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Query a ejecutar</param>
        /// <param name="reason">Razón opcional para el bypass</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Número de elementos</returns>
        public static async Task<int> ForceCountAsync<T>(this IQueryable<T> query, string? reason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ForceOperationContext.SetForce(reason, GetCurrentUser());
                return await query.CountAsync(cancellationToken);
            }
            finally
            {
                ForceOperationContext.ClearForce();
            }
        }

        #endregion
    }
}