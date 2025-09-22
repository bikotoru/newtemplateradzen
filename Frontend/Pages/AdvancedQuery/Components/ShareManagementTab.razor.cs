using Microsoft.AspNetCore.Components;
using Frontend.Pages.AdvancedQuery;
using Radzen;

namespace Frontend.Pages.AdvancedQuery.Components
{
    public partial class ShareManagementTab : ComponentBase
    {
        [Parameter] public SavedQueryDto? SavedQuery { get; set; }
        [Parameter] public EventCallback OnShared { get; set; }

        [Inject] private SavedQueryService SavedQueryService { get; set; } = null!;
        [Inject] private DialogService DialogService { get; set; } = null!;
        [Inject] private NotificationService NotificationService { get; set; } = null!;

        private bool isLoading = false;
        private List<SavedQueryShareDto>? shares;
        private bool showShareModal = false;
        private bool showEditModal = false;
        private SavedQueryShareDto? selectedShare;
        private SavedQueryDto? savedQuery;

        protected override async Task OnParametersSetAsync()
        {
            if (SavedQuery != null && SavedQuery != savedQuery)
            {
                savedQuery = SavedQuery;
                await LoadShares();
            }
        }

        private async Task LoadShares()
        {
            if (savedQuery == null) return;

            try
            {
                isLoading = true;
                var response = await SavedQueryService.GetSharesAsync(savedQuery.Id);
                if (response.Success)
                {
                    shares = response.Data;
                }
                else
                {
                    shares = new List<SavedQueryShareDto>();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error al cargar compartidos: {ex.Message}",
                    Duration = 5000
                });
                shares = new List<SavedQueryShareDto>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task OpenShareModal()
        {
            showShareModal = true;
            StateHasChanged();
        }

        private async Task EditShare(SavedQueryShareDto share)
        {
            selectedShare = share;
            showEditModal = true;
            StateHasChanged();
        }

        private async Task RevokeShare(SavedQueryShareDto share)
        {
            if (savedQuery == null) return;

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
                    var response = await SavedQueryService.RevokeShareAsync(savedQuery.Id, share.Id);
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
                        await LoadShares();
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

        private async Task OnShareUpdated()
        {
            showEditModal = false;
            selectedShare = null;
            await LoadShares();
            await OnShared.InvokeAsync();
            StateHasChanged();
        }
    }
}