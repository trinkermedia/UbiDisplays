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
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// Interaction logic for LogViewer.xaml
    /// </summary>
    public partial class LogViewer : UserControl
    {
        /// <summary>
        /// Is this log viewer activly displaying events.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// The internal class which we use to render a log message.
        /// </summary>
        public class LogMessage
        {
            /// <summary>
            /// Create a new list log message from an actual log message.
            /// </summary>
            /// <param name="pLogMessage"></param>
            public LogMessage(UbiDisplays.Model.Log.LogMessage pLogMessage)
            {
                Message = pLogMessage.Message;
                Source = pLogMessage.Source;
                Type = pLogMessage.LogType;
                Time = pLogMessage.Time;
            }
            /// <summary>
            /// The log message content.  A string.
            /// </summary>
            public String Message { get; set; }

            /// <summary>
            /// The log message source.
            /// </summary>
            public String Source { get; set; }

            /// <summary>
            /// The log message time.
            /// </summary>
            public DateTime Time { get; set; }

            /// <summary>
            /// The type of log message this is.
            /// </summary>
            public UbiDisplays.Model.Log.Type Type { get; set; }
        }


        /// <summary>
        /// Create a log viewer control.
        /// </summary>
        public LogViewer()
        {
            // Load the XAML.
            InitializeComponent();
            Active = true;

            // Listen to log events.
            UbiDisplays.Model.Log.OnNewLogMessage += Log_OnNewLogMessage;
        }

        /// <summary>
        /// Handle the arrival of a new log message.
        /// </summary>
        /// <param name="pLogMessage"></param>
        void Log_OnNewLogMessage(Model.Log.LogMessage pLogMessage)
        {
            // Write into the debug log.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // If we are not active, drop the message.
                if (!Active)
                    return;

                // Otherwise add it to our list.
                _lstDebug.Items.Insert(0, new LogMessage(pLogMessage));

                // Remove log items older than 100.
                const int REMOVE_COUNT = 100;
                if (_lstDebug.Items.Count == REMOVE_COUNT)
                    _lstDebug.Items.RemoveAt(REMOVE_COUNT - 1);
            }));
        }


        /// <summary>
        /// Clear all the items currently in the debug log.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DebugLogClear_Click(object sender, RoutedEventArgs e)
        {
            _lstDebug.Items.Clear();
        }

        /// <summary>
        /// Called when the text changes and we want to apply a new filter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DebugLogFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the text.
            String sFilterText = _txtDebugLogFilter.Text;

            // No filter.
            if (sFilterText == null || sFilterText.Length == 0)
            {
                _lstDebug.Items.Filter = null;
                return;
            }

            // Create a filter.
            _lstDebug.Items.Filter = delegate(object obj)
            {
                // Reference the log message.
                LogMessage pMessage = (LogMessage)obj;

                // Check the message.
                string str = pMessage.Message;
                if (String.IsNullOrEmpty(str)) return false;
                int index = str.IndexOf(sFilterText, 0);
                if (index > -1)
                    return true;

                // Check the source.
                str = pMessage.Source;
                if (String.IsNullOrEmpty(str)) return false;
                index = str.IndexOf(sFilterText, 0);

                // Return true if we were found.
                return (index > -1);
            };
        }

        /// <summary>
        /// Handle the user pressing the reload displays button.  This will clear the log and reload all displays.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReloadDisplays(object sender, RoutedEventArgs e)
        {
            // Clear the log.
            _lstDebug.Items.Clear();

            // Get a list of all the surfaces.
            var lSurfaces = Model.Authority.Surfaces;
            var sType = "";

            // If we are holding down shift, do a hard reset.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                foreach (var pSurface in lSurfaces)
                {
                    if (pSurface.ActiveDisplay == null)
                        continue;
                    pSurface.ActiveDisplay.Reload(true);
                }
                sType = "hard reload";
            }

            // Otherwise just do a soft reset.
            else
            {
                foreach (var pSurface in lSurfaces)
                {
                    if (pSurface.ActiveDisplay == null)
                        continue;
                    pSurface.ActiveDisplay.Reload(false, true);
                }
                sType = "quick refresh";
            }

            // Write to the log.
            Model.Log.Write("User forced " + sType + " of all active displays.", "Application", Model.Log.Type.AppInfo);
        }

        /// <summary>
        /// Create a new log window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DetachLog(object sender, RoutedEventArgs e)
        {
            var pLogWindow = new UbiDisplays.Interface.Controls.LogWindow();
            pLogWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            pLogWindow.Show();
        }

        /// <summary>
        /// Update the active state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Active = false;
        }


        /// <summary>
        /// Update the active state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Active = true;
        }
    }
}
