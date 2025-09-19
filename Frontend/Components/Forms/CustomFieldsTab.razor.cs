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

        // Usar RadzenFormField exactamente como en el formulario original
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

                case "boolean":
                    fieldBuilder.OpenComponent<RadzenSwitch>(40);
                    fieldBuilder.AddAttribute(41, "Value", bool.TryParse(value?.ToString(), out var boolValue) && boolValue);
                    fieldBuilder.AddAttribute(42, "Disabled", isDisabled);
                    fieldBuilder.AddAttribute(43, "ValueChanged", EventCallback.Factory.Create<bool>(this, async newValue =>
                        await UpdateCustomFieldValue(field.FieldName, newValue)));
                    fieldBuilder.CloseComponent();
                    break;

                case "select":
                    // Por ahora usamos opciones hardcodeadas para demo, luego se obtendrán de la configuración
                    var selectOptions = new List<string> { "Opción 1", "Opción 2", "Opción 3" };

                    fieldBuilder.OpenComponent<RadzenDropDown<string>>(50);
                    fieldBuilder.AddAttribute(51, "Data", selectOptions);
                    fieldBuilder.AddAttribute(52, "Value", value?.ToString());
                    fieldBuilder.AddAttribute(53, "Placeholder", $"Seleccione {field.DisplayName.ToLower()}");
                    fieldBuilder.AddAttribute(54, "Disabled", isDisabled);
                    fieldBuilder.AddAttribute(55, "AllowClear", true);
                    fieldBuilder.AddAttribute(56, "ValueChanged", EventCallback.Factory.Create<string>(this, async newValue =>
                        await UpdateCustomFieldValue(field.FieldName, newValue)));
                    fieldBuilder.CloseComponent();
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
}