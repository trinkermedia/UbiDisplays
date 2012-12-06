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

using UbiDisplays.Interface.Controls;

namespace UbiDisplays
{
    /// <summary>
    /// Interaction logic for ImportExport.xaml
    /// </summary>
    public partial class ImportExport : Window
    {
        /// <summary>
        /// Get or set if we are in export mode.
        /// </summary>
        public bool ExportMode
        {
            get
            {
                return !ImportMode;
            }
            set
            {
                ImportMode = !value;
            }
        }
        /// <summary>
        /// Get or set us to be in import mode.
        /// </summary>
        public bool ImportMode
        {
            get
            {
                return bImportMode;
            }
            set
            {
                bImportMode = value;
                if (bImportMode)
                {
                    // Set all the words to say "import".
                    this.Title = "Import Settings";
                    this._ExplanPanel.Title = "Choose what to import";
                    
                    this._ExplanPanel.Explanation = "Please select the elements you want to import from the file.";
                }
                else
                {
                    // Set all the words to say "export".
                    this.Title = "Export Settings";
                    this._ExplanPanel.Title = "Choose what to export";
                    this._ExplanPanel.Explanation = "Please select the elements you want to export to the file.";
                }
            }
        }
        /// <summary>
        /// Are we in import or export mode.
        /// </summary>
        private bool bImportMode = true;

        /// <summary>
        /// Create a new import export window.
        /// </summary>
        public ImportExport()
        {
            // Set up the XAML.
            InitializeComponent();
        }

        /// <summary>
        /// Handle the window load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Make the top bar show wpf glass.
            Glass.Extend(this, 0, 40);  // (int)topBar.ActualHeight
            
        }

        /// <summary>
        /// Open an export dialog box.
        /// </summary>
        /// <param name="bImportCalibration">Does the user want to export calibration data.</param>
        /// <param name="bImportSurfaces">Does the user want to export surface data.</param>
        /// <param name="bImportDisplays">Does the user want to export display data.</param>
        /// <returns>True if they clicked ok, false if they clicked cancel or closed the window.</returns>
        public static bool OpenExportDialog(out bool bExportCalibration, out bool bExportSurfaces, out bool bExportDisplays)
        {
            // Create a new import/export for this.
            var pDialog = new ImportExport();
            pDialog.ExportMode = true;

            // Show the import export as a dialog.
            var bResult = pDialog.ShowDialog() == true ? true : false;

            // Read out the import values.
            bExportCalibration = (pDialog._chkCalibration.IsChecked == true) ? true : false;
            bExportSurfaces = (pDialog._chkSurfaces.IsChecked == true) ? true : false;
            bExportDisplays = (pDialog._chkDisplays.IsChecked == true) ? true : false;

            // Returnthe result.
            return bResult;
        }

        /// <summary>
        /// Open an import dialog box.
        /// </summary>
        /// <param name="bImportCalibration">Does the user want to import calibration data.</param>
        /// <param name="bImportSurfaces">Does the user want to import surface data.</param>
        /// <param name="bImportDisplays">Does the user want to import display data.</param>
        /// <returns>True if they clicked ok, false if they clicked cancel or closed the window.</returns>
        public static bool OpenImportDialog(out bool bImportCalibration, out bool bImportSurfaces, out bool bImportDisplays)
        {
            // Create a new import/export for this.
            var pDialog = new ImportExport();
            pDialog.ImportMode = true;

            // Show the import export as a dialog.
            var bResult = pDialog.ShowDialog() == true ? true : false;

            // Read out the import values.
            bImportCalibration = (pDialog._chkCalibration.IsChecked == true) ? true : false;
            bImportSurfaces = (pDialog._chkSurfaces.IsChecked == true) ? true : false;
            bImportDisplays = (pDialog._chkDisplays.IsChecked == true) ? true : false;

            // Returnthe result.
            return bResult;
        }

        /// <summary>
        /// When the OK button is clicked, change the dialog result.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_OkClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// When the Cancel button is clicked, change the dialog result.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }


    }
}
