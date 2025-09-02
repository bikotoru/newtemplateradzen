using Radzen;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Define un campo para el formulario dinámico
    /// </summary>
    public class FormField
    {
        /// <summary>
        /// Nombre del campo (clave en el diccionario de resultados)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Etiqueta a mostrar para el campo
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Tipo de campo
        /// </summary>
        public FormFieldType Type { get; set; } = FormFieldType.Text;

        /// <summary>
        /// Indica si el campo es obligatorio
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// Valor por defecto del campo
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Mensaje de error para validación requerida
        /// </summary>
        public string RequiredMessage { get; set; } = "Este campo es obligatorio";

        /// <summary>
        /// Placeholder para el campo
        /// </summary>
        public string Placeholder { get; set; }

        /// <summary>
        /// Valor mínimo para campos numéricos o fechas
        /// </summary>
        public object MinValue { get; set; }

        /// <summary>
        /// Valor máximo para campos numéricos o fechas
        /// </summary>
        public object MaxValue { get; set; }

        /// <summary>
        /// Opciones para campos de selección (DropDown, RadioButtonList)
        /// </summary>
        public IEnumerable<object> Items { get; set; }

        /// <summary>
        /// Nombre de la propiedad a mostrar para las opciones (campos Select)
        /// </summary>
        public string TextProperty { get; set; }

        /// <summary>
        /// Nombre de la propiedad para el valor de las opciones (campos Select)
        /// </summary>
        public string ValueProperty { get; set; }

        /// <summary>
        /// Clase CSS adicional para el campo
        /// </summary>
        public string CssClass { get; set; }

        /// <summary>
        /// Indice de tab para navegación
        /// </summary>
        public int? TabIndex { get; set; }

        /// <summary>
        /// Indica si el campo está deshabilitado
        /// </summary>
        public bool Disabled { get; set; } = false;

        /// <summary>
        /// Mensaje de ayuda para el campo
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// Función de validación personalizada (debe devolver ValidationResult)
        /// </summary>
        public Func<object, ValidationResult> Validator { get; set; }

        /// <summary>
        /// Función de validación personalizada simple (devuelve true si es válido, false si no)
        /// </summary>
        private Func<object, bool> _simpleValidator;
        public Func<object, bool> SimpleValidator
        {
            get => _simpleValidator;
            set
            {
                _simpleValidator = value;
                if (value != null)
                {
                    // Crear un validador compatible con ValidationResult
                    Validator = (obj) =>
                    {
                        bool isValid = value(obj);
                        return isValid ? ValidationResult.Success : new ValidationResult(SimpleValidatorMessage ?? "El valor no es válido");
                    };
                }
            }
        }

        /// <summary>
        /// Mensaje de error para la validación simple
        /// </summary>
        public string SimpleValidatorMessage { get; set; } = "El valor no es válido";
    }
}