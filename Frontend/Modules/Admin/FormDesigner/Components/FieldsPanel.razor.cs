using Microsoft.AspNetCore.Components;
using Forms.Models.DTOs;

namespace Frontend.Modules.Admin.FormDesigner.Components;

public partial class FieldsPanel : ComponentBase
{
    [Parameter] public GetAvailableFieldsResponse? AvailableFields { get; set; }
    [Parameter] public FormLayoutDto? CurrentLayout { get; set; }
    [Parameter] public EventCallback<FormFieldItemDto> OnAddFieldClick { get; set; }
    [Parameter] public EventCallback<FormFieldItemDto> OnEditFieldClick { get; set; }
    [Parameter] public EventCallback OnCreateFieldClick { get; set; }

    private string GetFieldIcon(string fieldType)
    {
        return fieldType switch
        {
            "text" => "text_fields",
            "textarea" => "notes",
            "number" => "pin",
            "date" => "calendar_today",
            "boolean" => "toggle_on",
            "select" => "list",
            "multiselect" => "checklist",
            _ => "help"
        };
    }

    private string GetFieldTypeDisplay(string fieldType)
    {
        return fieldType switch
        {
            "text" => "Texto",
            "textarea" => "Área de Texto",
            "number" => "Número",
            "date" => "Fecha",
            "boolean" => "Sí/No",
            "select" => "Lista",
            "multiselect" => "Selección Múltiple",
            _ => fieldType
        };
    }

    private bool IsFieldAlreadyAdded(string fieldName)
    {
        if (CurrentLayout?.Sections == null) return false;

        return CurrentLayout.Sections.Any(s =>
            s.Fields.Any(f => f.FieldName == fieldName));
    }
}