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
    [Inject] public Frontend.Services.AvailableEntitiesService AvailableEntitiesService { get; set; } = default!;

    [Parameter] public string? EntityName { get; set; }
    [Parameter] public string? EntityDisplayName { get; set; }
    [Parameter] public Guid? FieldId { get; set; }

    private int selectedTabIndex = 0;
    private bool isCreating = false;
    private bool createPermissions = true;
    private bool isEditMode = false;
    private bool isLoading = false;

    // Datos del campo actual
    private CreateCustomFieldRequest currentField = new()
    {
        SortOrder = 100,
        IsRequired = false
    };

    private ValidationConfig validationConfig = new();
    private UIConfig uiConfig = new() { Options = new List<SelectOption>() };

    // Datos para dropdowns - se cargan dinámicamente desde system_form_entities
    private List<AvailableEntityOption> availableEntities = new();
    private bool entitiesLoading = false;

    // Campos disponibles para la entidad seleccionada
    private List<FieldOption> availableDisplayFields = new();
    private List<FieldOption> availableValueFields = new();
    private bool fieldsLoading = false;
    private string lastLoadedEntity = "";

    private List<FieldTypeOption> availableFieldTypes = new()
    {
        new() { Value = "text", DisplayName = "Texto", Description = "Campo de texto corto", Icon = "text_fields" },
        new() { Value = "textarea", DisplayName = "Área de Texto", Description = "Campo de texto largo", Icon = "notes" },
        new() { Value = "number", DisplayName = "Número", Description = "Campo numérico", Icon = "pin" },
        new() { Value = "date", DisplayName = "Fecha", Description = "Selector de fecha", Icon = "calendar_today" },
        new() { Value = "boolean", DisplayName = "Sí/No", Description = "Campo verdadero/falso", Icon = "toggle_on" },
        new() { Value = "select", DisplayName = "Lista", Description = "Lista desplegable", Icon = "list" },
        new() { Value = "multiselect", DisplayName = "Selección Múltiple", Description = "Lista de selección múltiple", Icon = "checklist" },
        new() { Value = "entity_reference", DisplayName = "Referencia Entidad", Description = "Referencia a otra entidad", Icon = "link" },
        new() { Value = "user_reference", DisplayName = "Referencia Usuario", Description = "Referencia a usuario", Icon = "person" },
        new() { Value = "file_reference", DisplayName = "Referencia Archivo", Description = "Referencia a archivo", Icon = "attach_file" }
    };

    protected override async Task OnInitializedAsync()
    {
        // Cargar entidades disponibles primero
        await LoadAvailableEntities();

        // Determinar si estamos en modo edición
        isEditMode = FieldId.HasValue;

        if (isEditMode && FieldId.HasValue)
        {
            // Cargar el campo existente para edición
            await LoadExistingField(FieldId.Value);
        }
        else
        {
            // Configurar la entidad basada en los parámetros recibidos para modo creación
            if (!string.IsNullOrEmpty(EntityName))
            {
                currentField.EntityName = EntityName;
            }
            else if (string.IsNullOrEmpty(currentField.EntityName))
            {
                currentField.EntityName = availableEntities.FirstOrDefault()?.Value ?? "";
            }
        }
    }

    private async Task LoadExistingField(Guid fieldId)
    {
        try
        {
            isLoading = true;

            var response = await Api.GetAsync<CustomFieldDefinitionDto>($"api/customfielddefinitions/{fieldId}", BackendType.FormBackend);

            if (response.Success && response.Data != null)
            {
                var existingField = response.Data;

                // Cargar datos en el formulario
                currentField = new CreateCustomFieldRequest
                {
                    EntityName = existingField.EntityName,
                    FieldName = existingField.FieldName,
                    DisplayName = existingField.DisplayName,
                    Description = existingField.Description,
                    FieldType = existingField.FieldType,
                    IsRequired = existingField.IsRequired,
                    DefaultValue = existingField.DefaultValue,
                    SortOrder = existingField.SortOrder,
                    OrganizationId = existingField.OrganizationId
                };

                // Cargar configuraciones
                validationConfig = existingField.ValidationConfig ?? new ValidationConfig();
                uiConfig = existingField.UIConfig ?? new UIConfig { Options = new List<SelectOption>() };

                // Si no hay opciones en UIConfig, inicializar lista vacía
                if (uiConfig.Options == null)
                    uiConfig.Options = new List<SelectOption>();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "No se pudo cargar el campo personalizado.",
                    Duration = 4000
                });

                DialogService.Close();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando campo: {ex.Message}",
                Duration = 4000
            });

            DialogService.Close();
        }
        finally
        {
            isLoading = false;
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

            if (isEditMode && FieldId.HasValue)
            {
                // Modo edición - usar PUT
                await UpdateField();
            }
            else
            {
                // Modo creación - usar POST
                await CreateNewField();
            }
        }
        finally
        {
            isCreating = false;
        }
    }

    private async Task CreateNewField()
    {
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
            DialogService.Close();
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

    private async Task UpdateField()
    {
        // Preparar la request de actualización
        var updateRequest = new UpdateCustomFieldRequest
        {
            DisplayName = currentField.DisplayName,
            Description = currentField.Description,
            IsRequired = currentField.IsRequired,
            DefaultValue = currentField.DefaultValue,
            SortOrder = currentField.SortOrder,
            ValidationConfig = validationConfig,
            UIConfig = uiConfig,
            IsEnabled = true
        };

        // Llamar a la API
        var response = await Api.PutAsync<CustomFieldDefinitionDto>($"api/customfielddefinitions/{FieldId.Value}", updateRequest, BackendType.FormBackend);

        if (response.Success)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Campo Actualizado",
                Detail = $"El campo '{currentField.DisplayName}' se ha actualizado exitosamente.",
                Duration = 4000
            });

            DialogService.Close();
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"No se pudo actualizar el campo: {response.Message}",
                Duration = 4000
            });
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
            EntityName = availableEntities.FirstOrDefault()?.Value ?? "",
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

    private bool HasReferenceConfig()
    {
        return currentField.FieldType is "entity_reference" or "user_reference" or "file_reference";
    }

    private global::Forms.Models.Configurations.ReferenceConfig GetReferenceConfig()
    {
        // Asegurar que UIConfig existe
        if (uiConfig.ReferenceConfig == null)
        {
            uiConfig.ReferenceConfig = new global::Forms.Models.Configurations.ReferenceConfig
            {
                TargetEntity = "",
                DisplayProperty = "Name",
                ValueProperty = "Id",
                AllowMultiple = false,
                AllowCreate = true,
                AllowClear = true,
                EnableCache = true,
                CacheTTLMinutes = 5
            };
        }

        return uiConfig.ReferenceConfig;
    }

    /// <summary>
    /// Cargar entidades disponibles desde la API
    /// </summary>
    private async Task LoadAvailableEntities()
    {
        try
        {
            entitiesLoading = true;
            StateHasChanged();

            var response = await AvailableEntitiesService.GetAvailableEntitiesAsync();

            if (response.Success && response.Data.Any())
            {
                availableEntities = response.Data.Select(e => new AvailableEntityOption
                {
                    Value = e.EntityName,
                    Text = e.DisplayName,
                    Description = e.Description,
                    Category = e.Category,
                    Icon = e.IconName
                }).ToList();

                Console.WriteLine($"[CustomFieldDesigner] Loaded {availableEntities.Count} available entities from API");
            }
            else
            {
                // Fallback a entidades básicas si falla la API
                availableEntities = new List<AvailableEntityOption>
                {
                    new() { Value = "Region", Text = "Región", Description = "Región geográfica" },
                    new() { Value = "SystemUsers", Text = "Usuarios del Sistema", Description = "Usuarios del sistema" }
                };

                Console.WriteLine($"[CustomFieldDesigner] API failed, using fallback entities: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomFieldDesigner] Error loading entities: {ex.Message}");

            // Fallback en caso de error
            availableEntities = new List<AvailableEntityOption>
            {
                new() { Value = "Region", Text = "Región", Description = "Región geográfica" },
                new() { Value = "SystemUsers", Text = "Usuarios del Sistema", Description = "Usuarios del sistema" }
            };
        }
        finally
        {
            entitiesLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Cargar campos disponibles para la entidad seleccionada
    /// </summary>
    private async Task LoadEntityFields(string entityName)
    {
        if (string.IsNullOrEmpty(entityName) || entityName == lastLoadedEntity)
            return;

        try
        {
            fieldsLoading = true;
            lastLoadedEntity = entityName;
            StateHasChanged();

            Console.WriteLine($"[CustomFieldDesigner] Loading fields for entity: {entityName}");

            var response = await AvailableEntitiesService.GetEntityFieldsAsync(entityName);

            if (response.Success && response.Data != null)
            {
                // Combinar campos de tabla y custom fields para display
                var displayFields = new List<FieldOption>();

                // Agregar campos de tabla (excluyendo Id y campos de sistema)
                foreach (var field in response.Data.TableFields.Where(f =>
                    f.FieldName != "Id" &&
                    !f.FieldName.EndsWith("Id") &&
                    f.FieldName != "Active" &&
                    f.FieldName != "FechaCreacion" &&
                    f.FieldName != "FechaModificacion"))
                {
                    displayFields.Add(new FieldOption
                    {
                        Value = field.FieldName,
                        Text = field.DisplayName,
                        Description = $"Campo de tabla ({field.DataType})",
                        FieldType = "table",
                        DataType = field.DataType
                    });
                }

                // Agregar campos custom
                foreach (var field in response.Data.CustomFields)
                {
                    displayFields.Add(new FieldOption
                    {
                        Value = field.FieldName,
                        Text = field.DisplayName,
                        Description = $"Campo personalizado ({field.FieldType})",
                        FieldType = "custom",
                        DataType = field.FieldType
                    });
                }

                availableDisplayFields = displayFields.OrderBy(f => f.Text).ToList();

                // Para campos de valor, incluir solo Id y campos únicos
                var valueFields = new List<FieldOption>
                {
                    new() { Value = "Id", Text = "ID", Description = "Identificador único", FieldType = "table", DataType = "uniqueidentifier" }
                };

                // Agregar otros campos de tabla que puedan servir como identificadores
                foreach (var field in response.Data.TableFields.Where(f =>
                    f.FieldName != "Id" &&
                    (f.FieldName.EndsWith("Id") || f.DataType == "uniqueidentifier")))
                {
                    valueFields.Add(new FieldOption
                    {
                        Value = field.FieldName,
                        Text = field.DisplayName,
                        Description = $"Campo identificador ({field.DataType})",
                        FieldType = "table",
                        DataType = field.DataType
                    });
                }

                availableValueFields = valueFields;

                // Auto-configurar valores por defecto
                var referenceConfig = GetReferenceConfig();
                if (string.IsNullOrEmpty(referenceConfig.DisplayProperty) && availableDisplayFields.Any())
                {
                    referenceConfig.DisplayProperty = availableDisplayFields.First().Value;
                }
                referenceConfig.ValueProperty = "Id"; // Siempre Id por defecto

                Console.WriteLine($"[CustomFieldDesigner] Loaded {availableDisplayFields.Count} display fields and {availableValueFields.Count} value fields for {entityName}");
            }
            else
            {
                // Fallback en caso de error
                availableDisplayFields = new List<FieldOption>
                {
                    new() { Value = "Name", Text = "Nombre", Description = "Campo de nombre por defecto", FieldType = "fallback", DataType = "string" }
                };
                availableValueFields = new List<FieldOption>
                {
                    new() { Value = "Id", Text = "ID", Description = "Identificador único", FieldType = "table", DataType = "uniqueidentifier" }
                };

                Console.WriteLine($"[CustomFieldDesigner] Failed to load fields for {entityName}: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomFieldDesigner] Error loading fields for {entityName}: {ex.Message}");

            // Fallback en caso de error
            availableDisplayFields = new List<FieldOption>
            {
                new() { Value = "Name", Text = "Nombre", Description = "Campo de nombre por defecto", FieldType = "fallback", DataType = "string" }
            };
            availableValueFields = new List<FieldOption>
            {
                new() { Value = "Id", Text = "ID", Description = "Identificador único", FieldType = "table", DataType = "uniqueidentifier" }
            };
        }
        finally
        {
            fieldsLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Manejar cambio de entidad objetivo
    /// </summary>
    private async Task OnTargetEntityChanged(string newEntityName)
    {
        if (!string.IsNullOrEmpty(newEntityName))
        {
            await LoadEntityFields(newEntityName);
        }
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

    public class AvailableEntityOption
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Icon { get; set; }
    }

    public class FieldOption
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
        public string? Description { get; set; }
        public string FieldType { get; set; } = ""; // "table", "custom", "fallback"
        public string DataType { get; set; } = "";
    }

    public class PermissionPreview
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    #endregion
}