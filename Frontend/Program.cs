using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Frontend;
using Frontend.Services;
using Radzen;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient básico para AuthService (sin interceptor para evitar circular dependency)
builder.Services.AddScoped<HttpClient>(sp =>
{
    var httpClient = new HttpClient()
    {
        BaseAddress = new Uri("https://localhost:7124")
    };
    return httpClient;
});

// Registrar todos los servicios usando ServiceRegistry
builder.Services.RegisterAllServices();

// Servicios adicionales
builder.Services.AddRadzenComponents();

// Servicios de autorización
builder.Services.AddAuthorizationCore();

// Servicio API
builder.Services.AddScoped<API>();

var app = builder.Build();

// AuthService se inicializará automáticamente cuando se use por primera vez

await app.RunAsync();
