using Radzen;
using System;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Opciones para el diálogo de entrada simple
    /// </summary>
    public class SimpleInputDialogOptions : DialogOptions
    {
        /// <summary>
        /// Tipo de entrada del diálogo
        /// </summary>
        public SimpleInputType InputType { get; set; } = SimpleInputType.Text;

        /// <summary>
        /// Etiqueta del campo
        /// </summary>
        public string Label { get; set; } = "Valor";

        /// <summary>
        /// Texto de placeholder
        /// </summary>
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Valor predeterminado
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Indica si el campo es obligatorio
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// Mensaje de error cuando el campo es obligatorio
        /// </summary>
        public string RequiredMessage { get; set; } = "Este campo es obligatorio";

        /// <summary>
        /// Valor mínimo para campos numéricos
        /// </summary>
        public decimal? MinValue { get; set; }

        /// <summary>
        /// Valor máximo para campos numéricos
        /// </summary>
        public decimal? MaxValue { get; set; }

        /// <summary>
        /// Permite el valor cero en campos numéricos sin considerarlo vacío
        /// </summary>
        public bool AllowZero { get; set; } = false;

        /// <summary>
        /// Número de filas para TextArea
        /// </summary>
        public int Rows { get; set; } = 5;

        /// <summary>
        /// Función de validación personalizada que devuelve true si es válido
        /// </summary>
        public Func<object, bool> Validator { get; set; }

        /// <summary>
        /// Mensaje de error para la validación personalizada
        /// </summary>
        public string ValidationMessage { get; set; } = "El valor no es válido";

        /// <summary>
        /// Texto del botón OK
        /// </summary>
        public string OkButtonText { get; set; } = "Aceptar";

        /// <summary>
        /// Texto del botón Cancelar
        /// </summary>
        public string CancelButtonText { get; set; } = "Cancelar";
    }
}