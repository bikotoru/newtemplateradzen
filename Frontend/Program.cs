using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Frontend;
using Frontend.Services;
using Radzen;
using Shared.Models.Services;

Console.WriteLine("🚀 Program.cs: Iniciando WebAssembly");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
Console.WriteLine("🚀 Program.cs: Builder creado");

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
Console.WriteLine("🚀 Program.cs: RootComponents agregados");

// HttpClient básico para AuthService (sin interceptor para evitar circular dependency)
Console.WriteLine("🚀 Program.cs: Registrando HttpClient básico");
builder.Services.AddScoped<HttpClient>(sp =>
{
    Console.WriteLine("🌐 HttpClient básico factory: Creando HttpClient sin interceptor");
    var httpClient = new HttpClient()
    {
        BaseAddress = new Uri("https://localhost:7124")
    };
    Console.WriteLine("🌐 HttpClient básico factory: HttpClient creado");
    return httpClient;
});

// Servicios de autenticación unificados
Console.WriteLine("🚀 Program.cs: Registrando servicios de autenticación");
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
{
    Console.WriteLine("🔐 AuthenticationStateProvider factory: Obteniendo AuthStateProvider");
    var authStateProvider = provider.GetRequiredService<AuthStateProvider>();
    Console.WriteLine("🔐 AuthenticationStateProvider factory: AuthStateProvider obtenido");
    return authStateProvider;
});

// AuthHttpHandler para inyección automática de tokens (opcional para requests que necesiten auth)
builder.Services.AddScoped<AuthHttpHandler>();
Console.WriteLine("🚀 Program.cs: Servicios de autenticación registrados");

// Servicios adicionales
Console.WriteLine("🚀 Program.cs: Registrando servicios adicionales");
builder.Services.AddRadzenComponents();

// Servicios de autorización
builder.Services.AddAuthorizationCore();
Console.WriteLine("🚀 Program.cs: Todos los servicios registrados");

Console.WriteLine("🚀 Program.cs: Construyendo app");
var app = builder.Build();
Console.WriteLine("🚀 Program.cs: App construida");

// AuthService se inicializará automáticamente cuando se use por primera vez
Console.WriteLine("🚀 Program.cs: AuthService configurado para inicialización lazy");

Console.WriteLine("🚀 Program.cs: Iniciando RunAsync");
await app.RunAsync();
Console.WriteLine("🚀 Program.cs: RunAsync completado");
