using Frontend.Components.CustomRadzen.Dialog.FormDialog;
using Microsoft.AspNetCore.Components;
using Radzen;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Extensiones para el DialogService para mostrar diálogos simples de entrada
    /// </summary>
    public static class SimpleDialogExtensions
    {
        /// <summary>
        /// Abre un diálogo con un solo campo de entrada
        /// </summary>
        /// <param name="dialogService">El servicio de diálogos</param>
        /// <param name="title">Título del diálogo</param>
        /// <param name="options">Opciones del diálogo de entrada simple</param>
        /// <returns>El valor ingresado o null si se canceló</returns>
        public static async Task<object> PromptAsync(
            this DialogService dialogService,
            string title,
            SimpleInputDialogOptions options = null
            )
        {
            // Valores por defecto
            options ??= new SimpleInputDialogOptions();
            options.Width = string.IsNullOrEmpty(options.Width) ? "400px" : options.Width;

            var result = await dialogService.OpenAsync<SimpleInputDialog>(title,
                new System.Collections.Generic.Dictionary<string, object>
                {
                    ["Options"] = options,
                    ["DialogService"] = dialogService
                },
                options
                );

            return result;
        }

        /// <summary>
        /// Abre un diálogo simple para ingresar texto
        /// </summary>
        /// <param name="dialogService">El servicio de diálogos</param>
        /// <param name="title">Título del diálogo</param>
        /// <param name="label">Etiqueta del campo</param>
        /// <param name="defaultValue">Valor predeterminado</param>
        /// <param name="required">Indica si el campo es obligatorio</param>
        /// <returns>El texto ingresado o null si se canceló</returns>
        public static async Task<string> PromptTextAsync(
            this DialogService dialogService,
            string title,
            string label = "Texto",
            string defaultValue = "",
            bool required = true)
        {
            var options = new SimpleInputDialogOptions
            {
                InputType = SimpleInputType.Text,
                Label = label,
                DefaultValue = defaultValue,
                Required = required
            };

            var result = await dialogService.PromptAsync(title, options);
            return result?.ToString();
        }

        /// <summary>
        /// Abre un diálogo simple para ingresar un valor numérico
        /// </summary>
        /// <param name="dialogService">El servicio de diálogos</param>
        /// <param name="title">Título del diálogo</param>
        /// <param name="label">Etiqueta del campo</param>
        /// <param name="defaultValue">Valor predeterminado</param>
        /// <param name="minValue">Valor mínimo permitido</param>
        /// <param name="maxValue">Valor máximo permitido</param>
        /// <param name="required">Indica si el campo es obligatorio</param>
        /// <returns>El número ingresado o null si se canceló</returns>
        public static async Task<decimal?> PromptNumberAsync(
            this DialogService dialogService,
            string title,
            string label = "Cantidad",
            decimal defaultValue = 0,
            decimal? minValue = null,
            decimal? maxValue = null,
            bool required = true)
        {
            var options = new SimpleInputDialogOptions
            {
                InputType = SimpleInputType.Numeric,
                Label = label,
                DefaultValue = defaultValue,
                MinValue = minValue,
                MaxValue = maxValue,
                Required = required
            };

            var result = await dialogService.PromptAsync(title, options);
            if (result == null)
                return null;

            return Convert.ToDecimal(result);
        }

        /// <summary>
        /// Abre un diálogo simple para ingresar texto multilínea
        /// </summary>
        /// <param name="dialogService">El servicio de diálogos</param>
        /// <param name="title">Título del diálogo</param>
        /// <param name="label">Etiqueta del campo</param>
        /// <param name="defaultValue">Valor predeterminado</param>
        /// <param name="rows">Número de filas del textarea</param>
        /// <param name="required">Indica si el campo es obligatorio</param>
        /// <returns>El texto ingresado o null si se canceló</returns>
        public static async Task<string> PromptTextAreaAsync(
            this DialogService dialogService,
            string title,
            string label = "Descripción",
            string defaultValue = "",
            int rows = 5,
            bool required = true)
        {
            var options = new SimpleInputDialogOptions
            {
                InputType = SimpleInputType.TextArea,
                Label = label,
                DefaultValue = defaultValue,
                Rows = rows,
                Required = required
            };

            var result = await dialogService.PromptAsync(title, options);
            return result?.ToString();
        }
    }
}