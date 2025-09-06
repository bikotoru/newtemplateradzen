using Microsoft.AspNetCore.Components;
using Radzen;
using System;
using System.Threading.Tasks;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Opciones para el diálogo de carga con spinner elegante
    /// </summary>
    public class LoadingDialogOptions : DialogOptions
    {
        /// <summary>
        /// Obtiene o establece el texto principal que se mostrará
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Obtiene o establece el subtítulo (opcional)
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// Obtiene o establece el tamaño del spinner (predeterminado: "48px")
        /// </summary>
        public string SpinnerSize { get; set; } = "48px";

        /// <summary>
        /// Obtiene o establece el color primario del spinner (predeterminado: "#0078D4" - Azul Microsoft)
        /// </summary>
        public string PrimaryColor { get; set; } = "#0078D4";

        /// <summary>
        /// Obtiene o establece el color secundario del spinner (predeterminado: "#50B0FF" - Azul claro)
        /// </summary>
        public string SecondaryColor { get; set; } = "#50B0FF";

        /// <summary>
        /// Obtiene o establece el grosor del borde del spinner (predeterminado: "4px")
        /// </summary>
        public string BorderWidth { get; set; } = "4px";
    }

    /// <summary>
    /// Métodos de extensión para DialogService para mostrar diálogos de carga con spinner elegante
    /// </summary>
    public static class LoadingDialogExtensions
    {
        /// <summary>
        /// Muestra un diálogo de carga que no se puede cerrar hasta que se invoque DialogService.Close()
        /// </summary>
        /// <param name="dialogService">El servicio de diálogo</param>
        /// <param name="text">El texto principal que se mostrará</param>
        /// <param name="subtitle">El subtítulo opcional</param>
        /// <param name="options">Opciones adicionales del diálogo de carga</param>
        /// <returns>Una tarea que se completa cuando se cierra el diálogo</returns>
        public static async Task OpenLoadingAsync(this DialogService dialogService, string title, string subtitle = null, LoadingDialogOptions options = null)
        {
            options ??= new LoadingDialogOptions();
            options.Text = title;
            options.Subtitle = subtitle;

            // Configurar el diálogo
            ConfigureDialogOptions(options);

            _ = Task.Run(async () => await dialogService.OpenAsync(string.Empty, ds => RenderLoadingContent(ds, options), options));
            await Task.Delay(100);
        }

        /// <summary>
        /// Muestra un diálogo de carga con opciones personalizadas
        /// </summary>
        /// <param name="dialogService">El servicio de diálogo</param>
        /// <param name="options">Opciones del diálogo de carga</param>
        /// <returns>Una tarea que se completa cuando se cierra el diálogo</returns>
        public static async Task OpenLoadingAsync(this DialogService dialogService, LoadingDialogOptions options)
        {
            // Configurar el diálogo
            ConfigureDialogOptions(options);

            // Usar título vacío porque no lo necesitamos
            _ = Task.Run(async () => await dialogService.OpenAsync(string.Empty, ds => RenderLoadingContent(ds, options), options));
            await Task.Delay(100);
        }


        /// <summary>
        /// Configura las opciones del diálogo para un estilo elegante y seguro
        /// </summary>
        private static void ConfigureDialogOptions(LoadingDialogOptions options)
        {
            // Ocultar elementos innecesarios
            options.ShowTitle = false;
            options.ShowClose = false;

            // Prevenir cierre accidental
            options.CloseDialogOnEsc = false;
            options.CloseDialogOnOverlayClick = false;

            // Establecer ancho y estilos si no están definidos
            options.Width = !string.IsNullOrEmpty(options.Width) ? options.Width : "350px";

            // Establecer clases CSS
            options.CssClass = !string.IsNullOrEmpty(options.CssClass)
                ? $"ms-loading-dialog {options.CssClass}"
                : "ms-loading-dialog";

            options.WrapperCssClass = !string.IsNullOrEmpty(options.WrapperCssClass)
                ? $"ms-loading-dialog-wrapper {options.WrapperCssClass}"
                : "ms-loading-dialog-wrapper";

            options.ContentCssClass = !string.IsNullOrEmpty(options.ContentCssClass)
                ? $"ms-loading-dialog-content {options.ContentCssClass}"
                : "ms-loading-dialog-content";
        }

        private static RenderFragment RenderLoadingContent(DialogService ds, LoadingDialogOptions options)
        {
            return builder =>
            {
                var i = 0;

                // Contenedor principal
                builder.OpenElement(i++, "div");
                builder.AddAttribute(i++, "class", "ms-loading-container");

                // Spinner
                builder.OpenElement(i++, "div");
                builder.AddAttribute(i++, "class", "ms-dual-spinner");
                builder.AddAttribute(i++, "style", $@"
                    width: {options.SpinnerSize}; 
                    height: {options.SpinnerSize};
                    border-top: {options.BorderWidth} solid {options.PrimaryColor};
                    border-right: {options.BorderWidth} solid transparent;
                ");
                builder.CloseElement(); // Cierre spinner

                // Contenedor de texto
                builder.OpenElement(i++, "div");
                builder.AddAttribute(i++, "class", "ms-loading-text-container");

                // Texto principal
                builder.OpenElement(i++, "p");
                builder.AddAttribute(i++, "class", "ms-loading-text");
                builder.AddContent(i++, options.Text);
                builder.CloseElement();

                // Subtítulo (si existe)
                if (!string.IsNullOrEmpty(options.Subtitle))
                {
                    builder.OpenElement(i++, "p");
                    builder.AddAttribute(i++, "class", "ms-loading-subtitle");
                    builder.AddContent(i++, options.Subtitle);
                    builder.CloseElement();
                }

                builder.CloseElement(); // Cierre contenedor de texto
                builder.CloseElement(); // Cierre contenedor principal

                // Estilos CSS para el spinner y el diálogo
                builder.OpenElement(i++, "style");
                builder.AddContent(i++, $@"
                    /* Estilos generales del diálogo */
                    .ms-loading-dialog .rz-dialog-content {{
                        padding: 24px !important;
                        overflow: hidden !important;
                        border-radius: 4px !important;
                        box-shadow: 0 6.4px 14.4px rgba(0, 0, 0, 0.13), 0 1.2px 3.6px rgba(0, 0, 0, 0.1) !important;
                    }}
                    
                    .ms-loading-dialog-wrapper {{
                        background-color: rgba(255, 255, 255, 0.6) !important;
                        backdrop-filter: blur(2px) !important;
                    }}
                    
                    /* Contenedor principal */
                    .ms-loading-container {{
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        justify-content: center;
                        text-align: center;
                        font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, 'Helvetica Neue', sans-serif;
                    }}
                    
                    /* Contenedor de texto */
                    .ms-loading-text-container {{
                        margin-top: 24px;
                        width: 100%;
                    }}
                    
                    /* Texto principal */
                    .ms-loading-text {{
                        font-size: 16px;
                        font-weight: 600;
                        color: #323130;
                        margin: 0;
                        line-height: 1.4;
                    }}
                    
                    /* Subtítulo */
                    .ms-loading-subtitle {{
                        font-size: 14px;
                        color: #605E5C;
                        margin: 8px 0 0 0;
                        line-height: 1.4;
                        font-weight: 400;
                    }}
                    
                    /* Spinner dual */
                    .ms-dual-spinner {{
                        display: inline-block;
                        border-radius: 50%;
                        box-sizing: border-box;
                        animation: rotation 1s linear infinite;
                        position: relative;
                    }}
                    
                    .ms-dual-spinner::after {{
                        content: '';
                        box-sizing: border-box;
                        position: absolute;
                        left: 0;
                        top: 0;
                        width: 100%;
                        height: 100%;
                        border-radius: 50%;
                        border-left: {options.BorderWidth} solid {options.SecondaryColor};
                        border-bottom: {options.BorderWidth} solid transparent;
                        animation: rotation 0.5s linear infinite reverse;
                    }}
                    
                    @keyframes rotation {{
                        0% {{ transform: rotate(0deg); }}
                        100% {{ transform: rotate(360deg); }}
                    }}
                ");
                builder.CloseElement(); // Cierre de estilos
            };
        }
    }
}