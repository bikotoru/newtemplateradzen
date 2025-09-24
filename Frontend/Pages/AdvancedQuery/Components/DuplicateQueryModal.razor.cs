using Microsoft.AspNetCore.Components;
using Frontend.Pages.AdvancedQuery;
using Radzen;

namespace Frontend.Pages.AdvancedQuery.Components
{
    public partial class DuplicateQueryModal : ComponentBase
    {
        [Parameter] public SavedQueryDto? SavedQuery { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public EventCallback OnDuplicated { get; set; }

        [Inject] private SavedQueryService SavedQueryService { get; set; } = null!;
        [Inject] private DialogService DialogService { get; set; } = null!;
        [Inject] private NotificationService NotificationService { get; set; } = null!;

        private bool isVisible = true;
        private bool isProcessing = false;
        private string newName = "";
        private string validationMessage = "";

        protected override void OnParametersSet()
        {
            if (SavedQuery != null)
            {
                newName = $"Copia de {SavedQuery.Name}";
            }
        }

        private async Task DuplicateQuery()
        {
            if (SavedQuery == null) return;

            // Validar nombre
            if (string.IsNullOrWhiteSpace(newName))
            {
                validationMessage = "El nombre es obligatorio";
                StateHasChanged();
                return;
            }

            if (newName.Length < 3)
            {
                validationMessage = "El nombre debe tener al menos 3 caracteres";
                StateHasChanged();
                return;
            }

            if (newName.Length > 100)
            {
                validationMessage = "El nombre no puede exceder 100 caracteres";
                StateHasChanged();
                return;
            }

            validationMessage = "";

            try
            {
                isProcessing = true;
                StateHasChanged();

                var response = await SavedQueryService.DuplicateSavedQueryAsync(SavedQuery.Id, newName);

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Éxito",
                        Detail = "Búsqueda duplicada exitosamente",
                        Duration = 4000
                    });

                    await OnDuplicated.InvokeAsync();
                    await Close();
                }
                else
                {
                    validationMessage = response.Message ?? "Error al duplicar la búsqueda";
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