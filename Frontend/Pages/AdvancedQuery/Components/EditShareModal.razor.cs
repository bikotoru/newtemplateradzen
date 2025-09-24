using Microsoft.AspNetCore.Components;
using Frontend.Pages.AdvancedQuery;
using Radzen;

namespace Frontend.Pages.AdvancedQuery.Components
{
    public partial class EditShareModal : ComponentBase
    {
        [Parameter] public SavedQueryShareDto? Share { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public EventCallback OnUpdated { get; set; }

        [Inject] private SavedQueryService SavedQueryService { get; set; } = null!;
        [Inject] private DialogService DialogService { get; set; } = null!;
        [Inject] private NotificationService NotificationService { get; set; } = null!;

        private bool isVisible = true;
        private bool isProcessing = false;
        private string validationMessage = "";
        private UpdateShareRequest permissions = new();

        protected override void OnParametersSet()
        {
            if (Share != null)
            {
                permissions = new UpdateShareRequest
                {
                    CanView = Share.CanView,
                    CanEdit = Share.CanEdit,
                    CanExecute = Share.CanExecute,
                    CanShare = Share.CanShare
                };
            }
        }

        private async Task UpdatePermissions()
        {
            if (Share == null) return;

            // Validación básica
            if (!permissions.CanView && !permissions.CanEdit && !permissions.CanExecute && !permissions.CanShare)
            {
                validationMessage = "Debe seleccionar al menos un permiso";
                StateHasChanged();
                return;
            }

            validationMessage = "";

            try
            {
                isProcessing = true;
                StateHasChanged();

                var response = await SavedQueryService.UpdateShareAsync(Share.SavedQueryId, Share.Id, permissions);

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Éxito",
                        Detail = "Permisos actualizados exitosamente",
                        Duration = 4000
                    });

                    await OnUpdated.InvokeAsync();
                    await Close();
                }
                else
                {
                    validationMessage = response.Message ?? "Error al actualizar los permisos";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                validationMessage = $"Error inesperado: {ex.Message}";
                StateHasChanged();
            }
            finally
            {
                isProcessing = false;
                StateHasChanged();
            }
        }

        private async Task Close()
        {
            isVisible = false;
            await OnClose.InvokeAsync();
        }
    }
}