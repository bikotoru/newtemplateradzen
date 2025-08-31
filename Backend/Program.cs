using Backend.Utils.EFInterceptors.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Get connection string from environment variable (launchSettings.json)
var connectionString = Environment.GetEnvironmentVariable("SQL") 
    ?? "InMemoryTestConnection"; // Fallback para testing

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Add EF Interceptors
builder.Services.AddEFInterceptors(connectionString);

// Register handlers from assemblies (this will scan for all handler classes)
builder.Services.AddHandlersFromAssemblies(typeof(Program));

// CORS para Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
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
