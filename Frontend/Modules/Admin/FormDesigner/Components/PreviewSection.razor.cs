using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Forms.Models.DTOs;
using Forms.Models.Enums;
using Forms.Models.Configurations;
using Frontend.Modules.Admin.FormDesigner.Components.Common;
using Radzen.Blazor;

namespace Frontend.Modules.Admin.FormDesigner.Components;

public partial class PreviewSection : ComponentBase
{
    [Parameter] public FormLayoutDto CurrentLayout { get; set; } = new();
    [Parameter] public FormSectionDto? SelectedSection { get; set; }
    [Parameter] public List<GridSizeOption> FieldSizeOptions { get; set; } = new();
    [Parameter] public EventCallback<FormSectionDto> OnSelectSection { get; set; }
    [Parameter] public EventCallback<FormSectionDto> OnConfigureSection { get; set; }
    [Parameter] public EventCallback<FormSectionDto> OnDeleteSection { get; set; }
    [Parameter] public EventCallback<(FormSectionDto, FormFieldLayoutDto)> OnMoveFieldLeft { get; set; }
    [Parameter] public EventCallback<(FormSectionDto, FormFieldLayoutDto)> OnMoveFieldRight { get; set; }
    [Parameter] public EventCallback<FormFieldLayoutDto> OnConfigureField { get; set; }
    [Parameter] public EventCallback<(FormSectionDto, FormFieldLayoutDto)> OnRemoveField { get; set; }
    [Parameter] public EventCallback OnFieldSizeChanged { get; set; }
    [Parameter] public EventCallback OnAddSection { get; set; }


    private RenderFragment RenderFieldPreview(FormFieldLayoutDto field) => builder =>
    {
        var displayName = field.DisplayName + (field.IsRequired ? " *" : "");

        // Para campos boolean, usar diseño horizontal simple pero dentro de un FormField
        if (field.FieldType.ToLowerInvariant() == "boolean")
        {
            var trueLabel = field.UIConfig?.TrueLabel ?? "Activado";
            var falseLabel = field.UIConfig?.FalseLabel ?? "Desactivado";

            builder.OpenComponent<RadzenFormField>(0);
            builder.AddAttribute(1, "Text", displayName);
            builder.AddAttribute(2, "Style", "width: 100%");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.AddMarkupContent(0, $@"
                    <div style='display: flex; align-items: center; gap: 1rem; padding: 0.5rem 0;'>
                        <div style='width: 40px; height: 20px; background: #10b981; border-radius: 10px; position: relative;'>
                            <div style='width: 16px; height: 16px; background: white; border-radius: 50%; position: absolute; top: 2px; right: 2px; box-shadow: 0 1px 3px rgba(0,0,0,0.3);'></div>
                        </div>
                        <span style='color: #10b981; font-weight: 500;'>{trueLabel}</span>
                        <span style='color: #94a3b8; font-size: 0.9rem;'>/ {falseLabel}</span>
                    </div>");
            }));
            builder.CloseComponent();
            return;
        }

        // Para otros tipos, usar RadzenFormField simple
        builder.OpenComponent<RadzenFormField>(0);
        builder.AddAttribute(1, "Text", displayName);
        builder.AddAttribute(2, "Style", "width: 100%");
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(fieldBuilder =>
        {
            switch (field.FieldType.ToLowerInvariant())
            {
                case "select":
                    fieldBuilder.OpenComponent<RadzenDropDown<string>>(10);
                    fieldBuilder.AddAttribute(11, "Placeholder", "Selecciona una opción");
                    fieldBuilder.AddAttribute(12, "Data", GetFieldOptions(field));
                    fieldBuilder.AddAttribute(13, "TextProperty", "Label");
                    fieldBuilder.AddAttribute(14, "ValueProperty", "Value");
                    fieldBuilder.AddAttribute(15, "Disabled", true);
                    fieldBuilder.AddAttribute(16, "Style", "width: 100%");
                    fieldBuilder.CloseComponent();
                    break;

                case "date":
                    var dateFormat = field.UIConfig?.Format ?? "dd/MM/yyyy";
                    var sampleDate = DateTime.Now.ToString(dateFormat);
                    fieldBuilder.OpenComponent<RadzenTextBox>(20);
                    fieldBuilder.AddAttribute(21, "Value", sampleDate);
                    fieldBuilder.AddAttribute(22, "Disabled", true);
                    fieldBuilder.AddAttribute(23, "Style", "width: 100%");
                    fieldBuilder.AddAttribute(24, "Placeholder", $"Formato: {dateFormat}");
                    fieldBuilder.CloseComponent();
                    break;

                case "number":
                    var prefix = field.UIConfig?.Prefix ?? "";
                    var suffix = field.UIConfig?.Suffix ?? "";
                    var decimals = field.UIConfig?.DecimalPlaces ?? 0;
                    var formatString = $"N{decimals}";
                    var sampleNumber = (1234.5678m).ToString(formatString);
                    var displayValue = $"{prefix}{sampleNumber}{suffix}";

                    fieldBuilder.OpenComponent<RadzenTextBox>(30);
                    fieldBuilder.AddAttribute(31, "Value", displayValue);
                    fieldBuilder.AddAttribute(32, "Disabled", true);
                    fieldBuilder.AddAttribute(33, "Style", "width: 100%");
                    fieldBuilder.AddAttribute(34, "Placeholder", $"Formato: {formatString}{(!string.IsNullOrEmpty(prefix) || !string.IsNullOrEmpty(suffix) ? $" ({prefix}...{suffix})" : "")}");
                    fieldBuilder.CloseComponent();
                    break;

                case "textarea":
                    fieldBuilder.OpenComponent<RadzenTextArea>(40);
                    fieldBuilder.AddAttribute(41, "Value", "Texto de ejemplo...");
                    fieldBuilder.AddAttribute(42, "Disabled", true);
                    fieldBuilder.AddAttribute(43, "Style", "width: 100%; height: 80px;");
                    fieldBuilder.CloseComponent();
                    break;

                case "multiselect":
                    fieldBuilder.OpenComponent<RadzenListBox<IEnumerable<string>>>(60);
                    fieldBuilder.AddAttribute(61, "Data", GetFieldOptions(field));
                    fieldBuilder.AddAttribute(62, "TextProperty", "Label");
                    fieldBuilder.AddAttribute(63, "ValueProperty", "Value");
                    fieldBuilder.AddAttribute(64, "Multiple", true);
                    fieldBuilder.AddAttribute(65, "Style", "width: 100%; height: 100px;");
                    fieldBuilder.AddAttribute(66, "Disabled", true);
                    fieldBuilder.CloseComponent();
                    break;

                default: // text, email, etc.
                    fieldBuilder.OpenComponent<RadzenTextBox>(50);
                    fieldBuilder.AddAttribute(51, "Value", "Texto de ejemplo");
                    fieldBuilder.AddAttribute(52, "Disabled", true);
                    fieldBuilder.AddAttribute(53, "Style", "width: 100%");
                    fieldBuilder.CloseComponent();
                    break;
            }
        }));
        builder.CloseComponent();
    };

    private object GetFieldOptions(FormFieldLayoutDto field)
    {
        if (field.UIConfig?.Options?.Any() == true)
        {
            return field.UIConfig.Options;
        }

        // Opciones por defecto si no hay configuradas
        return new List<SelectOption>
        {
            new() { Value = "opcion1", Label = "Opción 1" },
            new() { Value = "opcion2", Label = "Opción 2" }
        };
    }
}