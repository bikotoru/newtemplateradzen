using System;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Tipos de campos disponibles para el formulario dinámico
    /// </summary>
    public enum FormFieldType
    {
        /// <summary>
        /// Campo de texto de una línea (RadzenTextBox)
        /// </summary>
        Text,

        /// <summary>
        /// Campo de texto multilínea (RadzenTextArea)
        /// </summary>
        TextArea,

        /// <summary>
        /// Campo numérico (RadzenNumeric)
        /// </summary>
        Numeric,

        /// <summary>
        /// Campo de fecha (RadzenDatePicker)
        /// </summary>
        Date,

        /// <summary>
        /// Campo de fecha y hora (RadzenDatePicker con ShowTime=true)
        /// </summary>
        DateTime,

        /// <summary>
        /// Campo de selección (RadzenDropDown)
        /// </summary>
        Select,

        /// <summary>
        /// Campo de casilla de verificación (RadzenCheckBox)
        /// </summary>
        Checkbox,

        /// <summary>
        /// Campo de radio botones (RadzenRadioButtonList)
        /// </summary>
        Radio
    }
}