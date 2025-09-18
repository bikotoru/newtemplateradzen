using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using Radzen;
using Radzen.Blazor;
using Forms.Models.DTOs;
using Forms.Models.Enums;
using System.Text.Json;
using Frontend.Services;

namespace Frontend.Modules.Admin.CustomFields;

public partial class CustomFieldManager : ComponentBase
{
    [Inject] public API Api { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;
    [Inject] public NotificationService NotificationService { get; set; } = default!;

    private RadzenDataGrid<CustomFieldDefinitionDto>? fieldsGrid;
    private List<CustomFieldDefinitionDto> allFields = new();
    private List<CustomFieldDefinitionDto> filteredFields = new();
    private CustomFieldDefinitionDto? selectedFieldForPermissions;

    // Filtros
    private string? selectedEntity;
    private string? selectedFieldType;
    private string searchText = "";

    private readonly List<string> availableEntities = new()
    {
        "Empleado",
        "Empresa",
        "Cliente",
        "Proveedor"
    };

    private readonly List<string> availableFieldTypes = new()
    {
        "text",
        "textarea",
        "number",
        "date",
        "boolean",
        "select",
        "multiselect"
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadFields();
    }

    #region Carga de datos

    private async Task LoadFields()
    {
        try
        {
            allFields.Clear();

            // Cargar campos de todas las entidades
            foreach (var entity in availableEntities)
            {
                var response = await Api.GetAsync<List<CustomFieldDefinitionDto>>($"api/customfielddefinitions/{entity}", BackendType.FormBackend);
                if (response.Success && response.Data != null)
                {
                    allFields.AddRange(response.Data);
                }
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando campos: {ex.Message}",
                Duration = 4000
            });
        }
    }

    private void ApplyFilters()
    {
        var query = allFields.AsEnumerable();

        // Filtro por entidad
        if (!string.IsNullOrEmpty(selectedEntity))
        {
            query = query.Where(f => f.EntityName == selectedEntity);
        }

        // Filtro por tipo de campo
        if (!string.IsNullOrEmpty(selectedFieldType))
        {
            query = query.Where(f => f.FieldType == selectedFieldType);
        }

        // Filtro por texto de búsqueda
        if (!string.IsNullOrEmpty(searchText))
        {
            var search = searchText.ToLowerInvariant();
            query = query.Where(f =>
                f.DisplayName.ToLowerInvariant().Contains(search) ||
                f.FieldName.ToLowerInvariant().Contains(search) ||
                (f.Description?.ToLowerInvariant().Contains(search) ?? false)
            );
        }

        filteredFields = query.ToList();
        StateHasChanged();
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        searchText = e.Value?.ToString() ?? "";
        ApplyFilters();
    }

    private async Task ClearFilters()
    {
        selectedEntity = null;
        selectedFieldType = null;
        searchText = "";
        ApplyFilters();
    }

    #endregion

    #region Acciones de campos

    private async Task CreateNewField()
    {
        // Navegar al diseñador
        await DialogService.OpenAsync<CustomFieldDesigner>("Crear Campo Personalizado",
            new Dictionary<string, object>(),
            new DialogOptions()
            {
                Width = "1200px",
                Height = "800px",
                Resizable = true,
                Draggable = true,
                Icon = "design_services"

            });

        await LoadFields(); // Recargar después de crear
    }

    private async Task PreviewField(CustomFieldDefinitionDto field)
    {
        var result = await DialogService.OpenAsync("Vista Previa del Campo", ds =>
        {
            return builder =>
            {
                builder.OpenComponent<RadzenCard>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(cardBuilder =>
                {
                    // Información del campo
                    cardBuilder.OpenComponent<RadzenText>(0);
                    cardBuilder.AddAttribute(1, "TextStyle", TextStyle.H6);
                    cardBuilder.AddAttribute(2, "class", "rz-mb-3");
                    cardBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(titleBuilder =>
                    {
                        titleBuilder.AddContent(0, $"Campo: {field.DisplayName}");
                    }));
                    cardBuilder.CloseComponent();

                    // Detalles
                    cardBuilder.OpenElement(5, "div");
                    cardBuilder.AddAttribute(6, "class", "rz-mb-4");

                    cardBuilder.OpenComponent<RadzenText>(7);
                    cardBuilder.AddAttribute(8, "ChildContent", (RenderFragment)(detailBuilder =>
                    {
                        detailBuilder.AddMarkupContent(0, $"<strong>Entidad:</strong> {field.EntityName}<br/>");
                        detailBuilder.AddMarkupContent(1, $"<strong>Nombre técnico:</strong> {field.FieldName}<br/>");
                        detailBuilder.AddMarkupContent(2, $"<strong>Tipo:</strong> {field.FieldTypeDisplay}<br/>");
                        detailBuilder.AddMarkupContent(3, $"<strong>Requerido:</strong> {(field.IsRequired ? "Sí" : "No")}<br/>");
                        if (!string.IsNullOrEmpty(field.Description))
                        {
                            detailBuilder.AddMarkupContent(4, $"<strong>Descripción:</strong> {field.Description}<br/>");
                        }
                    }));
                    cardBuilder.CloseComponent();

                    cardBuilder.CloseElement();

                    // Vista previa del campo renderizado
                    cardBuilder.OpenComponent<RadzenText>(10);
                    cardBuilder.AddAttribute(11, "TextStyle", TextStyle.Subtitle2);
                    cardBuilder.AddAttribute(12, "class", "rz-mb-3");
                    cardBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(previewTitleBuilder =>
                    {
                        previewTitleBuilder.AddContent(0, "Vista Previa:");
                    }));
                    cardBuilder.CloseComponent();

                    // Aquí renderizaríamos el campo según su tipo
                    cardBuilder.OpenElement(15, "div");
                    cardBuilder.AddAttribute(16, "class", "rz-p-3");
                    cardBuilder.AddAttribute(17, "style", "border: 1px solid var(--rz-border-color); border-radius: 8px; background: var(--rz-base-50);");

                    cardBuilder.OpenComponent<RadzenFormField>(18);
                    cardBuilder.AddAttribute(19, "Text", field.DisplayName);
                    cardBuilder.AddAttribute(20, "Variant", Variant.Outlined);
                    cardBuilder.AddAttribute(21, "ChildContent", (RenderFragment)(fieldBuilder =>
                    {
                        RenderFieldByType(fieldBuilder, field);
                    }));
                    cardBuilder.CloseComponent();

                    cardBuilder.CloseElement();
                }));
                builder.CloseComponent();
            };
        }, new DialogOptions()
        {
            Width = "600px",
            Height = "400px"
        });
    }

    private async Task EditField(CustomFieldDefinitionDto field)
    {
        // Implementar edición
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = "Función en desarrollo",
            Detail = "La edición de campos personalizados estará disponible próximamente.",
            Duration = 3000
        });
    }

    private async Task ToggleFieldStatus(CustomFieldDefinitionDto field)
    {
        try
        {
            var request = new UpdateCustomFieldRequest
            {
                IsEnabled = !field.IsEnabled
            };

            var response = await Api.PutAsync<CustomFieldApiResponse<CustomFieldDefinitionDto>>($"api/customfielddefinitions/{field.Id}", request, BackendType.FormBackend);

            if (response.Success && response.Data?.Success == true)
            {
                field.IsEnabled = !field.IsEnabled;
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Estado actualizado",
                    Detail = $"El campo '{field.DisplayName}' ha sido {(field.IsEnabled ? "activado" : "desactivado")}.",
                    Duration = 3000
                });
                StateHasChanged();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = response.Message ?? "No se pudo actualizar el estado del campo.",
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
    }

    private async Task DeleteField(CustomFieldDefinitionDto field)
    {
        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas eliminar el campo '{field.DisplayName}'?",
            "Confirmar Eliminación",
            new ConfirmOptions()
            {
                OkButtonText = "Eliminar",
                CancelButtonText = "Cancelar"
            });

        if (confirmed == true)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Función en desarrollo",
                Detail = "La eliminación de campos personalizados estará disponible próximamente.",
                Duration = 3000
            });
        }
    }

    #endregion

    #region Utilidades

    private string GetFieldTypeIcon(string fieldType)
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

    private void RenderFieldByType(RenderTreeBuilder builder, CustomFieldDefinitionDto field)
    {
        var fieldType = FieldTypeExtensions.FromString(field.FieldType);

        switch (fieldType)
        {
            case FieldType.Text:
                builder.OpenComponent<RadzenTextBox>(0);
                builder.AddAttribute(1, "Placeholder", "Ejemplo de texto...");
                builder.AddAttribute(2, "Style", "width: 100%;");
                builder.AddAttribute(3, "Disabled", true);
                builder.CloseComponent();
                break;

            case FieldType.TextArea:
                builder.OpenComponent<RadzenTextArea>(0);
                builder.AddAttribute(1, "Placeholder", "Ejemplo de texto largo...");
                builder.AddAttribute(2, "Rows", 3);
                builder.AddAttribute(3, "Style", "width: 100%;");
                builder.AddAttribute(4, "Disabled", true);
                builder.CloseComponent();
                break;

            case FieldType.Number:
                builder.OpenComponent<RadzenNumeric<decimal?>>(0);
                builder.AddAttribute(1, "Placeholder", "123");
                builder.AddAttribute(2, "Style", "width: 100%;");
                builder.AddAttribute(3, "Disabled", true);
                builder.CloseComponent();
                break;

            case FieldType.Date:
                builder.OpenComponent<RadzenDatePicker<DateTime?>>(0);
                builder.AddAttribute(1, "Placeholder", "Selecciona una fecha");
                builder.AddAttribute(2, "Style", "width: 100%;");
                builder.AddAttribute(3, "Disabled", true);
                builder.CloseComponent();
                break;

            case FieldType.Boolean:
                builder.OpenComponent<RadzenSwitch>(0);
                builder.AddAttribute(1, "Disabled", true);
                builder.CloseComponent();
                break;

            case FieldType.Select:
                builder.OpenComponent<RadzenDropDown<string>>(0);
                builder.AddAttribute(1, "Placeholder", "Selecciona una opción");
                builder.AddAttribute(2, "Data", new List<string> { "Opción 1", "Opción 2", "Opción 3" });
                builder.AddAttribute(3, "Style", "width: 100%;");
                builder.AddAttribute(4, "Disabled", true);
                builder.CloseComponent();
                break;

            case FieldType.MultiSelect:
                builder.OpenComponent<RadzenListBox<IEnumerable<string>>>(0);
                builder.AddAttribute(1, "Data", new List<string> { "Opción 1", "Opción 2", "Opción 3" });
                builder.AddAttribute(2, "Multiple", true);
                builder.AddAttribute(3, "Style", "width: 100%; height: 120px;");
                builder.AddAttribute(4, "Disabled", true);
                builder.CloseComponent();
                break;
        }
    }

    private List<FieldPermissionInfo> GetFieldPermissions(CustomFieldDefinitionDto field)
    {
        return new List<FieldPermissionInfo>
        {
            new()
            {
                Action = "Ver",
                Permission = $"{field.EntityName}.{field.FieldName}.VIEW",
                Icon = "visibility"
            },
            new()
            {
                Action = "Crear",
                Permission = $"{field.EntityName}.{field.FieldName}.CREATE",
                Icon = "add"
            },
            new()
            {
                Action = "Actualizar",
                Permission = $"{field.EntityName}.{field.FieldName}.UPDATE",
                Icon = "edit"
            }
        };
    }

    #endregion

    #region Clases auxiliares

    public class FieldPermissionInfo
    {
        public string Action { get; set; } = "";
        public string Permission { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class CustomFieldApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; } = default!;
        public string? Message { get; set; }
    }

    #endregion
}