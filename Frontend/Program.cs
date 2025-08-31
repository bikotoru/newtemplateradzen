using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;
using Frontend.Services;
using Radzen;
using Shared.Models.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient configurado para API Backend
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("https://localhost:7002") // Puerto del Backend
});

// Servicios
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<CryptoService>();

await builder.Build().RunAsync();
