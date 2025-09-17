using Backend.Utils.Data;
using Backend.Utils.Services;
using Backend.Utils.Security;
using Microsoft.EntityFrameworkCore;
using CustomFields.API.Services;
using Shared.Models.Entities.SystemEntities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register custom services
builder.Services.AddScoped<CustomFieldPermissionService>();
builder.Services.AddScoped<FieldConditionEvaluator>();

// Register BaseQueryService for SystemFormEntity with AppDbContext
builder.Services.AddScoped<BaseQueryService<SystemFormEntity>>(provider =>
{
    var appDbContext = provider.GetRequiredService<AppDbContext>();
    var logger = provider.GetRequiredService<ILogger<BaseQueryService<SystemFormEntity>>>();
    return new BaseQueryService<SystemFormEntity>(appDbContext, logger);
});

// Register Security services and dependencies
builder.Services.AddScoped<Backend.Utils.Security.TokenCacheService>();
builder.Services.AddScoped<Backend.Utils.Security.TokenEncryptionService>();
builder.Services.AddScoped<PermissionService>();

// Register additional dependencies
builder.Services.AddHttpContextAccessor();

// Configure Database Connection - Updated to match launchSettings
var connectionString = "Server=localhost,1333;Database=AgendaGesV2;User Id=sa;Password=Soporte.2019;TrustServerCertificate=true;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
    .WithName("HealthCheck");

// Test endpoint
app.MapGet("/test", () => new { message = "CustomFields.API is running!", version = "1.0.0" })
    .WithName("TestEndpoint");

app.Run();
