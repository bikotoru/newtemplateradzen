using Radzen;

namespace Frontend.Components.CustomRadzen.Dialog.FormDialog
{
    public static class QuantityDialogExtensions
    {
        public static async Task<int?> ShowQuantityDialogAsync(
            this DialogService dialogService,
            string itemName,
            int defaultQuantity = 1,
            int minQuantity = 1,
            int? maxQuantity = null,
            string? title = null,
            string? description = null)
        {
            var options = new QuantityInputDialogOptions
            {
                ItemName = itemName,
                Title = title,
                Description = description,
                DefaultQuantity = defaultQuantity,
                MinQuantity = minQuantity,
                MaxQuantity = maxQuantity
            };

            return await ShowQuantityDialogAsync(dialogService, options);
        }

        public static async Task<int?> ShowQuantityDialogAsync(
            this DialogService dialogService,
            QuantityInputDialogOptions options)
        {
            var result = await dialogService.OpenAsync<QuantityInputDialog>(
                title: options.Title ?? $"Cantidad de {options.ItemName}",
                parameters: new Dictionary<string, object>
                {
                    { "Options", options },
                    { "DialogService", dialogService }
                },
                new DialogOptions
                {
                    Width = "400px",
                    ShowClose = false,
                    CloseDialogOnOverlayClick = false,
                    CloseDialogOnEsc = false
                });

            return result as int?;
        }
    }
}