using Backend.Utils.Data;
using Microsoft.EntityFrameworkCore;
using CustomFields.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register custom services
builder.Services.AddScoped<CustomFieldPermissionService>();
builder.Services.AddScoped<FieldConditionEvaluator>();

// Configure Database Connection
var connectionString = "Server=localhost,1333;Database=AgendaGes;User Id=sa;Password=Soporte.2019;TrustServerCertificate=true;";
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
