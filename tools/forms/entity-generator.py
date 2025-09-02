#!/usr/bin/env python3
"""
🏗️ Entity Generator for .NET + Blazor
Creates complete entity structure with frontend and backend files

Usage:
    python tools/forms/entity-generator.py --entity "Producto" --plural "Productos" --module "Ventas" --fields "nombre:string:255" "precio:decimal:18,2"
    python tools/forms/entity-generator.py --entity "Usuario" --plural "Usuarios" --module "Auth" --fields "email:string:100" "activo:bool"
"""

import os
import sys
import argparse
from pathlib import Path
import re
from datetime import datetime

class EntityGenerator:
    def __init__(self, project_root="."):
        self.project_root = Path(project_root)
        self.frontend_path = self.project_root / "Frontend"
        self.backend_path = self.project_root / "Backend"
        
        # Mapeo de tipos para frontend
        self.frontend_type_mapping = {
            'string': 'RadzenTextBox',
            'text': 'RadzenTextArea',
            'int': 'RadzenNumeric<int>',
            'decimal': 'RadzenNumeric<decimal>',
            'datetime': 'RadzenDatePicker<DateTime?>',
            'bool': 'RadzenCheckBox',
            'guid': 'RadzenTextBox',
            'autoincremental': 'RadzenTextBox'
        }
        
        # Mapeo de tipos para propiedades .NET
        self.net_type_mapping = {
            'string': 'string',
            'text': 'string',
            'int': 'int',
            'decimal': 'decimal',
            'datetime': 'DateTime?',
            'bool': 'bool',
            'guid': 'Guid?',
            'autoincremental': 'string'
        }
    
    def print_header(self):
        print("=" * 70)
        print("🏗️  ENTITY GENERATOR")
        print("=" * 70)
        print()
    
    def validate_entity_name(self, name):
        """Valida nombre de entidad"""
        if not re.match(r'^[A-Z][a-zA-Z0-9]*$', name):
            raise ValueError(f"Nombre de entidad inválido: {name}. Debe comenzar con mayúscula y contener solo letras y números.")
        return name
    
    def validate_module_name(self, name):
        """Valida nombre de módulo"""
        if not re.match(r'^[A-Z][a-zA-Z0-9]*$', name):
            raise ValueError(f"Nombre de módulo inválido: {name}. Debe comenzar con mayúscula y contener solo letras y números.")
        return name
    
    def parse_field(self, field_str):
        """Parsea un campo: 'nombre:tipo:tamaño' """
        parts = field_str.split(':')
        if len(parts) < 2:
            raise ValueError(f"Formato de campo inválido: {field_str}. Use 'nombre:tipo' o 'nombre:tipo:tamaño'")
        
        field_name = parts[0].strip()
        field_type = parts[1].lower().strip()
        field_size = parts[2].strip() if len(parts) > 2 else None
        
        # Validar tipo
        if field_type not in self.net_type_mapping:
            valid_types = list(self.net_type_mapping.keys())
            raise ValueError(f"Tipo de dato no soportado: {field_type}. Tipos válidos: {valid_types}")
        
        return {
            'name': field_name,
            'type': field_type,
            'size': field_size,
            'net_type': self.net_type_mapping[field_type],
            'frontend_component': self.frontend_type_mapping[field_type],
            'nullable': field_type not in ['bool']
        }
    
    def generate_frontend_fast_razor(self, entity_name, module_name, fields):
        """Genera el archivo Fast.razor para frontend"""
        field_forms = []
        
        for field in fields:
            component = field['frontend_component']
            field_name = field['name']
            
            if field['type'] == 'bool':
                field_forms.append(f"""                                        <RadzenFormField Text="{field_name}">
                                            <{component} @bind-Value="entity.{field_name}" />
                                        </RadzenFormField>""")
            elif field['type'] in ['int', 'decimal']:
                field_forms.append(f"""                                        <RadzenFormField Text="{field_name}">
                                            <{component} @bind-Value="entity.{field_name}" Placeholder="Ingrese {field_name.lower()}" />
                                        </RadzenFormField>""")
            else:
                field_forms.append(f"""                                        <RadzenFormField Text="{field_name}">
                                            <{component} @bind-Value="entity.{field_name}" Placeholder="Ingrese {field_name.lower()}" />
                                        </RadzenFormField>""")
        
        forms_content = "\n".join(field_forms) if field_forms else """                                        <RadzenFormField Text="Nombre">
                                            <RadzenTextBox @bind-Value="entity.Nombre" Placeholder="Ingrese el nombre" />
                                        </RadzenFormField>"""
        
        return f"""@using Frontend.Components.Base
@using Shared.Models.Entities
@using Frontend.Modules.{entity_name}
@inherits ComponentBase

<EssentialsCard Title="Nuevo {entity_name}" 
               Icon="add" 
               IconColor="#0078d4">
    <ChildContent>
        <RadzenStack Gap="1rem">
{forms_content}
        </RadzenStack>
    </ChildContent>
</EssentialsCard>

<style>
    .fast-form {{
        max-width: 500px;
        margin: 0 auto;
    }}
</style>
"""
    
    def generate_frontend_fast_cs(self, entity_name, module_name):
        """Genera el archivo Fast.razor.cs para frontend"""
        return f"""using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Frontend.Modules.{entity_name};
using Shared.Models.Builders;

namespace Frontend.Modules.{entity_name};

public partial class {entity_name}Fast : ComponentBase
{{
    [Inject] private {entity_name}Service {entity_name}Service {{ get; set; }} = null!;
    [Parameter] public EventCallback<{entity_name}> OnEntityCreated {{ get; set; }}
    [Parameter] public EventCallback OnCancel {{ get; set; }}

    private {entity_name} entity = new();
    private string errorMessage = string.Empty;
    private bool isLoading = false;

    private async Task SaveEntity()
    {{
        try
        {{
            isLoading = true;
            errorMessage = string.Empty;

            var createRequest = new CreateRequestBuilder<{entity_name}>(entity)
                .Build();

            var response = await {entity_name}Service.CreateAsync(createRequest);

            if (response.Success && response.Data != null)
            {{
                await OnEntityCreated.InvokeAsync(response.Data);
                entity = new {entity_name}();
            }}
            else
            {{
                errorMessage = response.Message ?? "Error al crear {entity_name.lower()}";
            }}
        }}
        catch (Exception ex)
        {{
            errorMessage = $"Error inesperado: {{ex.Message}}";
        }}
        finally
        {{
            isLoading = false;
            StateHasChanged();
        }}
    }}

    private async Task Cancel()
    {{
        await OnCancel.InvokeAsync();
    }}
}}
"""
    
    def generate_frontend_formulario_razor(self, entity_name, module_name, fields):
        """Genera el archivo Formulario.razor para frontend"""
        field_forms = []
        
        for field in fields:
            component = field['frontend_component']
            field_name = field['name']
            
            if field['type'] == 'bool':
                field_forms.append(f"""                                        <RadzenFormField Text="{field_name}">
                                            <{component} @bind-Value="entity.{field_name}" />
                                        </RadzenFormField>""")
            elif field['type'] in ['int', 'decimal']:
                field_forms.append(f"""                                        <RadzenFormField Text="{field_name}">
                                            <{component} @bind-Value="entity.{field_name}" Placeholder="Ingrese {field_name.lower()}" />
                                        </RadzenFormField>""")
            else:
                field_forms.append(f"""                                        <RadzenFormField Text="{field_name}">
                                            <{component} @bind-Value="entity.{field_name}" Placeholder="Ingrese {field_name.lower()}" />
                                        </RadzenFormField>""")
        
        forms_content = "\n".join(field_forms) if field_forms else """                                        <RadzenFormField Text="Nombre">
                                            <RadzenTextBox @bind-Value="entity.Nombre" Placeholder="Ingrese el nombre" />
                                        </RadzenFormField>"""
        
        return f"""@page "/{module_name.lower()}/{entity_name.lower()}/formulario"
@page "/{module_name.lower()}/{entity_name.lower()}/formulario/{{Id:guid}}"
@using Frontend.Components.Base.Forms.PageWithCommandBar
@using Frontend.Components.Base
@using Shared.Models.Entities

<PageTitle>{entity_name} - Formulario</PageTitle>

<PageWithCommandBar BackPath="/{module_name.lower()}/{entity_name.lower()}/list" 
                    ShowSave="true"
                    OnSaveClick="@SaveForm">

    <div class="full-width-tabs">
        <CrmTabs DefaultTabId="tab1" DisableUrlSync="true">
            
            <CrmTab Id="tab1" Title="Información General" Icon="edit" IconColor="#0078d4">
                <div class="scrollable-content">
                    <RadzenRow JustifyContent="JustifyContent.Center">
                        <RadzenColumn SizeLG="4" SizeMD="6" SizeSM="12">
                            
                            <EssentialsCard Title="Información Básica" 
                                           Icon="info" 
                                           IconColor="#0078d4"
                                           ShowEssentials="true">
                                
                                <EssentialsTemplate>
                                    <EssentialsGrid>
                                        <EssentialsItem Label="Estado" Value="@(entity.Active ? "Activo" : "Inactivo")" />
                                        <EssentialsItem Label="Tipo" Value="{entity_name}" />
                                        <EssentialsItem Label="Creado" Value="@(entity.FechaCreacion?.ToString("dd/MM/yyyy") ?? "Nuevo")" />
                                        <EssentialsItem Label="Módulo" Value="{module_name}" IsLink="true" />
                                    </EssentialsGrid>
                                </EssentialsTemplate>
                                
                                <ChildContent>
                                    <RadzenStack Gap="1rem">
{forms_content}
                                    </RadzenStack>
                                </ChildContent>
                            </EssentialsCard>
                            
                        </RadzenColumn>
                    </RadzenRow>
                </div>
            </CrmTab>
            
        </CrmTabs>
    </div>

    @if (!string.IsNullOrEmpty(mensaje))
    {{
        <div class="alert alert-success">
            <strong>✅ @mensaje</strong>
        </div>
    }}

    @if (!string.IsNullOrEmpty(errorMessage))
    {{
        <div class="alert alert-error">
            <strong>❌ Error:</strong> @errorMessage
        </div>
    }}

</PageWithCommandBar>

<style>
    .full-width-tabs {{
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
    }}

    .scrollable-content {{
        flex: 1;
        overflow-y: auto;
        padding: 2rem 1rem;
    }}

    .alert {{
        padding: 12px;
        border-radius: 4px;
        margin: 1rem auto;
        max-width: 600px;
        text-align: center;
        position: fixed;
        top: 10px;
        left: 50%;
        transform: translateX(-50%);
        z-index: 1000;
    }}

    .alert-success {{
        background: #d4edda;
        border: 1px solid #c3e6cb;
        color: #155724;
    }}

    .alert-error {{
        background: #f8d7da;
        border: 1px solid #f5c6cb;
        color: #721c24;
    }}

    @media (max-width: 768px) {{
        .scrollable-content {{
            padding: 1rem;
        }}
    }}
</style>
"""
    
    def generate_frontend_formulario_cs(self, entity_name, module_name):
        """Genera el archivo Formulario.razor.cs para frontend"""
        return f"""using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Frontend.Modules.{entity_name};
using Shared.Models.Builders;

namespace Frontend.Modules.{entity_name};

public partial class {entity_name}Formulario : ComponentBase
{{
    [Inject] private {entity_name}Service {entity_name}Service {{ get; set; }} = null!;
    [Inject] private NavigationManager Navigation {{ get; set; }} = null!;
    [Parameter] public Guid? Id {{ get; set; }}

    private {entity_name} entity = new();
    private string mensaje = string.Empty;
    private string errorMessage = string.Empty;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;

    protected override async Task OnInitializedAsync()
    {{
        if (isEditMode && Id.HasValue)
        {{
            await LoadEntity();
        }}
    }}

    private async Task LoadEntity()
    {{
        try
        {{
            isLoading = true;
            var response = await {entity_name}Service.GetByIdAsync(Id!.Value);
            
            if (response.Success && response.Data != null)
            {{
                entity = response.Data;
            }}
            else
            {{
                errorMessage = "No se pudo cargar la entidad";
                Navigation.NavigateTo("/{module_name.lower()}/{entity_name.lower()}/list");
            }}
        }}
        catch (Exception ex)
        {{
            errorMessage = $"Error cargando entidad: {{ex.Message}}";
        }}
        finally
        {{
            isLoading = false;
            StateHasChanged();
        }}
    }}

    private async Task SaveForm()
    {{
        try
        {{
            isLoading = true;
            mensaje = string.Empty;
            errorMessage = string.Empty;

            if (isEditMode)
            {{
                var updateRequest = new UpdateRequestBuilder<{entity_name}>(entity)
                    .Build();

                var response = await {entity_name}Service.UpdateAsync(updateRequest);

                if (response.Success)
                {{
                    mensaje = "{entity_name} actualizado exitosamente";
                    await Task.Delay(2000);
                    Navigation.NavigateTo("/{module_name.lower()}/{entity_name.lower()}/list");
                }}
                else
                {{
                    errorMessage = response.Message ?? "Error al actualizar {entity_name.lower()}";
                }}
            }}
            else
            {{
                var createRequest = new CreateRequestBuilder<{entity_name}>(entity)
                    .Build();

                var response = await {entity_name}Service.CreateAsync(createRequest);

                if (response.Success)
                {{
                    mensaje = "{entity_name} creado exitosamente";
                    entity = new {entity_name}();
                    await Task.Delay(2000);
                    Navigation.NavigateTo("/{module_name.lower()}/{entity_name.lower()}/list");
                }}
                else
                {{
                    errorMessage = response.Message ?? "Error al crear {entity_name.lower()}";
                }}
            }}
        }}
        catch (Exception ex)
        {{
            errorMessage = $"Error inesperado: {{ex.Message}}";
        }}
        finally
        {{
            isLoading = false;
            StateHasChanged();
        }}
    }}
}}
"""
    
    def generate_frontend_list_razor(self, entity_name, plural_name, module_name, fields):
        """Genera el archivo List.razor para frontend"""
        # Generar columnas para el grid
        grid_columns = []
        for field in fields[:5]:  # Máximo 5 campos en el grid
            if field['type'] in ['bool']:
                grid_columns.append(f"""                <RadzenDataGridColumn TItem="{entity_name}" Property="{field['name']}" Title="{field['name']}">
                    <Template Context="entity">
                        <RadzenCheckBox Value="entity.{field['name']}" Disabled="true" />
                    </Template>
                </RadzenDataGridColumn>""")
            elif field['type'] == 'datetime':
                grid_columns.append(f"""                <RadzenDataGridColumn TItem="{entity_name}" Property="{field['name']}" Title="{field['name']}">
                    <Template Context="entity">
                        @(entity.{field['name']}?.ToString("dd/MM/yyyy") ?? "-")
                    </Template>
                </RadzenDataGridColumn>""")
            else:
                grid_columns.append(f"""                <RadzenDataGridColumn TItem="{entity_name}" Property="{field['name']}" Title="{field['name']}" />""")
        
        columns_content = "\n".join(grid_columns) if grid_columns else f"""                <RadzenDataGridColumn TItem="{entity_name}" Property="Nombre" Title="Nombre" />"""
        
        return f"""@page "/{module_name.lower()}/{entity_name.lower()}/list"
@using Frontend.Components.Base.Forms.PageWithCommandBar
@using Frontend.Components.FluentUI.CommandBar
@using Shared.Models.Entities

<PageTitle>{plural_name}</PageTitle>

<PageWithCommandBar BackPath="/" 
                    ShowAdd="true"
                    OnAddClick="@GoToCreate">

    <div class="full-width-container">
        <RadzenDataGrid @ref="grid"
                       TItem="{entity_name}"
                       Data="@entities"
                       Count="@totalCount"
                       LoadData="@LoadData"
                       AllowPaging="true"
                       AllowSorting="true"
                       AllowFiltering="true"
                       AllowColumnResize="true"
                       PageSize="20"
                       PagerHorizontalAlign="HorizontalAlign.Center"
                       EmptyText="No se encontraron {plural_name.lower()}">
            
            <Columns>
{columns_content}
                
                <RadzenDataGridColumn TItem="{entity_name}" Sortable="false" Filterable="false" Width="120px" TextAlign="TextAlign.Center">
                    <HeaderTemplate>
                        <span>Acciones</span>
                    </HeaderTemplate>
                    <Template Context="entity">
                        <RadzenButton Icon="edit" 
                                     ButtonStyle="ButtonStyle.Light" 
                                     Size="ButtonSize.Small"
                                     Click="@(() => EditEntity(entity.Id))"
                                     class="me-1" />
                        <RadzenButton Icon="delete" 
                                     ButtonStyle="ButtonStyle.Danger" 
                                     Size="ButtonSize.Small"
                                     Click="@(() => DeleteEntity(entity.Id))" />
                    </Template>
                </RadzenDataGridColumn>
            </Columns>
        </RadzenDataGrid>
    </div>

</PageWithCommandBar>

<style>
    .full-width-container {{
        width: 100%;
        height: calc(100vh - 120px);
        padding: 1rem;
    }}
</style>
"""
    
    def generate_frontend_list_cs(self, entity_name, plural_name, module_name):
        """Genera el archivo List.razor.cs para frontend"""
        return f"""using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Models.Entities;
using Frontend.Modules.{entity_name};

namespace Frontend.Modules.{entity_name};

public partial class {entity_name}List : ComponentBase
{{
    [Inject] private {entity_name}Service {entity_name}Service {{ get; set; }} = null!;
    [Inject] private NavigationManager Navigation {{ get; set; }} = null!;
    [Inject] private DialogService DialogService {{ get; set; }} = null!;

    private RadzenDataGrid<{entity_name}>? grid;
    private IEnumerable<{entity_name}> entities = new List<{entity_name}>();
    private int totalCount;
    private bool isLoading = false;

    private async Task LoadData(LoadDataArgs args)
    {{
        try
        {{
            isLoading = true;
            
            var response = await {entity_name}Service.LoadDataAsync(args);
            
            if (response.Success && response.Data != null)
            {{
                entities = response.Data.Data;
                totalCount = response.Data.TotalCount;
            }}
            else
            {{
                entities = new List<{entity_name}>();
                totalCount = 0;
            }}
        }}
        catch (Exception ex)
        {{
            entities = new List<{entity_name}>();
            totalCount = 0;
        }}
        finally
        {{
            isLoading = false;
            StateHasChanged();
        }}
    }}

    private void GoToCreate()
    {{
        Navigation.NavigateTo("/{module_name.lower()}/{entity_name.lower()}/formulario");
    }}

    private void EditEntity(Guid id)
    {{
        Navigation.NavigateTo($"/{module_name.lower()}/{entity_name.lower()}/formulario/{{id}}");
    }}

    private async Task DeleteEntity(Guid id)
    {{
        try
        {{
            var confirm = await DialogService.Confirm(
                "¿Está seguro que desea eliminar este {entity_name.lower()}?", 
                "Confirmar eliminación", 
                new ConfirmOptions {{ OkButtonText = "Sí", CancelButtonText = "No" }}
            );

            if (confirm == true)
            {{
                var response = await {entity_name}Service.DeleteAsync(id);
                
                if (response.Success)
                {{
                    await grid!.Reload();
                }}
                else
                {{
                    await DialogService.Alert(
                        response.Message ?? "Error al eliminar {entity_name.lower()}", 
                        "Error"
                    );
                }}
            }}
        }}
        catch (Exception ex)
        {{
            await DialogService.Alert($"Error inesperado: {{ex.Message}}", "Error");
        }}
    }}
}}
"""
    
    def generate_frontend_config_cs(self, entity_name, module_name):
        """Genera el archivo Config.cs para frontend"""
        return f"""using Shared.Models.Entities;

namespace Frontend.Modules.{entity_name};

public static class {entity_name}Config
{{
    /// <summary>
    /// Configuraciones específicas para {entity_name}
    /// </summary>
    public static class Settings
    {{
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const string DefaultSortField = "FechaCreacion";
        public const bool DefaultSortDescending = true;
    }}

    /// <summary>
    /// Configuraciones para búsqueda
    /// </summary>
    public static class Search
    {{
        public static readonly string[] DefaultSearchFields = {{ "Nombre" }};
        public const int MinSearchLength = 2;
        public const int SearchDelay = 300; // ms
    }}

    /// <summary>
    /// Configuraciones para formularios
    /// </summary>
    public static class Forms
    {{
        public const string RequiredFieldMessage = "Este campo es obligatorio";
        public const string SaveSuccessMessage = "{entity_name} guardado exitosamente";
        public const string DeleteSuccessMessage = "{entity_name} eliminado exitosamente";
        public const string ErrorGenericMessage = "Ha ocurrido un error inesperado";
    }}

    /// <summary>
    /// Configuraciones para exportación
    /// </summary>
    public static class Export
    {{
        public const string DefaultFileName = "{plural_name}";
        public static readonly string[] AllowedFormats = {{ "xlsx", "csv", "pdf" }};
    }}
}}
"""
    
    def generate_frontend_service_cs(self, entity_name, module_name):
        """Genera el archivo Service.cs para frontend"""
        return f"""using Frontend.Services;
using Shared.Models.Entities;

namespace Frontend.Modules.{entity_name};

public class {entity_name}Service : BaseApiService<{entity_name}>
{{
    public {entity_name}Service(HttpClient httpClient, ILogger<{entity_name}Service> logger)
        : base(httpClient, logger, "api/{entity_name.lower()}")
    {{
    }}

    // ✅ Hereda automáticamente todos los métodos:
    // - CreateAsync(CreateRequest<{entity_name}>)
    // - UpdateAsync(UpdateRequest<{entity_name}>)
    // - DeleteAsync(Guid)
    // - GetByIdAsync(Guid)
    // - GetAllAsync(int page, int pageSize)
    // - QueryAsync(QueryRequest)
    // - SearchAsync(SearchRequest)
    // - LoadDataAsync(LoadDataArgs) para RadzenDataGrid
    // - CreateBatchAsync(CreateBatchRequest<{entity_name}>)
    // - UpdateBatchAsync(UpdateBatchRequest<{entity_name}>)

    // Métodos específicos adicionales para {entity_name}
    // Agregar aquí métodos custom si son necesarios
}}
"""
    
    def generate_backend_controller_cs(self, entity_name, module_name):
        """Genera el archivo Controller.cs para backend"""
        return f"""using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.{module_name}.{entity_name}
{{
    [Route("api/[controller]")]
    public class {entity_name}Controller : BaseQueryController<{entity_name}>
    {{
        private readonly {entity_name}Service _{entity_name.lower()}Service;

        public {entity_name}Controller({entity_name}Service {entity_name.lower()}Service, ILogger<{entity_name}Controller> logger, IServiceProvider serviceProvider)
            : base({entity_name.lower()}Service, logger, serviceProvider)
        {{
            _{entity_name.lower()}Service = {entity_name.lower()}Service;
        }}

        // ✅ Hereda automáticamente todos los endpoints SEALED:
        // POST /api/{entity_name.lower()}/create
        // PUT /api/{entity_name.lower()}/update  
        // GET /api/{entity_name.lower()}/all?page=1&pageSize=10&all=false
        // GET /api/{entity_name.lower()}/{{id}}
        // DELETE /api/{entity_name.lower()}/{{id}}
        // POST /api/{entity_name.lower()}/create-batch
        // PUT /api/{entity_name.lower()}/update-batch
        // GET /api/{entity_name.lower()}/health
        // POST /api/{entity_name.lower()}/query
        // POST /api/{entity_name.lower()}/paged
        // POST /api/{entity_name.lower()}/search
        // POST /api/{entity_name.lower()}/search-paged
        // POST /api/{entity_name.lower()}/select
        // POST /api/{entity_name.lower()}/select-paged

        // Agregar endpoints específicos adicionales para {entity_name} aquí si son necesarios
    }}
}}
"""
    
    def generate_backend_service_cs(self, entity_name, module_name):
        """Genera el archivo Service.cs para backend"""
        return f"""using Backend.Utils.Services;
using Backend.Utils.Data;
using Shared.Models.Entities;

namespace Backend.Modules.{module_name}.{entity_name}
{{
    public class {entity_name}Service : BaseQueryService<{entity_name}>
    {{
        public {entity_name}Service(AppDbContext context, ILogger<{entity_name}Service> logger, IServiceProvider serviceProvider)
            : base(context, logger, serviceProvider)
        {{
        }}

        // ✅ Hereda automáticamente toda la funcionalidad CRUD + Query + Search:
        // - CreateAsync(CreateRequest<{entity_name}>)
        // - UpdateAsync(UpdateRequest<{entity_name}>)
        // - DeleteAsync(Guid)
        // - GetByIdAsync(Guid)
        // - GetAllAsync(int page, int pageSize, bool includeInactive)
        // - QueryAsync(QueryRequest)
        // - SearchAsync(SearchRequest)
        // - CreateBatchAsync(CreateBatchRequest<{entity_name}>)
        // - UpdateBatchAsync(UpdateBatchRequest<{entity_name}>)
        // - Query() para QueryBuilder tipado

        // Métodos específicos adicionales para {entity_name}
        // Agregar aquí métodos de negocio específicos si son necesarios
        
        // Ejemplo de método custom:
        // public async Task<List<{entity_name}>> GetActive{plural_name}Async()
        // {{
        //     return await Query()
        //         .Where(x => x.Active == true)
        //         .OrderBy(x => x.FechaCreacion, true)
        //         .ToListAsync();
        // }}
    }}
}}
"""
    
    def create_directories(self, entity_name, module_name):
        """Crea las estructuras de directorios necesarias"""
        # Directorios frontend
        frontend_module_path = self.frontend_path / "Modules" / entity_name
        frontend_module_path.mkdir(parents=True, exist_ok=True)
        
        # Directorios backend
        backend_module_path = self.backend_path / "Modules" / module_name / entity_name
        backend_module_path.mkdir(parents=True, exist_ok=True)
        
        return frontend_module_path, backend_module_path
    
    def create_entity_files(self, entity_name, plural_name, module_name, fields):
        """Crea todos los archivos de la entidad"""
        try:
            # Crear directorios
            frontend_path, backend_path = self.create_directories(entity_name, module_name)
            
            # Archivos frontend
            files_created = []
            
            # Fast.razor
            fast_razor_content = self.generate_frontend_fast_razor(entity_name, module_name, fields)
            fast_razor_path = frontend_path / f"{entity_name}Fast.razor"
            fast_razor_path.write_text(fast_razor_content, encoding='utf-8')
            files_created.append(str(fast_razor_path))
            
            # Fast.razor.cs
            fast_cs_content = self.generate_frontend_fast_cs(entity_name, module_name)
            fast_cs_path = frontend_path / f"{entity_name}Fast.razor.cs"
            fast_cs_path.write_text(fast_cs_content, encoding='utf-8')
            files_created.append(str(fast_cs_path))
            
            # Formulario.razor
            form_razor_content = self.generate_frontend_formulario_razor(entity_name, module_name, fields)
            form_razor_path = frontend_path / f"{entity_name}Formulario.razor"
            form_razor_path.write_text(form_razor_content, encoding='utf-8')
            files_created.append(str(form_razor_path))
            
            # Formulario.razor.cs
            form_cs_content = self.generate_frontend_formulario_cs(entity_name, module_name)
            form_cs_path = frontend_path / f"{entity_name}Formulario.razor.cs"
            form_cs_path.write_text(form_cs_content, encoding='utf-8')
            files_created.append(str(form_cs_path))
            
            # List.razor
            list_razor_content = self.generate_frontend_list_razor(entity_name, plural_name, module_name, fields)
            list_razor_path = frontend_path / f"{entity_name}List.razor"
            list_razor_path.write_text(list_razor_content, encoding='utf-8')
            files_created.append(str(list_razor_path))
            
            # List.razor.cs
            list_cs_content = self.generate_frontend_list_cs(entity_name, plural_name, module_name)
            list_cs_path = frontend_path / f"{entity_name}List.razor.cs"
            list_cs_path.write_text(list_cs_content, encoding='utf-8')
            files_created.append(str(list_cs_path))
            
            # Config.cs
            config_content = self.generate_frontend_config_cs(entity_name, module_name)
            config_path = frontend_path / f"{entity_name}Config.cs"
            config_path.write_text(config_content, encoding='utf-8')
            files_created.append(str(config_path))
            
            # Service.cs frontend
            service_frontend_content = self.generate_frontend_service_cs(entity_name, module_name)
            service_frontend_path = frontend_path / f"{entity_name}Service.cs"
            service_frontend_path.write_text(service_frontend_content, encoding='utf-8')
            files_created.append(str(service_frontend_path))
            
            # Archivos backend
            
            # Controller.cs
            controller_content = self.generate_backend_controller_cs(entity_name, module_name)
            controller_path = backend_path / f"{entity_name}Controller.cs"
            controller_path.write_text(controller_content, encoding='utf-8')
            files_created.append(str(controller_path))
            
            # Service.cs backend
            service_backend_content = self.generate_backend_service_cs(entity_name, module_name)
            service_backend_path = backend_path / f"{entity_name}Service.cs"
            service_backend_path.write_text(service_backend_content, encoding='utf-8')
            files_created.append(str(service_backend_path))
            
            return files_created
            
        except Exception as e:
            raise Exception(f"Error creando archivos: {e}")
    
    def register_services(self, entity_name, module_name):
        """Registra los servicios en los ServiceRegistry correspondientes"""
        try:
            # Registrar servicio backend
            backend_registry_path = self.backend_path / "Services" / "ServiceRegistry.cs"
            if backend_registry_path.exists():
                content = backend_registry_path.read_text(encoding='utf-8')
                
                # Buscar la línea de registro de servicios
                service_line = f"            services.AddScoped<Backend.Modules.{module_name}.{entity_name}.{entity_name}Service>();"
                
                if service_line not in content:
                    # Buscar donde insertar el servicio
                    lines = content.split('\n')
                    insert_index = -1
                    
                    for i, line in enumerate(lines):
                        if "AddScoped<" in line and "Service>" in line:
                            insert_index = i + 1
                    
                    if insert_index > 0:
                        lines.insert(insert_index, service_line)
                        updated_content = '\n'.join(lines)
                        backend_registry_path.write_text(updated_content, encoding='utf-8')
                        print(f"   ✅ Servicio backend registrado en ServiceRegistry")
            
            # Registrar servicio frontend
            frontend_registry_path = self.frontend_path / "Services" / "ServiceRegistry.cs"
            if frontend_registry_path.exists():
                content = frontend_registry_path.read_text(encoding='utf-8')
                
                service_line = f"            services.AddScoped<Frontend.Modules.{entity_name}.{entity_name}Service>();"
                
                if service_line not in content:
                    lines = content.split('\n')
                    insert_index = -1
                    
                    for i, line in enumerate(lines):
                        if "AddScoped<" in line and "Service>" in line:
                            insert_index = i + 1
                    
                    if insert_index > 0:
                        lines.insert(insert_index, service_line)
                        updated_content = '\n'.join(lines)
                        frontend_registry_path.write_text(updated_content, encoding='utf-8')
                        print(f"   ✅ Servicio frontend registrado en ServiceRegistry")
                        
        except Exception as e:
            print(f"   ⚠️  Error registrando servicios: {e}")
    
    def run(self, entity_name, plural_name, module_name, fields=None):
        """Ejecuta el proceso completo de generación"""
        self.print_header()
        
        try:
            # Validaciones
            entity_name = self.validate_entity_name(entity_name)
            module_name = self.validate_module_name(module_name)
            
            print(f"🏗️  GENERANDO ENTIDAD: {entity_name}")
            print(f"📦 Módulo: {module_name}")
            print(f"📊 Plural: {plural_name}")
            
            # Parsear campos
            parsed_fields = []
            if fields:
                print(f"📝 Campos personalizados: {len(fields)}")
                for field_str in fields:
                    field = self.parse_field(field_str)
                    parsed_fields.append(field)
                    print(f"   • {field['name']}: {field['net_type']}")
            else:
                print("📝 Sin campos personalizados (solo campos base)")
            
            print()
            
            # Crear archivos
            print("📁 Creando estructura de archivos...")
            files_created = self.create_entity_files(entity_name, plural_name, module_name, parsed_fields)
            
            print(f"\n✅ ARCHIVOS CREADOS ({len(files_created)}):")
            for file_path in files_created:
                relative_path = str(Path(file_path).relative_to(self.project_root))
                print(f"   📄 {relative_path}")
            
            # Registrar servicios
            print(f"\n🔧 Registrando servicios...")
            self.register_services(entity_name, module_name)
            
            print("\n🎉 GENERACIÓN COMPLETADA EXITOSAMENTE")
            print(f"✅ Entidad '{entity_name}' generada en módulo '{module_name}'")
            print(f"✅ Frontend: Frontend/Modules/{entity_name}/")
            print(f"✅ Backend: Backend/Modules/{module_name}/{entity_name}/")
            print(f"\n📋 URLs DISPONIBLES:")
            print(f"   📝 Formulario: /{module_name.lower()}/{entity_name.lower()}/formulario")
            print(f"   📊 Lista: /{module_name.lower()}/{entity_name.lower()}/list")
            print(f"\n💡 PRÓXIMOS PASOS:")
            print(f"   1. Ejecutar: dotnet build")
            print(f"   2. Crear entidad en BD: python tools/db/table.py --name \"{entity_name.lower()}\" --fields [...] --execute")
            print(f"   3. Sincronizar modelos: python tools/dbsync/generate-models.py")
            
            return True
                
        except Exception as e:
            print(f"\n❌ ERROR: {e}")
            return False

def main():
    parser = argparse.ArgumentParser(description='🏗️ Entity Generator for .NET + Blazor')
    
    parser.add_argument('--entity', required=True,
                       help='Nombre singular de la entidad (ej: Producto)')
    parser.add_argument('--plural', required=True,
                       help='Nombre plural de la entidad (ej: Productos)')
    parser.add_argument('--module', required=True,
                       help='Nombre del módulo (ej: Ventas)')
    parser.add_argument('--fields', nargs='*', default=[],
                       help='Campos adicionales: "nombre:tipo:tamaño"')
    parser.add_argument('--project-root', default='.',
                       help='Ruta raíz del proyecto (default: .)')
    
    args = parser.parse_args()
    
    generator = EntityGenerator(args.project_root)
    
    try:
        success = generator.run(
            entity_name=args.entity,
            plural_name=args.plural,
            module_name=args.module,
            fields=args.fields
        )
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⏹️  Proceso cancelado por el usuario")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ ERROR inesperado: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()