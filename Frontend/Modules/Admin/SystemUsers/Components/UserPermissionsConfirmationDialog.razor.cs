using Microsoft.AspNetCore.Components;
using Shared.Models.DTOs.UserPermissions;

namespace Frontend.Modules.Admin.SystemUsers.Components
{
    public partial class UserPermissionsConfirmationDialog : ComponentBase
    {
        [Parameter] public string UserName { get; set; } = string.Empty;
        [Parameter] public List<UserPermissionDto> PermissionsToAdd { get; set; } = new();
        [Parameter] public List<UserPermissionDto> PermissionsToRemove { get; set; } = new();
        [Parameter] public EventCallback OnConfirm { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        protected int TotalChanges => PermissionsToAdd.Count + PermissionsToRemove.Count;
        protected bool HasChanges => TotalChanges > 0;
        protected bool HasCriticalChanges => PermissionsToRemove.Any();

        protected async Task Confirm()
        {
            if (OnConfirm.HasDelegate)
            {
                await OnConfirm.InvokeAsync();
            }
        }

        protected async Task Cancel()
        {
            if (OnCancel.HasDelegate)
            {
                await OnCancel.InvokeAsync();
            }
        }
    }
}