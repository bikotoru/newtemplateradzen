using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Forms.Models.DTOs;
using Forms.Models.Enums;
using Frontend.Services;
using Frontend.Modules.Admin.CustomFields;
using Shared.Models.Entities.SystemEntities;
using Shared.Models.Responses;
using System.Text.Json;
using Frontend.Components.Auth;
using Forms.Models.Configurations;

namespace Frontend.Modules.Admin.FormDesigner;

public partial class FormDesigner : AuthorizedPageBase
{
    [Inject] public API Api { get; set; } = default!;
    [Inject] public NotificationService NotificationService { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public string? EntityName { get; set; }

    private GetAvailableFieldsResponse availableFields = new();

    private string currentEntityName = "";
    private string currentEntityDisplayName = "";
    private FormLayoutDto currentLayout = new();

    private FormFieldLayoutDto? selectedField;
    private FormSectionDto? selectedSection;

    private bool isSaving = false;


    // Opciones para dropdowns
    private readonly List<GridSizeOption> gridSizeOptions = new()
    {
        new() { Value = 4, Text = "4 columnas (33%)" },
        new() { Value = 6, Text = "6 columnas (50%)" },
        new() { Value = 8, Text = "8 columnas (66%)" },
        new() { Value = 12, Text = "12 columnas (100%)" }
    };

    private readonly List<GridSizeOption> fieldSizeOptions = new()
    {
        new() { Value = 3, Text = "3 cols (25%)" },
        new() { Value = 4, Text = "4 cols (33%)" },
        new() { Value = 6, Text = "6 cols (50%)" },
        new() { Value = 8, Text = "8 cols (66%)" },
        new() { Value = 12, Text = "12 cols (100%)" }
    };

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(EntityName))
        {
            await SelectEntityByName(EntityName);
        }
        else
        {
            // Redirigir a la lista si no se proporciona EntityName
            Navigation.NavigateTo("/admin/form-designer/list");
        }
    }

    #region Carga de datos

    private async Task SelectEntityByName(string entityName)
    {
        try
        {
            Console.WriteLine($"[FormDesigner] SelectEntityByName iniciado para: {entityName}");

            // Buscar la entidad específica en system_form_entities
            var response = await Api.GetAsync<SystemFormEntities>($"api/form-designer/entities/by-name/{entityName}", BackendType.FormBackend);

            Console.WriteLine($"[FormDesigner] Entity API response.Success: {response.Success}");

            if (response.Success && response.Data != null)
            {
                currentEntityName = response.Data.EntityName;
                currentEntityDisplayName = response.Data.DisplayName;

                Console.WriteLine($"[FormDesigner] currentEntityName set to: {currentEntityName}");
                Console.WriteLine($"[FormDesigner] currentEntityDisplayName set to: {currentEntityDisplayName}");

                await LoadAvailableFields();
                await LoadFormLayout();

                selectedField = null;
                selectedSection = null;
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Entidad no encontrada",
                    Detail = $"No se encontró la entidad '{entityName}' o no tienes permisos para acceder a ella.",
                    Duration = 4000
                });

                // Redirigir a la lista
                Navigation.NavigateTo("/admin/form-designer/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando entidad: {ex.Message}",
                Duration = 4000
            });

            // Redirigir a la lista en caso de error
            Navigation.NavigateTo("/admin/form-designer/list");
        }
    }

    private async Task LoadAvailableFields()
    {
        try
        {
            Console.WriteLine($"[FormDesigner] LoadAvailableFields iniciado para entidad: {currentEntityName}");

            var request = new GetAvailableFieldsRequest
            {
                EntityName = currentEntityName
            };

            Console.WriteLine($"[FormDesigner] Enviando request: {System.Text.Json.JsonSerializer.Serialize(request)}");

            var response = await Api.PostAsync<GetAvailableFieldsResponse>("api/form-designer/formulario/available-fields", request, BackendType.FormBackend);

            Console.WriteLine($"[FormDesigner] Response.Success: {response.Success}");
            Console.WriteLine($"[FormDesigner] Response.Data != null: {response.Data != null}");

            if (response.Success && response.Data != null)
            {
                availableFields = response.Data;
                Console.WriteLine($"[FormDesigner] SystemFields count: {availableFields.SystemFields?.Count ?? 0}");
                Console.WriteLine($"[FormDesigner] CustomFields count: {availableFields.CustomFields?.Count ?? 0}");
                Console.WriteLine($"[FormDesigner] RelatedFields count: {availableFields.RelatedFields?.Count ?? 0}");

                if (availableFields.CustomFields?.Any() == true)
                {
                    Console.WriteLine("[FormDesigner] Custom fields encontrados:");
                    foreach (var field in availableFields.CustomFields)
                    {
                        Console.WriteLine($"[FormDesigner]   - {field.DisplayName} ({field.FieldName}) - Type: {field.FieldType}");
                    }
                }
                else
                {
                    Console.WriteLine("[FormDesigner] No se encontraron custom fields");
                }

                StateHasChanged();
                Console.WriteLine("[FormDesigner] StateHasChanged() ejecutado");
            }
            else
            {
                Console.WriteLine($"[FormDesigner] Response fallida - Success: {response.Success}, Message: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FormDesigner] Exception en LoadAvailableFields: {ex.Message}");
            Console.WriteLine($"[FormDesigner] StackTrace: {ex.StackTrace}");

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando campos disponibles: {ex.Message}",
                Duration = 4000
            });
        }
    }

    private async Task LoadFormLayout()
    {
        try
        {
            var response = await Api.GetAsync<FormLayoutDto>($"api/form-designer/formulario/layout/{currentEntityName}", BackendType.FormBackend);
            if (response.Success && response.Data != null)
            {
                currentLayout = response.Data;
            }
            else
            {
                // Crear layout por defecto
                currentLayout = new FormLayoutDto
                {
                    EntityName = currentEntityName,
                    FormName = $"{currentEntityDisplayName} - Formulario Principal",
                    IsDefault = true,
                    Sections = new List<FormSectionDto>()
                };
            }
        }
        catch (Exception)
        {
            // Crear layout por defecto en caso de error
            currentLayout = new FormLayoutDto
            {
                EntityName = currentEntityName,
                FormName = $"{currentEntityDisplayName} - Formulario Principal",
                IsDefault = true,
                Sections = new List<FormSectionDto>()
            };
        }
    }

    #endregion


    #region Manejo de Secciones

    private async Task AddNewSection()
    {
        var newSection = new FormSectionDto
        {
            Title = $"Sección {currentLayout.Sections.Count + 1}",
            GridSize = 12,
            SortOrder = currentLayout.Sections.Count,
            IsCollapsible = true,
            IsExpanded = true,
            Fields = new List<FormFieldLayoutDto>()
        };

        currentLayout.Sections.Add(newSection);
        selectedSection = newSection;
        selectedField = null;
        StateHasChanged();
    }

    private async Task DeleteSection(FormSectionDto section)
    {
        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas eliminar la sección '{section.Title}'?",
            "Confirmar Eliminación",
            new ConfirmOptions()
            {
                OkButtonText = "Eliminar",
                CancelButtonText = "Cancelar"
            });

        if (confirmed == true)
        {
            currentLayout.Sections.Remove(section);
            if (selectedSection == section) selectedSection = null;
            StateHasChanged();
        }
    }

    private async Task ConfigureSection(FormSectionDto section)
    {
        selectedSection = section;
        selectedField = null;
        StateHasChanged();
    }

    #endregion

    #region Manejo de Campos

    private async Task AddFieldToForm(FormFieldItemDto field)
    {
        // Si no hay secciones, crear una por defecto
        if (!currentLayout.Sections.Any())
        {
            await AddNewSection();
        }

        var firstSection = currentLayout.Sections.First();
        await AddFieldToSection(firstSection, field);
    }

    private async Task AddFieldToSection(FormSectionDto section, FormFieldItemDto field)
    {
        // Verificar que el campo no esté ya en la sección
        if (section.Fields.Any(f => f.FieldName == field.FieldName))
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Campo ya existe",
                Detail = $"El campo '{field.DisplayName}' ya está en esta sección.",
                Duration = 3000
            });
            return;
        }

        var newField = new FormFieldLayoutDto
        {
            FieldName = field.FieldName,
            DisplayName = field.DisplayName,
            FieldType = field.FieldType,
            GridSize = GetDefaultFieldSize(field.FieldType),
            SortOrder = section.Fields.Count,
            IsRequired = field.IsRequired,
            IsSystemField = field.IsSystemField,
            IsVisible = true
        };

        section.Fields.Add(newField);
        selectedField = newField;
        selectedSection = null;
        StateHasChanged();
    }

    private async Task RemoveField(FormSectionDto section, FormFieldLayoutDto field)
    {
        section.Fields.Remove(field);
        if (selectedField == field) selectedField = null;
        StateHasChanged();
    }

    private async Task ConfigureField(FormFieldLayoutDto field)
    {
        selectedField = field;
        selectedSection = null;
        StateHasChanged();
    }

    private async Task MoveFieldLeft(FormSectionDto section, FormFieldLayoutDto field)
    {
        var fields = section.Fields.OrderBy(f => f.SortOrder).ToList();
        var currentIndex = fields.IndexOf(field);

        if (currentIndex > 0)
        {
            var previousField = fields[currentIndex - 1];

            // Intercambiar los SortOrder
            (field.SortOrder, previousField.SortOrder) = (previousField.SortOrder, field.SortOrder);

            StateHasChanged();
        }
    }

    private async Task MoveFieldRight(FormSectionDto section, FormFieldLayoutDto field)
    {
        var fields = section.Fields.OrderBy(f => f.SortOrder).ToList();
        var currentIndex = fields.IndexOf(field);

        if (currentIndex < fields.Count - 1)
        {
            var nextField = fields[currentIndex + 1];

            // Intercambiar los SortOrder
            (field.SortOrder, nextField.SortOrder) = (nextField.SortOrder, field.SortOrder);

            StateHasChanged();
        }
    }

    private int GetDefaultFieldSize(string fieldType)
    {
        return fieldType switch
        {
            "textarea" => 12,
            "boolean" => 4,
            "date" => 6,
            "number" => 6,
            _ => 6
        };
    }

    #endregion

    #region Preview Actions

    private async Task MoveFieldLeftInPreview(FormSectionDto section, FormFieldLayoutDto field)
    {
        await MoveFieldLeft(section, field);
    }

    private async Task MoveFieldRightInPreview(FormSectionDto section, FormFieldLayoutDto field)
    {
        await MoveFieldRight(section, field);
    }

    private async Task ConfigureFieldInPreview(FormFieldLayoutDto field)
    {
        await ConfigureField(field);
    }

    private async Task RemoveFieldFromPreview(FormSectionDto section, FormFieldLayoutDto field)
    {
        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas eliminar el campo '{field.DisplayName}' de la vista previa?",
            "Confirmar Eliminación",
            new ConfirmOptions()
            {
                OkButtonText = "Eliminar",
                CancelButtonText = "Cancelar"
            });

        if (confirmed == true)
        {
            await RemoveField(section, field);
        }
    }

    private void OnFieldSizeChangedInPreview()
    {
        StateHasChanged();
    }

    #endregion

    #region Options Management

    private async Task EditFieldOptions(FormFieldLayoutDto field)
    {
        // Asegurarse de que UIConfig existe
        if (field.UIConfig == null)
        {
            field.UIConfig = new UIConfig();
        }

        // Asegurarse de que la lista de opciones existe
        if (field.UIConfig.Options == null)
        {
            field.UIConfig.Options = new List<SelectOption>();
        }

        var result = await DialogService.OpenAsync<FieldOptionsEditor>("Editar Opciones",
            new Dictionary<string, object>
            {
                { "FieldName", field.DisplayName },
                { "Options", field.UIConfig.Options.ToList() } // Pasar una copia
            },
            new DialogOptions()
            {
                Width = "800px",
                Height = "600px",
                Resizable = true,
                Draggable = true
            });

        // Si el usuario confirmó los cambios, actualizar las opciones
        if (result is List<SelectOption> updatedOptions)
        {
            field.UIConfig.Options = updatedOptions;
            StateHasChanged();
        }
    }

    #endregion

    #region Custom Fields Management

    private async Task CreateNewCustomField()
    {
        // Abrir el diseñador de campos personalizado
        await DialogService.OpenAsync<CustomFieldDesigner>("Crear Campo Personalizado",
            new Dictionary<string, object>
            {
                { "EntityName", currentEntityName }
            },
            new DialogOptions()
            {
                Width = "1200px",
                Height = "800px",
                Resizable = true,
                Draggable = true
            });

        // Recargar campos disponibles después de crear
        await LoadAvailableFields();
    }

    private async Task EditCustomField(FormFieldItemDto field)
    {
        if (field.Id.HasValue)
        {
            await DialogService.OpenAsync<CustomFieldDesigner>("Editar Campo Personalizado",
                new Dictionary<string, object>
                {
                    { "FieldId", field.Id.Value }
                },
                new DialogOptions()
                {
                    Width = "1200px",
                    Height = "800px",
                    Resizable = true,
                    Draggable = true,
                Icon = "design_services"
                });

            await LoadAvailableFields();
        }
    }

    #endregion

    #region Utilidades

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

    #endregion

    #region Acciones

    private async Task SaveLayout()
    {
        try
        {
            isSaving = true;

            var request = new SaveFormLayoutRequest
            {
                EntityName = currentLayout.EntityName,
                FormName = currentLayout.FormName,
                Description = currentLayout.Description,
                IsDefault = currentLayout.IsDefault,
                Sections = currentLayout.Sections
            };

            var response = await Api.PostAsync<FormLayoutDto>("api/form-designer/formulario/save-layout", request, BackendType.FormBackend);

            if (response.Success)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Layout Guardado",
                    Detail = "El diseño del formulario se ha guardado exitosamente.",
                    Duration = 3000
                });

                if (response.Data != null)
                {
                    currentLayout = response.Data;
                }
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = response.Message ?? "No se pudo guardar el layout.",
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
                Detail = $"Error guardando layout: {ex.Message}",
                Duration = 4000
            });
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task PreviewForm()
    {
        // Implementar vista previa del formulario
        await DialogService.OpenAsync("Vista Previa del Formulario", ds =>
        {
            return builder =>
            {
                builder.AddContent(0, "Vista previa del formulario aquí...");
            };
        }, new DialogOptions()
        {
            Width = "90%",
            Height = "80%"
        });
    }

    private async Task ConfigureEntities()
    {
        // Implementar configuración de entidades
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = "Función en desarrollo",
            Detail = "La configuración de entidades estará disponible próximamente.",
            Duration = 3000
        });
    }

    #endregion

    #region Métodos para campos personalizados y JSON

    private async Task ShowCreateFieldDialog()
    {
        await DialogService.OpenAsync<CustomFieldDesigner>("Campos Personalizados",
            new Dictionary<string, object>
            {
                { "EntityName", currentEntityName },
                { "EntityDisplayName", currentEntityDisplayName }
            },
            new DialogOptions()
            {
                Width = "1000px",
                Height = "750px",
                Resizable = true,
                Icon = "design_services"
            });

        // Recargar campos disponibles después de crear
        await LoadAvailableFields();
    }


    private async Task ShowJsonPreview()
    {
        try
        {
            // Generar el JSON de la configuración actual
            var jsonObject = new
            {
                EntityName = currentEntityName,
                Layout = new
                {
                    Id = currentLayout.Id,
                    FormName = currentLayout.FormName,
                    Description = currentLayout.Description,
                    IsDefault = currentLayout.IsDefault,
                    IsActive = currentLayout.IsActive,
                    OrganizationId = currentLayout.OrganizationId,
                    CreatedAt = currentLayout.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    Sections = currentLayout.Sections.Select(section => new
                    {
                        Id = section.Id,
                        Title = section.Title,
                        Description = section.Description,
                        GridSize = section.GridSize,
                        SortOrder = section.SortOrder,
                        IsCollapsible = section.IsCollapsible,
                        IsExpanded = section.IsExpanded,
                        Fields = section.Fields.OrderBy(f => f.SortOrder).Select(field => new
                        {
                            Id = field.Id,
                            FieldName = field.FieldName,
                            DisplayName = field.DisplayName,
                            FieldType = field.FieldType,
                            GridSize = field.GridSize,
                            SortOrder = field.SortOrder,
                            IsRequired = field.IsRequired,
                            IsReadOnly = field.IsReadOnly,
                            IsVisible = field.IsVisible,
                            IsSystemField = field.IsSystemField,
                            Conditions = field.Conditions
                        }).ToList()
                    }).ToList()
                }
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await DialogService.OpenAsync<JsonPreviewDialog>("Configuración JSON del Formulario",
                new Dictionary<string, object>
                {
                    { "JsonContent", jsonContent }
                },
                new DialogOptions()
                {
                    Width = "800px",
                    Height = "600px",
                    Resizable = true
                });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error generando JSON: {ex.Message}",
                Duration = 5000
            });
        }
    }

    private void GoBackToList()
    {
        Navigation.NavigateTo("/admin/form-designer/list");
    }

    #endregion

    #region Clases auxiliares

    public class GridSizeOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = "";
    }

    public class CustomFieldApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; } = default!;
        public string? Message { get; set; }
    }

    public class EntityApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public List<FormEntityDto> Data { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    #endregion

    #region Preview Methods

    private RenderFragment RenderSimpleFieldPreview(FormFieldLayoutDto field) => builder =>
    {
        var displayName = field.DisplayName + (field.IsRequired ? " *" : "");

        // Para campos boolean, usar diseño horizontal simple pero dentro de un FormField
        if (field.FieldType.ToLowerInvariant() == "boolean")
        {
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
                        <span style='color: #10b981; font-weight: 500;'>Activado</span>
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
                    fieldBuilder.OpenComponent<RadzenTextBox>(20);
                    fieldBuilder.AddAttribute(21, "Value", DateTime.Now.ToString("dd/MM/yyyy"));
                    fieldBuilder.AddAttribute(22, "Disabled", true);
                    fieldBuilder.AddAttribute(23, "Style", "width: 100%");
                    fieldBuilder.CloseComponent();
                    break;

                case "number":
                    fieldBuilder.OpenComponent<RadzenTextBox>(30);
                    fieldBuilder.AddAttribute(31, "Value", "123");
                    fieldBuilder.AddAttribute(32, "Disabled", true);
                    fieldBuilder.AddAttribute(33, "Style", "width: 100%");
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

    #endregion

    #region Options Helper Methods

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

    private object GetFieldOptionsForMainPreview(FormFieldLayoutDto field)
    {
        if (field.UIConfig?.Options?.Any() == true)
        {
            return field.UIConfig.Options.Select(o => o.Label).ToList();
        }

        // Opciones por defecto si no hay configuradas
        return new List<string> { "Opción 1", "Opción 2" };
    }

    #endregion
}