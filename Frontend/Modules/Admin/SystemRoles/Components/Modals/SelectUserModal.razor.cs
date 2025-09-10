using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Models.DTOs.RoleUsers;

namespace Frontend.Modules.Admin.SystemRoles.Components.Modals;

public partial class SelectUserModal : ComponentBase
{
    [Inject] private DialogService DialogService { get; set; } = default!;

    [Parameter] public string RoleName { get; set; } = string.Empty;
    [Parameter] public List<RoleUserDto> AvailableUsers { get; set; } = new();

    private string searchTerm = string.Empty;
    private List<RoleUserDto>? filteredUsers;

    protected override void OnInitialized()
    {
        filteredUsers = AvailableUsers;
    }

    private void OnSearchChanged(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredUsers = AvailableUsers;
        }
        else
        {
            var term = searchTerm.ToLower();
            filteredUsers = AvailableUsers
                .Where(u => u.Nombre.ToLower().Contains(term) ||
                           u.Email.ToLower().Contains(term))
                .ToList();
        }
        
        StateHasChanged();
    }

    private void SelectUser(RoleUserDto user)
    {
        DialogService.Close(user);
    }

    private void Cancel()
    {
        DialogService.Close();
    }
}