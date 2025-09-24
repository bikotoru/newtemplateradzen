using Backend.Utils.EFInterceptors.Extensions;
using Backend.Modules.Auth.Login;
using Backend.Utils.Security;
using Backend.Utils.Services;
using Backend.Services;
using Shared.Models.Security;

var builder = WebApplication.CreateBuilder(args);

// Get connection string from environment variable (launchSettings.json)
var connectionString = Environment.GetEnvironmentVariable("SQL") 
    ?? "InMemoryTestConnection"; // Fallback para testing

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
        // Manejar ciclos de referencia automáticamente
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Add EF Interceptors
builder.Services.AddEFInterceptors(connectionString);

// Register handlers from assemblies (this will scan for all handler classes)
builder.Services.AddHandlersFromAssemblies(typeof(Backend.Utils.EFInterceptors.Core.SaveHandler));

// Register all services using ServiceRegistry
builder.Services.RegisterAllServices();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<TokenEncryptionService>();
builder.Services.AddScoped<Backend.Utils.Security.ICurrentUserService, Backend.Utils.Security.CurrentUserService>();
builder.Services.AddHttpContextAccessor();

// Register Background Services
builder.Services.AddHostedService<TokenRefreshBackgroundService>();

// CORS para Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7114", "http://localhost:5232")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("BlazorPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();

// Hacer Program accesible para tests
public partial class Program { }
