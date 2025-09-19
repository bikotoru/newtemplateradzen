using Microsoft.AspNetCore.Components;
using global::Forms.Models.DTOs;
using global::Forms.Models.Enums;
using System.Text.Json;
using Radzen.Blazor;

namespace Frontend.Components.Forms;

public partial class CustomFieldsTab : ComponentBase
{
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;

    [Parameter] public string EntityName { get; set; } = "";
    [Parameter] public string? CustomFieldsValue { get; set; }
    [Parameter] public EventCallback<string?> CustomFieldsValueChanged { get; set; }
    [Parameter] public bool IsReadOnly { get; set; } = false;

    private FormLayoutDto? formLayout;
    private Dictionary<string, object?> customFieldValues = new();
    private bool isLoading = false;
    private bool hasCustomFields = false;
    public List<string> ValidationErrors { get; private set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomFieldsLayout();
        LoadCustomFieldsValues();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(CustomFieldsValue))
        {
            LoadCustomFieldsValues();
        }
    }

    private async Task LoadCustomFieldsLayout()
    {
        if (string.IsNullOrEmpty(EntityName)) return;

        try
        {
            isLoading = true;
            StateHasChanged();

            var apiService = ServiceProvider.GetRequiredService<Frontend.Services.API>();
            var response = await apiService.GetAsync<FormLayoutDto>($"api/form-designer/formulario/layout/{EntityName}", BackendType.FormBackend);

            if (response.Success && response.Data != null)
            {
                formLayout = response.Data;
                hasCustomFields = formLayout.Sections.Any(s => s.Fields.Any(f => !f.IsSystemField));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomFieldsTab] Error loading layout: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void LoadCustomFieldsValues()
    {
        customFieldValues.Clear();

        if (!string.IsNullOrEmpty(CustomFieldsValue))
        {
            try
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, object?>>(CustomFieldsValue);
                if (values != null)
                {
                    customFieldValues = values;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CustomFieldsTab] Error deserializing custom fields: {ex.Message}");
            }
        }
    }

    private async Task UpdateCustomFieldValue(string fieldName, object? value)
    {
        if (IsReadOnly) return;

        customFieldValues[fieldName] = value;

        try
        {
            var json = JsonSerializer.Serialize(customFieldValues);
            await CustomFieldsValueChanged.InvokeAsync(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomFieldsTab] Error serializing custom fields: {ex.Message}");
        }
    }

    private object? GetFieldValue(string fieldName)
    {
        return customFieldValues.TryGetValue(fieldName, out var value) ? value : null;
    }

    private string GetSectionIcon(FormSectionDto section)
    {
        // Icono basado en el título o tipo de sección
        return section.Title.ToLowerInvariant() switch
        {
            var title when title.Contains("adicional") => "plus-circle",
            var title when title.Contains("personal") => "user",
            var title when title.Contains("contacto") => "envelope",
            var title when title.Contains("dirección") || title.Contains("direccion") => "map-marker",
            var title when title.Contains("financier") => "dollar-sign",
            _ => "folder"
        };
    }

    public bool IsValid()
    {
        ValidationErrors.Clear();

        if (formLayout?.Sections == null) return true;

        foreach (var section in formLayout.Sections)
        {
            foreach (var field in section.Fields.Where(f => !f.IsSystemField && f.IsVisible && f.IsRequired))
            {
                var value = GetFieldValue(field.FieldName);

                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    ValidationErrors.Add($"{field.DisplayName} es obligatorio");
                }
            }
        }

        return !ValidationErrors.Any();
    }

    private RenderFragment RenderCustomField(FormFieldLayoutDto field) => builder =>
    {
        var value = GetFieldValue(field.FieldName);
        var fieldId = $"custom_{field.FieldName}";
        var isDisabled = IsReadOnly || field.IsReadOnly;

        // Label
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "form-group");
        builder.AddAttribute(2, "style", "margin-bottom: 1rem;");

        builder.OpenElement(10, "label");
        builder.AddAttribute(11, "for", fieldId);
        builder.AddAttribute(12, "style", "display: block; margin-bottom: 0.5rem; font-weight: 500;");
        builder.AddContent(13, field.DisplayName);
        if (field.IsRequired)
        {
            builder.OpenElement(14, "span");
            builder.AddAttribute(15, "style", "color: red; margin-left: 4px;");
            builder.AddContent(16, "*");
            builder.CloseElement();
        }
        builder.CloseElement(); // label

        // Input field
        switch (field.FieldType.ToLowerInvariant())
        {
            case "textarea":
                builder.OpenElement(20, "textarea");
                builder.AddAttribute(21, "id", fieldId);
                builder.AddAttribute(22, "class", "form-control");
                builder.AddAttribute(23, "placeholder", $"Ingrese {field.DisplayName.ToLower()}");
                builder.AddAttribute(24, "rows", 3);
                builder.AddAttribute(25, "disabled", isDisabled);
                builder.AddAttribute(26, "value", value?.ToString() ?? "");
                builder.AddAttribute(27, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
                    await UpdateCustomFieldValue(field.FieldName, e.Value?.ToString())));
                builder.CloseElement();
                break;

            case "number":
                builder.OpenElement(30, "input");
                builder.AddAttribute(31, "type", "number");
                builder.AddAttribute(32, "id", fieldId);
                builder.AddAttribute(33, "class", "form-control");
                builder.AddAttribute(34, "placeholder", $"Ingrese {field.DisplayName.ToLower()}");
                builder.AddAttribute(35, "disabled", isDisabled);
                builder.AddAttribute(36, "value", value?.ToString() ?? "");
                builder.AddAttribute(37, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
                    await UpdateCustomFieldValue(field.FieldName, e.Value?.ToString())));
                builder.CloseElement();
                break;

            case "date":
                builder.OpenElement(40, "input");
                builder.AddAttribute(41, "type", "date");
                builder.AddAttribute(42, "id", fieldId);
                builder.AddAttribute(43, "class", "form-control");
                builder.AddAttribute(44, "disabled", isDisabled);
                var dateValue = DateTime.TryParse(value?.ToString(), out var date) ? date.ToString("yyyy-MM-dd") : "";
                builder.AddAttribute(45, "value", dateValue);
                builder.AddAttribute(46, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
                    await UpdateCustomFieldValue(field.FieldName, e.Value?.ToString())));
                builder.CloseElement();
                break;

            case "boolean":
                builder.OpenElement(50, "div");
                builder.AddAttribute(51, "class", "form-check");
                builder.OpenElement(52, "input");
                builder.AddAttribute(53, "type", "checkbox");
                builder.AddAttribute(54, "id", fieldId);
                builder.AddAttribute(55, "class", "form-check-input");
                builder.AddAttribute(56, "disabled", isDisabled);
                var boolValue = bool.TryParse(value?.ToString(), out var b) && b;
                builder.AddAttribute(57, "checked", boolValue);
                builder.AddAttribute(58, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
                    await UpdateCustomFieldValue(field.FieldName, e.Value?.ToString())));
                builder.CloseElement();
                builder.CloseElement();
                break;

            default: // text
                builder.OpenElement(60, "input");
                builder.AddAttribute(61, "type", "text");
                builder.AddAttribute(62, "id", fieldId);
                builder.AddAttribute(63, "class", "form-control");
                builder.AddAttribute(64, "placeholder", $"Ingrese {field.DisplayName.ToLower()}");
                builder.AddAttribute(65, "disabled", isDisabled);
                builder.AddAttribute(66, "value", value?.ToString() ?? "");
                builder.AddAttribute(67, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
                    await UpdateCustomFieldValue(field.FieldName, e.Value?.ToString())));
                builder.CloseElement();
                break;
        }

        builder.CloseElement(); // div form-group
    };
}