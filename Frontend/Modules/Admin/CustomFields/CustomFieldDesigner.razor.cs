using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using Forms.Models.DTOs;
using Forms.Models.Enums;
using Forms.Models.Configurations;
using System.Text.Json;
using Frontend.Services;

namespace Frontend.Modules.Admin.CustomFields;

public partial class CustomFieldDesigner : ComponentBase
{
    [Inject] public API Api { get; set; } = default!;
    [Inject] public NotificationService NotificationService { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    [Parameter] public string? EntityName { get; set; }
    [Parameter] public string? EntityDisplayName { get; set; }

    private int selectedTabIndex = 0;
    private bool isCreating = false;
    private bool createPermissions = true;

    // Datos del campo actual
    private CreateCustomFieldRequest currentField = new()
    {
        SortOrder = 100,
        IsRequired = false
    };

    private ValidationConfig validationConfig = new();
    private UIConfig uiConfig = new() { Options = new List<SelectOption>() };

    // Datos para dropdowns
    private List<string> availableEntities = new()
    {
        "Empleado",
        "Empresa",
        "Cliente",
        "Proveedor"
    };

    private List<FieldTypeOption> availableFieldTypes = new()
    {
        new() { Value = "text", DisplayName = "Texto", Description = "Campo de texto corto", Icon = "text_fields" },
        new() { Value = "textarea", DisplayName = "Área de Texto", Description = "Campo de texto largo", Icon = "notes" },
        new() { Value = "number", DisplayName = "Número", Description = "Campo numérico", Icon = "pin" },
        new() { Value = "date", DisplayName = "Fecha", Description = "Selector de fecha", Icon = "calendar_today" },
        new() { Value = "boolean", DisplayName = "Sí/No", Description = "Campo verdadero/falso", Icon = "toggle_on" },
        new() { Value = "select", DisplayName = "Lista", Description = "Lista desplegable", Icon = "list" },
        new() { Value = "multiselect", DisplayName = "Selección Múltiple", Description = "Lista de selección múltiple", Icon = "checklist" }
    };

    protected override void OnInitialized()
    {
        // Configurar la entidad basada en los parámetros recibidos
        if (!string.IsNullOrEmpty(EntityName))
        {
            currentField.EntityName = EntityName;
        }
        else if (string.IsNullOrEmpty(currentField.EntityName))
        {
            currentField.EntityName = availableEntities.FirstOrDefault() ?? "";
        }
    }

    #region Navegación entre pasos

    private void NextStep()
    {
        if (selectedTabIndex < 3 && CanContinue())
        {
            selectedTabIndex++;
        }
    }

    private void PreviousStep()
    {
        if (selectedTabIndex > 0)
        {
            selectedTabIndex--;
        }
    }

    private bool CanContinue()
    {
        return selectedTabIndex switch
        {
            0 => !string.IsNullOrEmpty(currentField.EntityName) &&
                 !string.IsNullOrEmpty(currentField.FieldName) &&
                 !string.IsNullOrEmpty(currentField.DisplayName),
            1 => !string.IsNullOrEmpty(currentField.FieldType),
            2 => true, // Configuración es opcional
            3 => true, // Confirmación
            _ => false
        };
    }

    #endregion

    #region Selección de tipo de campo

    private void SelectFieldType(string fieldType)
    {
        currentField.FieldType = fieldType;

        // Limpiar configuraciones anteriores
        validationConfig = new ValidationConfig();
        uiConfig = new UIConfig();

        // Configurar valores por defecto según el tipo
        switch (fieldType)
        {
            case "select":
            case "multiselect":
                uiConfig.Options = new List<SelectOption>();
                break;
            case "textarea":
                uiConfig.Rows = 3;
                break;
            case "boolean":
                uiConfig.Style = "switch";
                uiConfig.TrueLabel = "Sí";
                uiConfig.FalseLabel = "No";
                break;
        }

        StateHasChanged();
    }

    private string GetFieldTypeDisplay()
    {
        var fieldType = availableFieldTypes.FirstOrDefault(f => f.Value == currentField.FieldType);
        return fieldType?.DisplayName ?? currentField.FieldType;
    }

    #endregion

    #region Vista previa del campo

    private RenderFragment RenderFieldPreview()
    {
        if (string.IsNullOrEmpty(currentField.FieldType) || string.IsNullOrEmpty(currentField.DisplayName))
        {
            return builder => { };
        }

        return builder =>
        {
            var fieldType = FieldTypeExtensions.FromString(currentField.FieldType);

            builder.OpenComponent<RadzenFormField>(0);
            builder.AddAttribute(1, "Text", currentField.DisplayName);
            builder.AddAttribute(2, "Variant", Variant.Outlined);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(childBuilder =>
            {
                switch (fieldType)
                {
                    case FieldType.Text:
                        childBuilder.OpenComponent<RadzenTextBox>(0);
                        childBuilder.AddAttribute(1, "Placeholder", uiConfig.Placeholder ?? "");
                        childBuilder.AddAttribute(2, "Style", "width: 100%;");
                        childBuilder.AddAttribute(3, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.TextArea:
                        childBuilder.OpenComponent<RadzenTextArea>(0);
                        childBuilder.AddAttribute(1, "Placeholder", uiConfig.Placeholder ?? "");
                        childBuilder.AddAttribute(2, "Rows", uiConfig.Rows ?? 3);
                        childBuilder.AddAttribute(3, "Style", "width: 100%;");
                        childBuilder.AddAttribute(4, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.Number:
                        childBuilder.OpenComponent<RadzenNumeric<decimal?>>(0);
                        childBuilder.AddAttribute(1, "Placeholder", uiConfig.Placeholder ?? "");
                        childBuilder.AddAttribute(2, "Style", "width: 100%;");
                        childBuilder.AddAttribute(3, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.Date:
                        childBuilder.OpenComponent<RadzenDatePicker<DateTime?>>(0);
                        childBuilder.AddAttribute(1, "Placeholder", uiConfig.Placeholder ?? "Selecciona una fecha");
                        childBuilder.AddAttribute(2, "Style", "width: 100%;");
                        childBuilder.AddAttribute(3, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.Boolean:
                        if (uiConfig.Style == "switch")
                        {
                            childBuilder.OpenComponent<RadzenSwitch>(0);
                            childBuilder.AddAttribute(1, "Disabled", true);
                            childBuilder.CloseComponent();
                        }
                        else
                        {
                            childBuilder.OpenComponent<RadzenCheckBox<bool>>(0);
                            childBuilder.AddAttribute(1, "Disabled", true);
                            childBuilder.CloseComponent();
                        }
                        break;

                    case FieldType.Select:
                        childBuilder.OpenComponent<RadzenDropDown<string>>(0);
                        childBuilder.AddAttribute(1, "Placeholder", uiConfig.Placeholder ?? "Selecciona una opción");
                        childBuilder.AddAttribute(2, "Data", uiConfig.Options?.Select(o => o.Value) ?? new List<string>());
                        childBuilder.AddAttribute(3, "Style", "width: 100%;");
                        childBuilder.AddAttribute(4, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;

                    case FieldType.MultiSelect:
                        childBuilder.OpenComponent<RadzenListBox<IEnumerable<string>>>(0);
                        childBuilder.AddAttribute(1, "Data", uiConfig.Options?.Select(o => o.Value) ?? new List<string>());
                        childBuilder.AddAttribute(2, "Multiple", true);
                        childBuilder.AddAttribute(3, "Style", "width: 100%;");
                        childBuilder.AddAttribute(4, "Disabled", true);
                        childBuilder.CloseComponent();
                        break;
                }
            }));
            builder.CloseComponent();

            // Mostrar texto de ayuda si existe
            if (!string.IsNullOrEmpty(uiConfig.HelpText))
            {
                builder.OpenComponent<RadzenText>(10);
                builder.AddAttribute(11, "TextStyle", TextStyle.Caption);
                builder.AddAttribute(12, "class", "rz-mt-2");
                builder.AddAttribute(13, "ChildContent", (RenderFragment)(textBuilder =>
                {
                    textBuilder.AddContent(0, uiConfig.HelpText);
                }));
                builder.CloseComponent();
            }
        };
    }

    #endregion

    #region Configuración de validaciones y UI

    private bool IsTextType() => currentField.FieldType == "text" || currentField.FieldType == "textarea";
    private bool IsNumericType() => currentField.FieldType == "number";
    private bool HasSelectOptions() => currentField.FieldType == "select" || currentField.FieldType == "multiselect";

    private void AddSelectOption()
    {
        uiConfig.Options ??= new List<SelectOption>();
        uiConfig.Options.Add(new SelectOption { Value = "", Label = "" });
        StateHasChanged();
    }

    private void RemoveSelectOption(SelectOption option)
    {
        uiConfig.Options?.Remove(option);
        StateHasChanged();
    }

    #endregion

    #region Permisos

    private List<PermissionPreview> GetPermissionsPreview()
    {
        if (string.IsNullOrEmpty(currentField.EntityName) || string.IsNullOrEmpty(currentField.FieldName))
        {
            return new List<PermissionPreview>();
        }

        return new List<PermissionPreview>
        {
            new()
            {
                Name = $"{currentField.EntityName}.{currentField.FieldName}.VIEW",
                Description = $"Ver campo '{currentField.DisplayName}' en {currentField.EntityName}",
                Icon = "visibility"
            },
            new()
            {
                Name = $"{currentField.EntityName}.{currentField.FieldName}.CREATE",
                Description = $"Crear valores para campo '{currentField.DisplayName}' en {currentField.EntityName}",
                Icon = "add"
            },
            new()
            {
                Name = $"{currentField.EntityName}.{currentField.FieldName}.UPDATE",
                Description = $"Actualizar valores del campo '{currentField.DisplayName}' en {currentField.EntityName}",
                Icon = "edit"
            }
        };
    }

    #endregion

    #region Creación del campo

    private async Task CreateField()
    {
        try
        {
            isCreating = true;

            // Preparar la request
            var request = new CreateCustomFieldRequest
            {
                EntityName = currentField.EntityName,
                FieldName = currentField.FieldName,
                DisplayName = currentField.DisplayName,
                Description = currentField.Description,
                FieldType = currentField.FieldType,
                IsRequired = currentField.IsRequired,
                DefaultValue = currentField.DefaultValue,
                SortOrder = currentField.SortOrder,
                ValidationConfig = validationConfig,
                UIConfig = uiConfig,
                OrganizationId = currentField.OrganizationId
            };

            // Llamar a la API
            var response = await Api.PostAsync<CustomFieldDefinitionDto>("api/customfielddefinitions", request, BackendType.FormBackend);

            if (response.Success)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Campo Creado",
                    Detail = $"El campo '{currentField.DisplayName}' se ha creado exitosamente con permisos automáticos.",
                    Duration = 4000
                });

                // Limpiar formulario
                ResetForm();
                selectedTabIndex = 0;
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"No se pudo crear el campo: {response.Message}",
                    Duration = 4000
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error inesperado: {ex.Message}",
                Duration = 4000
            });
        }
        finally
        {
            isCreating = false;
        }
    }

    private void Cancel()
    {
        ResetForm();
        selectedTabIndex = 0;
    }

    private void ResetForm()
    {
        currentField = new CreateCustomFieldRequest
        {
            EntityName = availableEntities.FirstOrDefault() ?? "",
            SortOrder = 100,
            IsRequired = false
        };
        validationConfig = new ValidationConfig();
        uiConfig = new UIConfig { Options = new List<SelectOption>() };
        createPermissions = true;
    }

    private string GetFieldTypeCardClass(string fieldTypeValue)
    {
        return $"field-type-card {(currentField.FieldType == fieldTypeValue ? "selected" : "")}";
    }

    private string GetFieldTypeIconStyle(string fieldTypeValue)
    {
        var color = currentField.FieldType == fieldTypeValue ? "var(--rz-primary)" : "var(--rz-text-color)";
        return $"font-size: 2.5rem; color: {color};";
    }

    #endregion

    #region Métodos auxiliares

    private void OnFieldNameChanged(ChangeEventArgs e)
    {
        currentField.FieldName = e.Value?.ToString() ?? "";
        StateHasChanged();
    }

    private string GetFullFieldName()
    {
        if (string.IsNullOrEmpty(currentField.FieldName) || string.IsNullOrEmpty(EntityName))
            return "";

        return $"{EntityName}.CustomField.{currentField.FieldName}";
    }

    #endregion

    #region Clases auxiliares

    public class FieldTypeOption
    {
        public string Value { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class PermissionPreview
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    #endregion
}