using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Frontend;
using Frontend.Services;
using Radzen;
using Shared.Models.Services;


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

// Servicios de autenticación unificados
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
{
    var authStateProvider = provider.GetRequiredService<AuthStateProvider>();
    return authStateProvider;
});

// AuthHttpHandler para inyección automática de tokens (opcional para requests que necesiten auth)
builder.Services.AddScoped<AuthHttpHandler>();

// Servicios adicionales
builder.Services.AddRadzenComponents();

// Servicios de autorización
builder.Services.AddAuthorizationCore();

// Servicio API
builder.Services.AddScoped<API>();

var app = builder.Build();

// AuthService se inicializará automáticamente cuando se use por primera vez

await app.RunAsync();
