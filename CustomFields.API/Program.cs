using Backend.Utils.Data;
using Backend.Utils.Services;
using Backend.Utils.Security;
using Backend.Utils.EFInterceptors.Extensions;
using Microsoft.EntityFrameworkCore;
using CustomFields.API.Services;
using Shared.Models.Entities.SystemEntities;

var builder = WebApplication.CreateBuilder(args);

// Get connection string from environment variable to match Backend
var connectionString = Environment.GetEnvironmentVariable("SQL")
    ?? "Server=localhost,1333;Database=AgendaGesV2;User Id=sa;Password=Soporte.2019;TrustServerCertificate=true;";

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddOpenApi();

// Add EF Interceptors (this includes database context configuration)
builder.Services.AddEFInterceptors(connectionString);

// Register handlers from assemblies (same as Backend)
builder.Services.AddHandlersFromAssemblies(typeof(Backend.Utils.EFInterceptors.Core.SaveHandler));

// Register custom services
builder.Services.AddScoped<CustomFieldPermissionService>();
builder.Services.AddScoped<FieldConditionEvaluator>();

// Register BaseQueryService for SystemFormEntities with AppDbContext
builder.Services.AddScoped<BaseQueryService<SystemFormEntities>>(provider =>
{
    var appDbContext = provider.GetRequiredService<AppDbContext>();
    var logger = provider.GetRequiredService<ILogger<BaseQueryService<SystemFormEntities>>>();
    return new BaseQueryService<SystemFormEntities>(appDbContext, logger);
});


// Register specialized SystemFormEntityService that handles organization filters correctly
builder.Services.AddScoped<SystemFormEntityService>();

// Register Security services and dependencies (same as Backend)
builder.Services.AddScoped<Backend.Utils.Security.TokenCacheService>();
builder.Services.AddScoped<Backend.Utils.Security.TokenEncryptionService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<Backend.Utils.Security.ICurrentUserService, Backend.Utils.Security.CurrentUserService>();

// Register Login service (needed for authentication)
builder.Services.AddScoped<Backend.Modules.Auth.Login.LoginService>();

// Register additional dependencies
builder.Services.AddHttpContextAccessor();

// CORS para Blazor WebAssembly (same as Backend)
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

// Configure pipeline (same as Backend)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("BlazorPolicy");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
    .WithName("HealthCheck");

// Test endpoint
app.MapGet("/test", () => new { message = "CustomFields.API is running!", version = "1.0.0" })
    .WithName("TestEndpoint");

app.Run();
