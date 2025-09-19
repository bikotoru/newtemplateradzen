using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using global::Forms.Models.DTOs;
using global::Forms.Models.Enums;
using System.Text.Json;
using Radzen.Blazor;
using Radzen;

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
        // Icono basado en el título o tipo de sección usando Material Design Icons
        return section.Title.ToLowerInvariant() switch
        {
            var title when title.Contains("adicional") => "extension",
            var title when title.Contains("personal") => "person",
            var title when title.Contains("contacto") => "contact_mail",
            var title when title.Contains("dirección") || title.Contains("direccion") => "location_on",
            var title when title.Contains("financier") => "attach_money",
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
        var isDisabled = IsReadOnly || field.IsReadOnly;
        var displayName = field.DisplayName + (field.IsRequired ? " *" : "");

        // Para campos boolean, no usar RadzenFormField
        if (field.FieldType.ToLowerInvariant() == "boolean")
        {
            RenderBooleanField(builder, field, value, isDisabled, displayName);
            return;
        }

        // Para otros tipos, usar RadzenFormField
        builder.OpenComponent<RadzenFormField>(0);
        builder.AddAttribute(1, "Text", displayName);
        builder.AddAttribute(2, "Style", "width: 100%");
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(fieldBuilder =>
        {
            switch (field.FieldType.ToLowerInvariant())
            {
                case "textarea":
                    fieldBuilder.OpenComponent<RadzenTextArea>(10);
                    fieldBuilder.AddAttribute(11, "Value", value?.ToString() ?? "");
                    fieldBuilder.AddAttribute(12, "Placeholder", $"Ingrese {field.DisplayName.ToLower()}");
                    fieldBuilder.AddAttribute(13, "Disabled", isDisabled);
                    fieldBuilder.AddAttribute(14, "Rows", 3);
                    fieldBuilder.AddAttribute(15, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
                        await UpdateCustomFieldValue(field.FieldName, e.Value?.ToString())));
                    fieldBuilder.CloseComponent();
                    break;

                case "number":
                    fieldBuilder.OpenComponent<RadzenNumeric<decimal?>>(20);
                    fieldBuilder.AddAttribute(21, "Value", decimal.TryParse(value?.ToString(), out var numValue) ? numValue : (decimal?)null);
                    fieldBuilder.AddAttribute(22, "Placeholder", $"Ingrese {field.DisplayName.ToLower()}");
                    fieldBuilder.AddAttribute(23, "Disabled", isDisabled);
                    fieldBuilder.AddAttribute(24, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, async newValue =>
                        await UpdateCustomFieldValue(field.FieldName, newValue)));
                    fieldBuilder.CloseComponent();
                    break;

                case "date":
                    fieldBuilder.OpenComponent<RadzenDatePicker<DateTime?>>(30);
                    fieldBuilder.AddAttribute(31, "Value", DateTime.TryParse(value?.ToString(), out var dateValue) ? dateValue : (DateTime?)null);
                    fieldBuilder.AddAttribute(32, "Disabled", isDisabled);
                    fieldBuilder.AddAttribute(33, "DateFormat", "dd/MM/yyyy");
                    fieldBuilder.AddAttribute(34, "ValueChanged", EventCallback.Factory.Create<DateTime?>(this, async newValue =>
                        await UpdateCustomFieldValue(field.FieldName, newValue?.ToString("yyyy-MM-dd"))));
                    fieldBuilder.CloseComponent();
                    break;

                case "select":
                    // Obtener opciones del uiConfig
                    var selectOptions = field.UIConfig?.Options?.Where(o => !o.Disabled).ToList() ?? new List<global::Forms.Models.Configurations.SelectOption>();

                    if (selectOptions.Any())
                    {
                        fieldBuilder.OpenComponent<RadzenDropDown<string>>(50);
                        fieldBuilder.AddAttribute(51, "Data", selectOptions);
                        fieldBuilder.AddAttribute(52, "ValueProperty", "Value");
                        fieldBuilder.AddAttribute(53, "TextProperty", "Label");
                        fieldBuilder.AddAttribute(54, "Value", value?.ToString());
                        fieldBuilder.AddAttribute(55, "Placeholder", $"Seleccione {field.DisplayName.ToLower()}");
                        fieldBuilder.AddAttribute(56, "Disabled", isDisabled);
                        fieldBuilder.AddAttribute(57, "AllowClear", true);
                        fieldBuilder.AddAttribute(58, "ValueChanged", EventCallback.Factory.Create<string>(this, async newValue =>
                            await UpdateCustomFieldValue(field.FieldName, newValue)));
                        fieldBuilder.CloseComponent();
                    }
                    else
                    {
                        // Fallback si no hay opciones configuradas
                        fieldBuilder.OpenComponent<RadzenTextBox>(59);
                        fieldBuilder.AddAttribute(591, "Value", value?.ToString() ?? "");
                        fieldBuilder.AddAttribute(592, "Placeholder", "No hay opciones configuradas");
                        fieldBuilder.AddAttribute(593, "Disabled", true);
                        fieldBuilder.CloseComponent();
                    }
                    break;

                default: // text
                    fieldBuilder.OpenComponent<RadzenTextBox>(60);
                    fieldBuilder.AddAttribute(61, "Value", value?.ToString() ?? "");
                    fieldBuilder.AddAttribute(62, "Placeholder", $"Ingrese {field.DisplayName.ToLower()}");
                    fieldBuilder.AddAttribute(63, "Disabled", isDisabled);
                    fieldBuilder.AddAttribute(64, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, async e =>
                        await UpdateCustomFieldValue(field.FieldName, e.Value?.ToString())));
                    fieldBuilder.CloseComponent();
                    break;
            }
        }));
        builder.CloseComponent();
    };

    private void RenderBooleanField(RenderTreeBuilder builder, FormFieldLayoutDto field, object? value, bool isDisabled, string displayName)
    {
        var boolValue = bool.TryParse(value?.ToString(), out var parsedBool) && parsedBool;
        var trueLabel = field.UIConfig?.TrueLabel ?? "Verdadero";
        var falseLabel = field.UIConfig?.FalseLabel ?? "Falso";
        var currentValue = boolValue ? trueLabel : falseLabel;

        // Layout personalizado sin RadzenFormField
        builder.OpenComponent<RadzenStack>(0);
        builder.AddAttribute(1, "Orientation", Orientation.Horizontal);
        builder.AddAttribute(2, "AlignItems", AlignItems.Center);
        builder.AddAttribute(3, "Gap", "1rem");
        builder.AddAttribute(4, "Style", "width: 100%; padding: 0.75rem 0; border-bottom: 1px solid #f3f4f6;");
        builder.AddAttribute(5, "ChildContent", (RenderFragment)(stackBuilder =>
        {
            // Label del campo
            stackBuilder.OpenComponent<RadzenText>(10);
            stackBuilder.AddAttribute(11, "TextStyle", TextStyle.Body1);
            stackBuilder.AddAttribute(12, "Style", "font-weight: 500; min-width: 140px; color: #374151;");
            stackBuilder.AddAttribute(13, "Text", displayName);
            stackBuilder.CloseComponent();

            // Switch
            stackBuilder.OpenComponent<RadzenSwitch>(20);
            stackBuilder.AddAttribute(21, "Value", boolValue);
            stackBuilder.AddAttribute(22, "Name", field.FieldName);
            stackBuilder.AddAttribute(23, "Disabled", isDisabled);
            stackBuilder.AddAttribute(24, "ValueChanged", EventCallback.Factory.Create<bool>(this, async newValue =>
            {
                await UpdateCustomFieldValue(field.FieldName, newValue);
                StateHasChanged(); // Para actualizar el valor mostrado
            }));
            stackBuilder.CloseComponent();

            // Valor actual
            stackBuilder.OpenComponent<RadzenText>(30);
            stackBuilder.AddAttribute(31, "TextStyle", TextStyle.Body2);
            stackBuilder.AddAttribute(32, "Style", $"color: {(boolValue ? "#10b981" : "#ef4444")}; font-weight: 500; margin-left: 0.5rem;");
            stackBuilder.AddAttribute(33, "Text", currentValue);
            stackBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}