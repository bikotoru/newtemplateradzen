using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Web;

namespace Frontend.Components.Base;

public partial class CrmTabs : ComponentBase, IDisposable
{
    [Parameter] public string DefaultTabId { get; set; } = "";
    [Parameter] public EventCallback<string> OnTabChanged { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    /// <summary>
    /// Desactiva la sincronización con URL (no agrega parámetros ?tab=xxx)
    /// </summary>
    [Parameter] public bool DisableUrlSync { get; set; } = false;

    private readonly List<CrmTabModel> _tabs = new();
    private readonly HashSet<string> _visitedTabs = new(); // Tabs que ya fueron visitados
    private string _activeTabId = "";
    private bool _initialized = false;
    private bool _firstRender = true;
    private bool _isUpdating = false;

    protected override async Task OnInitializedAsync()
    {
        // Suscribirse a cambios de navegación para detectar cambios en la URL
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _firstRender = false;
            
            // Dar tiempo mínimo para que se registren los tabs child
            await Task.Delay(50);
            
            // Inicializar desde URL
            if (!_initialized && _tabs.Any())
            {
                await InitializeFromUrl();
            }
            
            // Inicializar JavaScript de tabs
            await JSRuntime.InvokeVoidAsync("CrmComponents.initTabNavigation");
            await JSRuntime.InvokeVoidAsync("CrmComponents.initTabs");
        }
    }

    private async Task InitializeFromUrl()
    {
        string? tabToActivate = null;
        
        // Solo leer de URL si no está desactivada la sincronización
        if (!DisableUrlSync)
        {
            var uri = new Uri(NavigationManager.Uri);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var tabFromUrl = query["tab"];

            if (!string.IsNullOrEmpty(tabFromUrl) && _tabs.Any(t => t.Id == tabFromUrl))
            {
                tabToActivate = tabFromUrl;
            }
        }
        
        // Si no hay tab de URL o está desactivado, usar DefaultTabId o el primero
        if (string.IsNullOrEmpty(tabToActivate))
        {
            if (!string.IsNullOrEmpty(DefaultTabId) && _tabs.Any(t => t.Id == DefaultTabId))
            {
                tabToActivate = DefaultTabId;
            }
            else if (_tabs.Any())
            {
                tabToActivate = _tabs.First().Id;
            }
        }

        if (!string.IsNullOrEmpty(tabToActivate))
        {
            await SetActiveTab(tabToActivate, !DisableUrlSync);
        }

        _initialized = true;
        StateHasChanged();
    }

    private async void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        if (!_initialized || DisableUrlSync) return;

        var uri = new Uri(e.Location);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var tabFromUrl = query["tab"];

        if (!string.IsNullOrEmpty(tabFromUrl) && _tabs.Any(t => t.Id == tabFromUrl) && _activeTabId != tabFromUrl)
        {
            await SetActiveTab(tabFromUrl, false);
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task SetActiveTab(string tabId, bool updateUrl = true)
    {
        if (string.IsNullOrEmpty(tabId) || !_tabs.Any(t => t.Id == tabId) || _activeTabId == tabId || _isUpdating)
            return;

        _isUpdating = true;

        try
        {
            // Solo cambiar si es diferente
            _activeTabId = tabId;

            // Marcar el tab como visitado (lazy loading con caché)
            _visitedTabs.Add(tabId);

            // Actualizar estado de tabs - Optimizado para solo cambiar el estado
            foreach (var tab in _tabs)
            {
                tab.IsActive = tab.Id == tabId;
            }

            // Actualizar URL si es necesario y no está desactivado
            if (updateUrl && !DisableUrlSync)
            {
                var uri = new Uri(NavigationManager.Uri);
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["tab"] = tabId;

                var newUrl = $"{uri.GetLeftPart(UriPartial.Path)}?{query}";
                NavigationManager.NavigateTo(newUrl, false);
            }

            // Actualizar JavaScript
            if (_initialized)
            {
                await JSRuntime.InvokeVoidAsync("CrmComponents.setActiveTab", tabId);
            }

            // Forzar re-render
            StateHasChanged();
            
            // Invocar callback después del render
            if (OnTabChanged.HasDelegate)
            {
                await OnTabChanged.InvokeAsync(tabId);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    public void AddTab(string id, string title, string icon, string iconColor, string titleColor, RenderFragment content, bool isVisible = true)
    {
        // Evitar duplicados
        if (_tabs.Any(t => t.Id == id))
            return;

        var tab = new CrmTabModel
        {
            Id = id,
            Title = title,
            Icon = icon,
            IconColor = iconColor,
            TitleColor = titleColor,
            Content = content,
            IsVisible = isVisible,
            IsActive = false
        };

        _tabs.Add(tab);

        // Auto-inicializar si no se ha hecho aún
        if (!_initialized && !_firstRender && _tabs.Count > 0)
        {
            _ = Task.Run(async () => {
                await Task.Delay(25);
                await InvokeAsync(async () => {
                    if (!_initialized)
                    {
                        await InitializeFromUrl();
                    }
                });
            });
        }

        StateHasChanged();
    }

    public async Task RemoveTab(string id)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == id);
        if (tab == null) return;

        _tabs.Remove(tab);

        // Si era el tab activo, activar el primero disponible
        if (tab.IsActive && _tabs.Any())
        {
            await SetActiveTab(_tabs.First().Id);
        }
        else
        {
            StateHasChanged();
        }
    }

    public void UpdateTabTitle(string id, string newTitle)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == id);
        if (tab != null)
        {
            tab.Title = newTitle;
            StateHasChanged();
        }
    }

    public void SetTabVisibility(string id, bool isVisible)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == id);
        if (tab != null)
        {
            tab.IsVisible = isVisible;
            StateHasChanged();
        }
    }

    public string GetActiveTabId() => _activeTabId;

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}

public class CrmTabModel
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public string IconColor { get; set; } = "";
    public string TitleColor { get; set; } = "";
    public bool IsActive { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public RenderFragment? Content { get; set; }
}