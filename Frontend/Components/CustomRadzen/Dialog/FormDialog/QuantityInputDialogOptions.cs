namespace Frontend.Components.CustomRadzen.Dialog.FormDialog
{
    public class QuantityInputDialogOptions
    {
        public string Title { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string QuantityLabel { get; set; } = "Cantidad";
        public string Placeholder { get; set; } = "Ingrese la cantidad";
        public int? DefaultQuantity { get; set; } = 1;
        public int MinQuantity { get; set; } = 1;
        public int? MaxQuantity { get; set; }
        public string CancelButtonText { get; set; } = "Cancelar";
        public string ConfirmButtonText { get; set; } = "Confirmar";
        public Func<int, QuantityValidationResult>? CustomValidator { get; set; }
    }

    public class QuantityValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static QuantityValidationResult Valid() => new() { IsValid = true };
        public static QuantityValidationResult Invalid(string message) => new() { IsValid = false, ErrorMessage = message };
    }
}