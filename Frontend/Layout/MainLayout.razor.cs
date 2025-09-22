using Frontend.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Frontend.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] protected AuthService AuthService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    protected bool sidebar1Expanded = true;

    protected override void OnInitialized()
    {
        try 
        {
            // Solo suscribirse a cambios en el estado de auth
            // NO inicializar el AuthService aquí para evitar bloqueos
            AuthService.OnAuthStateChanged += OnAuthStateChanged;
        }
        catch (Exception ex)
        {
        }
    }

    private async void OnAuthStateChanged()
    {
        try 
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
        }
    }

    protected string GetDisplayName()
    {
        try
        {
            var name = AuthService.GetDisplayName();
            return name;
        }
        catch (Exception ex)
        {
            return "Usuario";
        }
    }

    protected string GetUserEmail()
    {
        try
        {
            var email = AuthService.Session.Email;
            return email ?? "user@example.com";
        }
        catch (Exception ex)
        {
            return "user@example.com";
        }
    }

    protected bool HasAdminPermissions()
    {
        try
        {
            return AuthService.HasAnyPermission(new[] { 
                "SYSTEMUSER.VIEW", "SYSTEMUSER.VIEWMENU",
                "SYSTEMROLE.VIEW", "SYSTEMROLE.VIEWMENU", 
                "SYSTEMPERMISSION.VIEW", "SYSTEMPERMISSION.VIEWMENU"
            });
        }
        catch
        {
            return false;
        }
    }

    protected async Task ProfileMenuClick(RadzenProfileMenuItem args)
    {
        if (args.Value == "Logout")
        {
            await AuthService.LogoutAsync();
            Navigation.NavigateTo("/");
        }
    }

    protected void NavigateToAdvancedQuery()
    {
        Navigation.NavigateTo("/advanced-query");
    }

    public void Dispose()
    {
        try
        {
            AuthService.OnAuthStateChanged -= OnAuthStateChanged;
        }
        catch (Exception ex)
        {
        }
    }
}