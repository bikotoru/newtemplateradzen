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

}