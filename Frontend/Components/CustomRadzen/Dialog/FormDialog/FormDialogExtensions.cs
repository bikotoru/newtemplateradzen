using Frontend.Components.CustomRadzen.Dialog.FormDialog;
using Microsoft.AspNetCore.Components;
using Radzen;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Extensiones para el DialogService para manejo de formularios dinámicos
    /// </summary>
    public static class FormDialogExtensions
    {
        /// <summary>
        /// Abre un diálogo con un formulario dinámico basado en la configuración proporcionada
        /// </summary>
        /// <param name="dialogService">El servicio de diálogos</param>
        /// <param name="title">Título del diálogo</param>
        /// <param name="options">Opciones del formulario</param>
        /// <param name="cancellationToken">Token de cancelación opcional</param>
        /// <returns>Un diccionario con los valores del formulario o null si se canceló</returns>
        public static async Task<Dictionary<string, object>> OpenFormAsync(
            this DialogService dialogService,
            string title,
            FormDialogOptions options = null
            )
        {
            // Asegurar que hay opciones
            options ??= new FormDialogOptions();

            // Aplicar valores por defecto para las opciones del diálogo
            options.Width = !string.IsNullOrEmpty(options.Width) ? options.Width : "650px";
            options.Style = !string.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !string.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-form {options.CssClass}" : "rz-dialog-form";
            options.WrapperCssClass = !string.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";

            var result = await dialogService.OpenAsync<DynamicFormDialog>(title,
                new Dictionary<string, object>
                {
                    ["Options"] = options,
                    ["DialogService"] = dialogService
                },
                options
                );

            return result as Dictionary<string, object>;
        }

        /// <summary>
        /// Abre un diálogo con un formulario dinámico basado en la configuración proporcionada
        /// </summary>
        /// <param name="dialogService">El servicio de diálogos</param>
        /// <param name="title">Título del diálogo como RenderFragment</param>
        /// <param name="options">Opciones del formulario</param>
        /// <param name="cancellationToken">Token de cancelación opcional</param>
        /// <returns>Un diccionario con los valores del formulario o null si se canceló</returns>
        public static async Task<Dictionary<string, object>> OpenFormAsync(
            this DialogService dialogService,
            RenderFragment title,
            FormDialogOptions options = null
            )
        {
            // Asegurar que hay opciones
            options ??= new FormDialogOptions();

            // Aplicar valores por defecto para las opciones del diálogo
            options.Width = !string.IsNullOrEmpty(options.Width) ? options.Width : "650px";
            options.Style = !string.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !string.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-form {options.CssClass}" : "rz-dialog-form";
            options.WrapperCssClass = !string.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";
            options.TitleContent = ds => title;

            var result = await dialogService.OpenAsync<DynamicFormDialog>(string.Empty,
                new Dictionary<string, object>
                {
                    ["Options"] = options,
                    ["DialogService"] = dialogService
                },
                options
                );

            return result as Dictionary<string, object>;
        }
    }
}