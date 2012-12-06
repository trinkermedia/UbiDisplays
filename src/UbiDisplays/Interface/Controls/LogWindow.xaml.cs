using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        /// <summary>
        /// The default button brush.
        /// </summary>
        private Brush pButtonBrush = null;

        /// <summary>
        /// Create a new log window.
        /// </summary>
        public LogWindow()
        {
            InitializeComponent();

            // Store the button brush so we can reset it later.
            pButtonBrush = _PinButton.BorderBrush;
        }

        /// <summary>
        /// Pin this window to the top.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PinWindow(object sender, RoutedEventArgs e)
        {
            if (this.Topmost)
            {
                _PinButton.BorderBrush = pButtonBrush;
                this.Topmost = false;
            }
            else
            {
                _PinButton.BorderBrush = Brushes.Orange;
                this.Topmost = true;
            }
        }
    }
}
