using Microsoft.AspNetCore.Components;
using Shared.Models.DTOs.UserRoles;
using Radzen;

namespace Frontend.Modules.Admin.SystemUsers.Components.Modals
{
    public partial class RolePermissionsDetailModal : ComponentBase
    {
        [Inject] private DialogService DialogService { get; set; } = default!;

        [Parameter] public AvailableRoleDto Role { get; set; } = default!;
    }
}