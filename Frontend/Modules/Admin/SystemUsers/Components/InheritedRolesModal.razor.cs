using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Modules.Admin.SystemUsers.Components
{
    public partial class InheritedRolesModal : ComponentBase
    {
        [Parameter] public string PermissionName { get; set; } = string.Empty;
        [Parameter] public List<string> InheritedFromRoles { get; set; } = new();
        [Parameter] public EventCallback OnClose { get; set; }

        protected async Task Close()
        {
            if (OnClose.HasDelegate)
            {
                await OnClose.InvokeAsync();
            }
        }
    }
}