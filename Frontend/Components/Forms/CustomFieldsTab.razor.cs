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
    [Inject] private Frontend.Services.EntityRegistrationService EntityRegistrationService { get; set; } = null!;

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
                    var prefix = field.UIConfig?.Prefix ?? "";
                    var suffix = field.UIConfig?.Suffix ?? "";
                    var decimals = field.UIConfig?.DecimalPlaces ?? 2;
                    var placeholder = $"Ingrese {field.DisplayName.ToLower()}";

                    // Si tiene prefijo o sufijo, crear un contenedor visual
                    if (!string.IsNullOrEmpty(prefix) || !string.IsNullOrEmpty(suffix))
                    {
                        fieldBuilder.OpenComponent<RadzenStack>(20);
                        fieldBuilder.AddAttribute(21, "Orientation", Orientation.Horizontal);
                        fieldBuilder.AddAttribute(22, "AlignItems", AlignItems.Stretch);
                        fieldBuilder.AddAttribute(23, "Gap", "0");
                        fieldBuilder.AddAttribute(24, "Style", "border: 1px solid var(--rz-border-color); border-radius: 4px; overflow: hidden;");
                        fieldBuilder.AddAttribute(25, "ChildContent", (RenderFragment)(stackBuilder =>
                        {
                            // Prefijo
                            if (!string.IsNullOrEmpty(prefix))
                            {
                                stackBuilder.OpenComponent<RadzenText>(251);
                                stackBuilder.AddAttribute(252, "Style", "background: var(--rz-base-200); padding: 0.75rem; border-right: 1px solid var(--rz-border-color); font-weight: 500; display: flex; align-items: center;");
                                stackBuilder.AddAttribute(253, "Text", prefix);
                                stackBuilder.CloseComponent();
                            }

                            // Campo numérico
                            stackBuilder.OpenComponent<RadzenNumeric<decimal?>>(26);
                            stackBuilder.AddAttribute(261, "Value", decimal.TryParse(value?.ToString(), out var numValue) ? numValue : (decimal?)null);
                            stackBuilder.AddAttribute(262, "Placeholder", placeholder);
                            stackBuilder.AddAttribute(263, "Disabled", isDisabled);
                            stackBuilder.AddAttribute(264, "Format", $"N{decimals}");
                            stackBuilder.AddAttribute(265, "Style", "border: none; flex: 1;");
                            stackBuilder.AddAttribute(266, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, async newValue =>
                                await UpdateCustomFieldValue(field.FieldName, newValue)));
                            stackBuilder.CloseComponent();

                            // Sufijo
                            if (!string.IsNullOrEmpty(suffix))
                            {
                                stackBuilder.OpenComponent<RadzenText>(27);
                                stackBuilder.AddAttribute(271, "Style", "background: var(--rz-base-200); padding: 0.75rem; border-left: 1px solid var(--rz-border-color); font-weight: 500; display: flex; align-items: center;");
                                stackBuilder.AddAttribute(272, "Text", suffix);
                                stackBuilder.CloseComponent();
                            }
                        }));
                        fieldBuilder.CloseComponent();
                    }
                    else
                    {
                        // Campo numérico simple sin prefijo/sufijo
                        fieldBuilder.OpenComponent<RadzenNumeric<decimal?>>(28);
                        fieldBuilder.AddAttribute(281, "Value", decimal.TryParse(value?.ToString(), out var numValue) ? numValue : (decimal?)null);
                        fieldBuilder.AddAttribute(282, "Placeholder", placeholder);
                        fieldBuilder.AddAttribute(283, "Disabled", isDisabled);
                        fieldBuilder.AddAttribute(284, "Format", $"N{decimals}");
                        fieldBuilder.AddAttribute(285, "Style", "width: 100%;");
                        fieldBuilder.AddAttribute(286, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, async newValue =>
                            await UpdateCustomFieldValue(field.FieldName, newValue)));
                        fieldBuilder.CloseComponent();
                    }
                    break;

                case "date":
                    var dateFormat = field.UIConfig?.Format ?? "dd/MM/yyyy";
                    var showTime = dateFormat.Contains("HH:mm");

                    fieldBuilder.OpenComponent<RadzenDatePicker<DateTime?>>(30);
                    fieldBuilder.AddAttribute(31, "Value", DateTime.TryParse(value?.ToString(), out var dateValue) ? dateValue : (DateTime?)null);
                    fieldBuilder.AddAttribute(32, "Disabled", isDisabled);
                    fieldBuilder.AddAttribute(33, "DateFormat", dateFormat);
                    fieldBuilder.AddAttribute(34, "ShowTime", showTime);
                    fieldBuilder.AddAttribute(35, "ShowSeconds", dateFormat.Contains("ss"));
                    fieldBuilder.AddAttribute(36, "HourFormat", "24");
                    fieldBuilder.AddAttribute(37, "ValueChanged", EventCallback.Factory.Create<DateTime?>(this, async newValue =>
                    {
                        var formattedValue = newValue?.ToString("yyyy-MM-dd" + (showTime ? " HH:mm:ss" : ""));
                        await UpdateCustomFieldValue(field.FieldName, formattedValue);
                    }));
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

                case "multiselect":
                    // Obtener opciones del uiConfig
                    var multiSelectOptions = field.UIConfig?.Options?.Where(o => !o.Disabled).ToList() ?? new List<global::Forms.Models.Configurations.SelectOption>();

                    if (multiSelectOptions.Any())
                    {
                        // Parsear valor actual como lista de strings
                        var currentValues = new List<string>();
                        if (value != null)
                        {
                            try
                            {
                                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                                {
                                    currentValues = jsonElement.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                                }
                                else if (value is string strValue && !string.IsNullOrEmpty(strValue))
                                {
                                    currentValues = JsonSerializer.Deserialize<List<string>>(strValue) ?? new List<string>();
                                }
                            }
                            catch
                            {
                                currentValues = new List<string>();
                            }
                        }

                        fieldBuilder.OpenComponent<RadzenListBox<IEnumerable<string>>>(70);
                        fieldBuilder.AddAttribute(71, "Data", multiSelectOptions);
                        fieldBuilder.AddAttribute(72, "ValueProperty", "Value");
                        fieldBuilder.AddAttribute(73, "TextProperty", "Label");
                        fieldBuilder.AddAttribute(74, "Value", currentValues.AsEnumerable());
                        fieldBuilder.AddAttribute(75, "Multiple", true);
                        fieldBuilder.AddAttribute(76, "Disabled", isDisabled);
                        fieldBuilder.AddAttribute(77, "Style", "width: 100%; height: 120px;");
                        fieldBuilder.AddAttribute(78, "ValueChanged", EventCallback.Factory.Create<IEnumerable<string>>(this, async newValues =>
                        {
                            var serializedValues = JsonSerializer.Serialize(newValues?.ToList() ?? new List<string>());
                            await UpdateCustomFieldValue(field.FieldName, serializedValues);
                        }));
                        fieldBuilder.CloseComponent();
                    }
                    else
                    {
                        // Fallback si no hay opciones configuradas
                        fieldBuilder.OpenComponent<RadzenTextBox>(79);
                        fieldBuilder.AddAttribute(791, "Value", value?.ToString() ?? "");
                        fieldBuilder.AddAttribute(792, "Placeholder", "No hay opciones configuradas");
                        fieldBuilder.AddAttribute(793, "Disabled", true);
                        fieldBuilder.CloseComponent();
                    }
                    break;

                case "entity_reference":
                case "user_reference":
                case "file_reference":
                    RenderReferenceField(fieldBuilder, field, value, isDisabled);
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
        var trueLabel = field.UIConfig?.TrueLabel ?? "Sí";
        var falseLabel = field.UIConfig?.FalseLabel ?? "No";
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

    private void RenderReferenceField(RenderTreeBuilder builder, FormFieldLayoutDto field, object? value, bool isDisabled)
    {
        try
        {
            // Verificaciones null básicas
            if (builder == null || field == null)
            {
                Console.WriteLine("[CustomFieldsTab] RenderReferenceField: builder or field is null");
                return;
            }

            // Configuración de referencia
            var referenceConfig = field.UIConfig?.ReferenceConfig;
            if (referenceConfig == null)
            {
                // Fallback a text box si no hay configuración
                builder.OpenComponent<RadzenTextBox>(100);
                builder.AddAttribute(101, "Value", value?.ToString() ?? "");
                builder.AddAttribute(102, "Placeholder", $"Configure la referencia para {field.DisplayName}");
                builder.AddAttribute(103, "Disabled", true);
                builder.CloseComponent();
                return;
            }

        // Determinar el tipo de valor esperado
        var valueType = GetReferenceValueType(field.FieldType);

        // Render Lookup component
        var lookupType = GetLookupComponentType(referenceConfig.TargetEntity);
        if (lookupType != null)
        {
            builder.OpenComponent(200, lookupType);

            // Obtener configuración correcta desde EntityRegistrationService
            var entityConfig = EntityRegistrationService?.GetEntityConfiguration(referenceConfig.TargetEntity);
            var displayProperty = entityConfig?.DisplayProperty ?? referenceConfig.DisplayProperty ?? "Name";
            var valueProperty = entityConfig?.ValueProperty ?? referenceConfig.ValueProperty ?? "Id";

            Console.WriteLine($"[CustomFieldsTab] Using DisplayProperty: '{displayProperty}' for entity: '{referenceConfig.TargetEntity}'");

            // Configurar propiedades básicas del Lookup
            builder.AddAttribute(201, "Value", GetReferenceValue(value, valueType));
            builder.AddAttribute(202, "ValueChanged", CreateReferenceValueChangedCallback(field.FieldName, valueType));
            builder.AddAttribute(203, "DisplayProperty", displayProperty);
            builder.AddAttribute(204, "ValueProperty", valueProperty);
            builder.AddAttribute(205, "Placeholder", $"Seleccione {field.DisplayName.ToLower()}");
            builder.AddAttribute(206, "AllowClear", referenceConfig.AllowClear);
            builder.AddAttribute(207, "Disabled", isDisabled);
            builder.AddAttribute(208, "ShowAdd", referenceConfig.AllowCreate && !isDisabled);
            builder.AddAttribute(209, "EnableCache", referenceConfig.EnableCache);

            // Configurar cache TTL
            if (referenceConfig.CacheTTLMinutes > 0)
            {
                builder.AddAttribute(210, "CacheTTL", TimeSpan.FromMinutes(referenceConfig.CacheTTLMinutes));
            }

            // Configurar Service específico según la entidad
            var service = GetServiceForEntity(referenceConfig.TargetEntity);
            if (service != null)
            {
                builder.AddAttribute(211, "Service", service);
            }

            builder.CloseComponent();
        }
        else
        {
            // Fallback si no se puede determinar el tipo de lookup
            builder.OpenComponent<RadzenTextBox>(300);
            builder.AddAttribute(301, "Value", value?.ToString() ?? "");
            builder.AddAttribute(302, "Placeholder", $"Referencia a {referenceConfig.TargetEntity}");
            builder.AddAttribute(303, "Disabled", true);
            builder.AddAttribute(304, "Style", "background-color: #f3f4f6;");
            builder.CloseComponent();
        }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomFieldsTab] Error in RenderReferenceField: {ex.Message}");
            // Fallback rendering
            builder.OpenComponent<RadzenTextBox>(400);
            builder.AddAttribute(401, "Value", value?.ToString() ?? "");
            builder.AddAttribute(402, "Placeholder", "Error rendering reference field");
            builder.AddAttribute(403, "Disabled", true);
            builder.CloseComponent();
        }
    }

    private Type GetReferenceValueType(string fieldType)
    {
        return fieldType.ToLowerInvariant() switch
        {
            "entity_reference" => typeof(Guid?),
            "user_reference" => typeof(Guid?),
            "file_reference" => typeof(string), // Puede ser path o ID
            _ => typeof(string)
        };
    }

    private object? GetReferenceValue(object? value, Type expectedType)
    {
        if (value == null) return null;

        try
        {
            if (expectedType == typeof(Guid?) || expectedType == typeof(Guid))
            {
                if (value is Guid guid) return guid;
                if (Guid.TryParse(value.ToString(), out var parsedGuid)) return parsedGuid;
                return null;
            }

            return value.ToString();
        }
        catch
        {
            return null;
        }
    }

    private object CreateReferenceValueChangedCallback(string fieldName, Type valueType)
    {
        if (valueType == typeof(Guid?) || valueType == typeof(Guid))
        {
            return EventCallback.Factory.Create<Guid?>(this, async newValue =>
                await UpdateCustomFieldValue(fieldName, newValue));
        }

        return EventCallback.Factory.Create<string>(this, async newValue =>
            await UpdateCustomFieldValue(fieldName, newValue));
    }

    private Type? GetLookupComponentType(string targetEntity)
    {
        try
        {
            if (EntityRegistrationService == null)
            {
                Console.WriteLine("[CustomFieldsTab] EntityRegistrationService is null");
                return null;
            }

            if (string.IsNullOrEmpty(targetEntity))
            {
                Console.WriteLine("[CustomFieldsTab] targetEntity is null or empty");
                return null;
            }

            // Usar EntityRegistrationService para obtener el tipo de Lookup
            var lookupType = EntityRegistrationService.CreateLookupType(targetEntity);

            if (lookupType != null)
            {
                Console.WriteLine($"[CustomFieldsTab] Successfully created lookup type for '{targetEntity}'");
                return lookupType;
            }

            // Log entidades disponibles para debugging
            var availableEntities = EntityRegistrationService.GetAllEntities();
            var entitiesStr = availableEntities?.Keys != null ? string.Join(", ", availableEntities.Keys) : "none";
            Console.WriteLine($"[CustomFieldsTab] Entity '{targetEntity}' not found. Available entities: {entitiesStr}");

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomFieldsTab] Error getting lookup type for '{targetEntity}': {ex.Message}");
            return null;
        }
    }

    private object? GetServiceForEntity(string targetEntity)
    {
        try
        {
            if (EntityRegistrationService == null)
            {
                Console.WriteLine("[CustomFieldsTab] EntityRegistrationService is null");
                return null;
            }

            if (string.IsNullOrEmpty(targetEntity))
            {
                Console.WriteLine("[CustomFieldsTab] targetEntity is null or empty");
                return null;
            }

            // Usar EntityRegistrationService para obtener el servicio
            var service = EntityRegistrationService.GetEntityService(targetEntity);

            if (service != null)
            {
                Console.WriteLine($"[CustomFieldsTab] Successfully retrieved service for '{targetEntity}'");
                return service;
            }

            // Log entidades disponibles para debugging
            var availableEntities = EntityRegistrationService.GetAllEntities();
            var entitiesStr = availableEntities?.Keys != null ? string.Join(", ", availableEntities.Keys) : "none";
            Console.WriteLine($"[CustomFieldsTab] Service for '{targetEntity}' not found. Available entities: {entitiesStr}");

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomFieldsTab] Error getting service for '{targetEntity}': {ex.Message}");
            return null;
        }
    }
}