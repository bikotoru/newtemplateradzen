using Microsoft.AspNetCore.Components;
using Frontend.Pages.AdvancedQuery;
using Radzen;

namespace Frontend.Pages.AdvancedQuery.Components
{
    public partial class ShareQueryModal : ComponentBase
    {
        [Parameter] public SavedQueryDto? SavedQuery { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public EventCallback OnShared { get; set; }

        [Inject] private SavedQueryService SavedQueryService { get; set; } = null!;
        [Inject] private DialogService DialogService { get; set; } = null!;
        [Inject] private NotificationService NotificationService { get; set; } = null!;

        private bool isVisible = true;
        private List<AvailableUserDto>? availableUsers;
        private List<AvailableRoleDto>? availableRoles;
        private List<SavedQueryShareDto>? currentShares;
        private string? selectedUserId;
        private string? selectedRoleId;
        private CreateShareRequest userPermissions = new();
        private CreateShareRequest rolePermissions = new();

        protected override async Task OnInitializedAsync()
        {
            if (SavedQuery != null)
            {
                await LoadAvailableUsersAndRoles();
                await LoadCurrentShares();
            }
        }

        private async Task LoadAvailableUsersAndRoles()
        {
            if (SavedQuery == null) return;

            try
            {
                var usersTask = SavedQueryService.GetAvailableUsersAsync(SavedQuery.Id);
                var rolesTask = SavedQueryService.GetAvailableRolesAsync(SavedQuery.Id);

                await Task.WhenAll(usersTask, rolesTask);

                var usersResponse = await usersTask;
                var rolesResponse = await rolesTask;

                availableUsers = usersResponse.Success ? usersResponse.Data : new List<AvailableUserDto>();
                availableRoles = rolesResponse.Success ? rolesResponse.Data : new List<AvailableRoleDto>();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error al cargar usuarios y roles: {ex.Message}",
                    Duration = 5000
                });
                availableUsers = new List<AvailableUserDto>();
                availableRoles = new List<AvailableRoleDto>();
            }
        }

        private async Task LoadCurrentShares()
        {
            if (SavedQuery == null) return;

            try
            {
                var response = await SavedQueryService.GetSharesAsync(SavedQuery.Id);
                currentShares = response.Success ? response.Data : new List<SavedQueryShareDto>();
            }
            catch (Exception ex)
            {
                currentShares = new List<SavedQueryShareDto>();
            }
        }

        private async Task ShareWithUser()
        {
            if (SavedQuery == null || string.IsNullOrEmpty(selectedUserId)) return;

            try
            {
                await DialogService.OpenLoadingAsync("Compartiendo...");
                var response = await SavedQueryService.ShareWithUserAsync(
                    SavedQuery.Id, 
                    Guid.Parse(selectedUserId), 
                    userPermissions
                );
                DialogService.Close();

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Éxito",
                        Detail = "Búsqueda compartida con usuario exitosamente",
                        Duration = 4000
                    });
                    
                    selectedUserId = null;
                    userPermissions = new CreateShareRequest();
                    await LoadCurrentShares();
                    await OnShared.InvokeAsync();
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al compartir con usuario",
                        Duration = 5000
                    });
                }
            }
            catch (Exception ex)
            {
                DialogService.Close();
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error inesperado: {ex.Message}",
                    Duration = 5000
                });
            }
        }

        private async Task ShareWithRole()
        {
            if (SavedQuery == null || string.IsNullOrEmpty(selectedRoleId)) return;

            try
            {
                await DialogService.OpenLoadingAsync("Compartiendo...");
                var response = await SavedQueryService.ShareWithRoleAsync(
                    SavedQuery.Id, 
                    Guid.Parse(selectedRoleId), 
                    rolePermissions
                );
                DialogService.Close();

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Éxito",
                        Detail = "Búsqueda compartida con rol exitosamente",
                        Duration = 4000
                    });
                    
                    selectedRoleId = null;
                    rolePermissions = new CreateShareRequest();
                    await LoadCurrentShares();
                    await OnShared.InvokeAsync();
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al compartir con rol",
                        Duration = 5000
                    });
                }
            }
            catch (Exception ex)
            {
                DialogService.Close();
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error inesperado: {ex.Message}",
                    Duration = 5000
                });
            }
        }

        private async Task RevokeShare(SavedQueryShareDto share)
        {
            if (SavedQuery == null) return;

            var confirmed = await DialogService.Confirm(
                $"¿Está seguro que desea revocar el compartido con '{share.SharedWithName}'?",
                "Confirmar Revocación",
                new ConfirmOptions { OkButtonText = "Sí, Revocar", CancelButtonText = "Cancelar" }
            );

            if (confirmed == true)
            {
                try
                {
                    await DialogService.OpenLoadingAsync("Revocando...");
                    var response = await SavedQueryService.RevokeShareAsync(SavedQuery.Id, share.Id);
                    DialogService.Close();

                    if (response.Success)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Éxito",
                            Detail = "Compartido revocado exitosamente",
                            Duration = 4000
                        });
                        await LoadCurrentShares();
                        await OnShared.InvokeAsync();
                    }
                    else
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Error,
                            Summary = "Error",
                            Detail = response.Message ?? "Error al revocar compartido",
                            Duration = 5000
                        });
                    }
                }
                catch (Exception ex)
                {
                    DialogService.Close();
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = $"Error inesperado: {ex.Message}",
                        Duration = 5000
                    });
                }
            }
        }

        private async Task Close()
        {
            isVisible = false;
            await OnClose.InvokeAsync();
        }
    }
}