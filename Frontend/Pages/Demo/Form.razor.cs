using Microsoft.AspNetCore.Components;

namespace Frontend.Pages.Demo;

public partial class Form : ComponentBase
{
    private FormData formData = new();
    private string mensaje = string.Empty;

    private async Task SaveForm()
    {
        // Simular guardado
        await Task.Delay(500);
        
        mensaje = $"Formulario guardado: {formData.Nombre} - {formData.Descripcion}";
        StateHasChanged();
        
        // Limpiar mensaje después de 3 segundos
        await Task.Delay(3000);
        mensaje = string.Empty;
        StateHasChanged();
    }

    public class FormData
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}