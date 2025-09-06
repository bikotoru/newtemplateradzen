using Radzen;
using System;

namespace Frontend.Componentes.CustomRadzen.Dialog
{
    /// <summary>
    /// Options for the ConfirmWait dialog with countdown functionality
    /// </summary>
    public class ConfirmWaitOptions : ConfirmOptions
    {
        private int _waitSeconds = 5;

        /// <summary>
        /// Gets or sets the number of seconds to wait before enabling the OK button.
        /// Default is 5 seconds.
        /// </summary>
        public int WaitSeconds
        {
            get => _waitSeconds;
            set
            {
                if (_waitSeconds != value)
                {
                    _waitSeconds = value;
                    OnPropertyChanged(nameof(WaitSeconds));
                }
            }
        }

        /// <summary>
        /// Gets or sets the text format to show during countdown.
        /// {0} will be replaced with the remaining seconds.
        /// Default is "{0}s".
        /// </summary>
        private string _countdownFormat = "{0}s";
        public string CountdownFormat
        {
            get => _countdownFormat;
            set
            {
                if (_countdownFormat != value)
                {
                    _countdownFormat = value;
                    OnPropertyChanged(nameof(CountdownFormat));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show a busy indicator on the button during countdown.
        /// Default is false.
        /// </summary>
        private bool _showBusyIndicator = false;
        public bool ShowBusyIndicator
        {
            get => _showBusyIndicator;
            set
            {
                if (_showBusyIndicator != value)
                {
                    _showBusyIndicator = value;
                    OnPropertyChanged(nameof(ShowBusyIndicator));
                }
            }
        }
    }
}