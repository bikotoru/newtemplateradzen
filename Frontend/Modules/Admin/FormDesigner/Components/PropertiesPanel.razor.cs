using Microsoft.AspNetCore.Components;
using Forms.Models.DTOs;
using Frontend.Modules.Admin.FormDesigner.Components.Common;

namespace Frontend.Modules.Admin.FormDesigner.Components;

public partial class PropertiesPanel : ComponentBase
{
    [Parameter] public FormFieldLayoutDto? SelectedField { get; set; }
    [Parameter] public FormSectionDto? SelectedSection { get; set; }
    [Parameter] public List<GridSizeOption> FieldSizeOptions { get; set; } = new();
    [Parameter] public List<GridSizeOption> GridSizeOptions { get; set; } = new();
    [Parameter] public EventCallback<FormFieldLayoutDto> OnEditOptionsClick { get; set; }

    private List<object> GetDateFormatOptions()
    {
        return new List<object>
        {
            new { Value = "dd/MM/yyyy", Text = "Solo fecha (dd/MM/yyyy)" },
            new { Value = "dd/MM/yyyy HH:mm", Text = "Fecha y hora (dd/MM/yyyy HH:mm)" },
            new { Value = "dd/MM/yyyy HH:mm:ss", Text = "Fecha, hora y segundos (dd/MM/yyyy HH:mm:ss)" },
            new { Value = "yyyy-MM-dd", Text = "ISO fecha (yyyy-MM-dd)" },
            new { Value = "yyyy-MM-dd HH:mm:ss", Text = "ISO fecha y hora (yyyy-MM-dd HH:mm:ss)" },
            new { Value = "dd-MM-yyyy", Text = "Fecha con guiones (dd-MM-yyyy)" },
            new { Value = "MM/dd/yyyy", Text = "Formato US (MM/dd/yyyy)" },
            new { Value = "dd/MMM/yyyy", Text = "Fecha con mes abreviado (dd/ENE/yyyy)" }
        };
    }

    private List<GridSizeOption> GetDecimalOptions()
    {
        return new List<GridSizeOption>
        {
            new() { Value = 0, Text = "Sin decimales (N0) - 1,234" },
            new() { Value = 1, Text = "1 decimal (N1) - 1,234.5" },
            new() { Value = 2, Text = "2 decimales (N2) - 1,234.56" },
            new() { Value = 3, Text = "3 decimales (N3) - 1,234.567" },
            new() { Value = 4, Text = "4 decimales (N4) - 1,234.5678" }
        };
    }
}