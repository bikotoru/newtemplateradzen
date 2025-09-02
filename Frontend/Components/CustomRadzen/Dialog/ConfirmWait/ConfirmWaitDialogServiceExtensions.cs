using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Extension methods for the Radzen DialogService
    /// </summary>
    public static class ConfirmWaitDialogServiceExtensions
    {
        /// <summary>
        /// Displays a confirmation dialog with a waiting period before the confirm button is enabled.
        /// </summary>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="message">The message displayed to the user.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public static async Task<bool?> ConfirmWait(
            this DialogService dialogService,
            string message = "Confirm?",
            string title = "Confirm",
            ConfirmWaitOptions options = null,
            CancellationToken? cancellationToken = null)
        {
            // Validate and set default values for the dialog options
            options ??= new ConfirmWaitOptions();
            options.OkButtonText = !string.IsNullOrEmpty(options.OkButtonText) ? options.OkButtonText : "Ok";
            options.CancelButtonText = !string.IsNullOrEmpty(options.CancelButtonText) ? options.CancelButtonText : "Cancel";
            options.Width = !string.IsNullOrEmpty(options.Width) ? options.Width : ""; // Width is set to 600px by default by OpenAsync
            options.Style = !string.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !string.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-confirm {options.CssClass}" : "rz-dialog-confirm";
            options.WrapperCssClass = !string.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";

            return await dialogService.OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-message");
                    b.AddContent(i++, message);
                    b.CloseElement();
                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-buttons");

                    // Using our custom CountdownButton component for the second method too
                    // b.OpenComponent<CountdownButton>(i++); // Comentado temporalmente
                    // Comentado temporalmente para CountdownButton
                    /*
                    b.AddAttribute(i++, "Text", options.OkButtonText);
                    b.AddAttribute(i++, "WaitSeconds", options.WaitSeconds);
                    b.AddAttribute(i++, "CountdownFormat", options.CountdownFormat);
                    b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Primary);
                    b.AddAttribute(i++, "ShowBusyIndicator", options.ShowBusyIndicator);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<MouseEventArgs>(dialogService, () => ds.Close(true)));
                    b.CloseComponent();
                    */

                    // Regular cancel button
                    b.OpenComponent<Radzen.Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.CancelButtonText);
                    b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Light);
                    b.AddAttribute(i++, "Style", "margin-bottom: 10px; width: 150px");
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<MouseEventArgs>(dialogService, () => ds.Close(false)));
                    b.CloseComponent();
                    b.CloseElement();
                };
                return content;
            }, options, cancellationToken);
        }

        /// <summary>
        /// Displays a confirmation dialog with a waiting period before the confirm button is enabled.
        /// </summary>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="message">The message displayed to the user as RenderFragment.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public static async Task<bool?> ConfirmWait(
            this DialogService dialogService,
            RenderFragment message,
            string title = "Confirm",
            ConfirmWaitOptions options = null,
            CancellationToken? cancellationToken = null)
        {
            // Validate and set default values for the dialog options
            options ??= new ConfirmWaitOptions();
            options.OkButtonText = !string.IsNullOrEmpty(options.OkButtonText) ? options.OkButtonText : "Ok";
            options.CancelButtonText = !string.IsNullOrEmpty(options.CancelButtonText) ? options.CancelButtonText : "Cancel";
            options.Width = !string.IsNullOrEmpty(options.Width) ? options.Width : ""; // Width is set to 600px by default by OpenAsync
            options.Style = !string.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !string.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-confirm {options.CssClass}" : "rz-dialog-confirm";
            options.WrapperCssClass = !string.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";

            return await dialogService.OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-message");
                    b.AddContent(i++, message);
                    b.CloseElement();
                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-buttons");

                    // Using our custom CountdownButton component
                    // b.OpenComponent<CountdownButton>(i++); // Comentado temporalmente
                    b.AddAttribute(i++, "Text", options.OkButtonText);
                    b.AddAttribute(i++, "WaitSeconds", options.WaitSeconds);
                    b.AddAttribute(i++, "CountdownFormat", options.CountdownFormat);
                    b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Primary);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<MouseEventArgs>(dialogService, () => ds.Close(true)));
                    b.CloseComponent();

                    // Regular cancel button
                    b.OpenComponent<Radzen.Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.CancelButtonText);
                    b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Light);
                    b.AddAttribute(i++, "Style", "margin-bottom: 10px; width: 150px");
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<MouseEventArgs>(dialogService, () => ds.Close(false)));
                    b.CloseComponent();
                    b.CloseElement();
                };
                return content;
            }, options, cancellationToken);
        }
    }
}