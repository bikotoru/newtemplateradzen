using Radzen;
using System;
using System.Collections.Generic;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Opciones para el diálogo de formulario dinámico
    /// </summary>
    public class FormDialogOptions : DialogOptions
    {
        /// <summary>
        /// Lista de campos para el formulario
        /// </summary>
        public List<FormField> Fields { get; set; } = new List<FormField>();

        /// <summary>
        /// Texto del botón de guardar
        /// </summary>
        private string _saveButtonText = "Guardar";
        public string SaveButtonText
        {
            get => _saveButtonText;
            set
            {
                if (_saveButtonText != value)
                {
                    _saveButtonText = value;
                    OnPropertyChanged(nameof(SaveButtonText));
                }
            }
        }

        /// <summary>
        /// Texto del botón de cancelar
        /// </summary>
        private string _cancelButtonText = "Cancelar";
        public string CancelButtonText
        {
            get => _cancelButtonText;
            set
            {
                if (_cancelButtonText != value)
                {
                    _cancelButtonText = value;
                    OnPropertyChanged(nameof(CancelButtonText));
                }
            }
        }

        /// <summary>
        /// ButtonStyle para el botón guardar
        /// </summary>
        private ButtonStyle _saveButtonStyle = ButtonStyle.Primary;
        public ButtonStyle SaveButtonStyle
        {
            get => _saveButtonStyle;
            set
            {
                if (_saveButtonStyle != value)
                {
                    _saveButtonStyle = value;
                    OnPropertyChanged(nameof(SaveButtonStyle));
                }
            }
        }

        /// <summary>
        /// ButtonStyle para el botón cancelar
        /// </summary>
        private ButtonStyle _cancelButtonStyle = ButtonStyle.Light;
        public ButtonStyle CancelButtonStyle
        {
            get => _cancelButtonStyle;
            set
            {
                if (_cancelButtonStyle != value)
                {
                    _cancelButtonStyle = value;
                    OnPropertyChanged(nameof(CancelButtonStyle));
                }
            }
        }

        /// <summary>
        /// Orientación del formulario (horizontal o vertical)
        /// </summary>
        private bool _horizontal = false;
        public bool Horizontal
        {
            get => _horizontal;
            set
            {
                if (_horizontal != value)
                {
                    _horizontal = value;
                    OnPropertyChanged(nameof(Horizontal));
                }
            }
        }

        /// <summary>
        /// Espaciado entre campos
        /// </summary>
        private string _fieldSpacing = "mb-3";
        public string FieldSpacing
        {
            get => _fieldSpacing;
            set
            {
                if (_fieldSpacing != value)
                {
                    _fieldSpacing = value;
                    OnPropertyChanged(nameof(FieldSpacing));
                }
            }
        }

        /// <summary>
        /// Valores iniciales para los campos
        /// </summary>
        private Dictionary<string, object> _initialValues = new Dictionary<string, object>();
        public Dictionary<string, object> InitialValues
        {
            get => _initialValues;
            set
            {
                if (_initialValues != value)
                {
                    _initialValues = value;
                    OnPropertyChanged(nameof(InitialValues));
                }
            }
        }
    }
}