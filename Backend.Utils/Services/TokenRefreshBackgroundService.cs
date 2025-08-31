using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Backend.Utils.Security;

namespace Backend.Utils.Services
{
    /// <summary>
    /// Servicio de background para limpieza y refresh automático de tokens
    /// </summary>
    public class TokenRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenRefreshBackgroundService> _logger;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(15); // Ejecutar cada 15 minutos

        public TokenRefreshBackgroundService(IServiceProvider serviceProvider, ILogger<TokenRefreshBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TokenRefreshBackgroundService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessTokenMaintenance();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en TokenRefreshBackgroundService");
                }

                await Task.Delay(_refreshInterval, stoppingToken);
            }

            _logger.LogInformation("TokenRefreshBackgroundService detenido");
        }

        private async Task ProcessTokenMaintenance()
        {
            using var scope = _serviceProvider.CreateScope();
            var tokenCacheService = scope.ServiceProvider.GetRequiredService<TokenCacheService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<PermissionService>();

            try
            {
                // Limpiar tokens expirados
                await tokenCacheService.CleanupExpiredTokensAsync();

                // Procesar tokens que necesitan refresh
                // Esta funcionalidad se puede implementar después según necesidades específicas

                _logger.LogDebug("Mantenimiento de tokens completado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante mantenimiento de tokens");
            }
        }
    }
}