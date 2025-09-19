using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Forms.Models.DTOs;
using Forms.Models.Enums;
using Forms.Models.Configurations;
using Frontend.Modules.Admin.FormDesigner.Components.Common;
using Radzen.Blazor;

namespace Frontend.Modules.Admin.FormDesigner.Components;

public partial class FormCanvas : ComponentBase
{
    [Parameter] public FormLayoutDto CurrentLayout { get; set; } = new();
    [Parameter] public FormSectionDto? SelectedSection { get; set; }
    [Parameter] public List<GridSizeOption> GridSizeOptions { get; set; } = new();
    [Parameter] public List<GridSizeOption> FieldSizeOptions { get; set; } = new();
    [Parameter] public EventCallback OnAddSection { get; set; }
    [Parameter] public EventCallback<FormSectionDto> OnSelectSection { get; set; }
    [Parameter] public EventCallback<FormSectionDto> OnConfigureSection { get; set; }
    [Parameter] public EventCallback<FormSectionDto> OnDeleteSection { get; set; }
    [Parameter] public EventCallback<(FormSectionDto, FormFieldLayoutDto)> OnMoveFieldLeft { get; set; }
    [Parameter] public EventCallback<(FormSectionDto, FormFieldLayoutDto)> OnMoveFieldRight { get; set; }
    [Parameter] public EventCallback<FormFieldLayoutDto> OnConfigureField { get; set; }
    [Parameter] public EventCallback<(FormSectionDto, FormFieldLayoutDto)> OnRemoveField { get; set; }
    [Parameter] public EventCallback OnFieldSizeChanged { get; set; }


    private bool HasPreviewFields => CurrentLayout.Sections.Any(s => s.Fields.Any(f => !f.IsSystemField));

    private string GetSectionGridStyle(FormSectionDto section)
    {
        var columns = section.GridSize switch
        {
            4 => "repeat(1, 1fr)",
            6 => "repeat(2, 1fr)",
            8 => "repeat(3, 1fr)",
            12 => "repeat(4, 1fr)",
            _ => "repeat(2, 1fr)"
        };

        return $"grid-template-columns: {columns};";
    }

    private string GetFieldGridStyle(FormFieldLayoutDto field)
    {
        var span = field.GridSize switch
        {
            3 => "span 1",
            4 => "span 1",
            6 => "span 2",
            8 => "span 3",
            12 => "span 4",
            _ => "span 2"
        };

        return $"grid-column: {span};";
    }

    private RenderFragment RenderFieldPreview(FormFieldLayoutDto field)
    {
        return builder =>
        {
            var fieldType = FieldTypeExtensions.FromString(field.FieldType);

            builder.OpenComponent<RadzenFormField>(0);
            builder.AddAttribute(1, "Text", field.DisplayName + (field.IsRequired ? " *" : ""));
            builder.AddAttribute(2, "Variant", Variant.Outlined);
            builder.AddAttribute(2, "Style", "width: 100%");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(childBuilder =>
            {
                switch (fieldType)
                {
                    case FieldType.Text:
                        childBuilder.OpenComponent<RadzenTextBox>(0);
                        childBuilder.AddAttribute(1, "Placeholder", "Ejemplo de texto...");
                        childBuilder.AddAttribute(2, "Style", "width: 100%;");
                        childBuilder.AddAttribute(3, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.TextArea:
                        childBuilder.OpenComponent<RadzenTextArea>(0);
                        childBuilder.AddAttribute(1, "Placeholder", "Ejemplo de texto largo...");
                        childBuilder.AddAttribute(2, "Rows", 3);
                        childBuilder.AddAttribute(3, "Style", "width: 100%;");
                        childBuilder.AddAttribute(4, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.Number:
                        childBuilder.OpenComponent<RadzenNumeric<decimal?>>(0);
                        childBuilder.AddAttribute(1, "Placeholder", "123");
                        childBuilder.AddAttribute(2, "Style", "width: 100%;");
                        childBuilder.AddAttribute(3, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.Date:
                        childBuilder.OpenComponent<RadzenDatePicker<DateTime?>>(0);
                        childBuilder.AddAttribute(1, "Placeholder", "Selecciona una fecha");
                        childBuilder.AddAttribute(2, "Style", "width: 100%;");
                        childBuilder.AddAttribute(3, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.Boolean:
                        childBuilder.OpenComponent<RadzenSwitch>(0);
                        childBuilder.AddAttribute(1, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.Select:
                        childBuilder.OpenComponent<RadzenDropDown<string>>(0);
                        childBuilder.AddAttribute(1, "Placeholder", "Selecciona una opción");
                        childBuilder.AddAttribute(2, "Data", GetFieldOptionsForMainPreview(field));
                        childBuilder.AddAttribute(3, "Style", "width: 100%;");
                        childBuilder.AddAttribute(4, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.MultiSelect:
                        childBuilder.OpenComponent<RadzenListBox<IEnumerable<string>>>(0);
                        childBuilder.AddAttribute(1, "Data", GetFieldOptionsForMainPreview(field));
                        childBuilder.AddAttribute(2, "Multiple", true);
                        childBuilder.AddAttribute(3, "Style", "width: 100%; height: 100px;");
                        childBuilder.AddAttribute(4, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;
                }
            }));
            builder.CloseComponent();
        };
    }

    private object GetFieldOptionsForMainPreview(FormFieldLayoutDto field)
    {
        if (field.UIConfig?.Options?.Any() == true)
        {
            return field.UIConfig.Options.Select(o => o.Label).ToList();
        }

        // Opciones por defecto si no hay configuradas
        return new List<string> { "Opción 1", "Opción 2" };
    }
}