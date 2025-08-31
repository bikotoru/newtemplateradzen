using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.EFInterceptors.Core;
using Backend.Utils.Data;

namespace Backend.Utils.EFInterceptors.Extensions
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Registers the EF Interceptor services and replaces the default AppDbContext with InterceptedAppDbContext
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">Database connection string</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEFInterceptors(this IServiceCollection services, string connectionString)
        {
            // Remove existing AppDbContext registration if any
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Register the interceptor service
            services.AddScoped<EFInterceptorService>();

            // Register the intercepted DbContext
            services.AddDbContext<AppDbContext, InterceptedAppDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(true);
            });

            return services;
        }

        /// <summary>
        /// Registers a specific AddHandler
        /// </summary>
        /// <typeparam name="THandler">Type of the AddHandler to register</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="lifetime">Service lifetime (default: Scoped)</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddAddHandler<THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where THandler : AddHandler
        {
            services.Add(new ServiceDescriptor(typeof(AddHandler), typeof(THandler), lifetime));
            return services;
        }

        /// <summary>
        /// Registers a specific UpdateHandler
        /// </summary>
        /// <typeparam name="THandler">Type of the UpdateHandler to register</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="lifetime">Service lifetime (default: Scoped)</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddUpdateHandler<THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where THandler : UpdateHandler
        {
            services.Add(new ServiceDescriptor(typeof(UpdateHandler), typeof(THandler), lifetime));
            return services;
        }

        /// <summary>
        /// Registers a specific SaveHandler
        /// </summary>
        /// <typeparam name="THandler">Type of the SaveHandler to register</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="lifetime">Service lifetime (default: Scoped)</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddSaveHandler<THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where THandler : SaveHandler
        {
            services.Add(new ServiceDescriptor(typeof(SaveHandler), typeof(THandler), lifetime));
            return services;
        }

        /// <summary>
        /// Registers multiple handlers at once using assembly scanning
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assemblyTypes">Types from assemblies to scan for handlers</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddHandlersFromAssemblies(this IServiceCollection services, params Type[] assemblyTypes)
        {
            foreach (var assemblyType in assemblyTypes)
            {
                var assembly = assemblyType.Assembly;
                
                // Register AddHandlers
                var addHandlers = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AddHandler)));
                
                foreach (var handler in addHandlers)
                {
                    services.AddScoped(typeof(AddHandler), handler);
                }

                // Register UpdateHandlers
                var updateHandlers = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(UpdateHandler)));
                
                foreach (var handler in updateHandlers)
                {
                    services.AddScoped(typeof(UpdateHandler), handler);
                }

                // Register SaveHandlers
                var saveHandlers = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(SaveHandler)));
                
                foreach (var handler in saveHandlers)
                {
                    services.AddScoped(typeof(SaveHandler), handler);
                }
            }

            return services;
        }
    }
}