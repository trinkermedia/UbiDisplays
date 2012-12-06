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
    /// Interaction logic for SurfaceEditor.xaml
    /// </summary>
    public partial class SurfaceEditor : Window
    {
        /// <summary>
        /// Set or get the surface which this item reflects.
        /// </summary>
        public UbiDisplays.Model.Surface Surface
        {
            get
            {
                return _pSurface;
            }
            set
            {
                // Unbind events.
                if (_pSurface != null)
                {
                    _pSurface.OnSurfacePropertiesUpdated -= Surface_OnPropertiesUpdated;
                    _pSurface.OnDeleted -= Surface_OnDeleted;
                    _pSurface.OnDisplayChanged -= Surface_OnDisplayChanged;
                    //_pSurface.OnDebugModeChange
                }

                // Store value.
                _pSurface = value;

                // Bind events.
                if (_pSurface != null)
                {
                    _pSurface.OnSurfacePropertiesUpdated += Surface_OnPropertiesUpdated;
                    _pSurface.OnDeleted += Surface_OnDeleted;
                    _pSurface.OnDisplayChanged += Surface_OnDisplayChanged;
                    //_pSurface.OnDebugModeChange
                }

                // Make the UI match the surface.
                MakeUIMatchSurface();
            }
        }

        /// <summary>
        /// Inner storage for the surface.
        /// </summary>
        private UbiDisplays.Model.Surface _pSurface = null;

        /// <summary>
        /// Load a new surface editor.
        /// </summary>
        public SurfaceEditor()
        {
            // Load the XAML.
            InitializeComponent();

            // Make it match the surface.
            MakeUIMatchSurface();
        }

        /// <summary>
        /// Handle updates to the surface's spatial properties.
        /// </summary>
        /// <param name="obj"></param>
        private void Surface_OnPropertiesUpdated(Model.Surface obj)
        {
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                MakeUIMatchSurface();
            });
        }

        /// <summary>
        /// Handle this surface being deleted.
        /// </summary>
        /// <param name="obj"></param>
        private void Surface_OnDeleted(Model.IResource obj)
        {
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                this.Surface = null;
                MakeUIMatchSurface();
            });
        }

        /// <summary>
        /// Handle the display changing on this surface.
        /// </summary>
        /// <param name="obj"></param>
        private void Surface_OnDisplayChanged(Model.Surface obj)
        {
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                MakeUIMatchSurface();
            });
        }

        /// <summary>
        /// Make the UI match the current surface.
        /// </summary>
        private void MakeUIMatchSurface()
        {
            // Handle us not having a surface.
            if (_pSurface == null || _pSurface.IsDeleted())
            {
                // Disable the control.
                this.IsEnabled = false;
                _txtLoadInstruction.Text = "";
                _txtSurfaceName.Text = "";
                _lblWidth.Content = "";
                _lblHeight.Content = "";
                _lblAngle.Content = "";
                _txtDisplayResolution.Text = "";
                _chkAutomaticInject.IsChecked = false;
            }
            else
            {
                // Enable the control.
                this.IsEnabled = true;

                // Update the name.
                _txtLoadInstruction.Text = (_pSurface.ActiveDisplay != null)?_pSurface.ActiveDisplay.LoadInstruction : "";
                _txtSurfaceName.Text = _pSurface.Identifier;
                _txtSurfaceName.Foreground = Brushes.Black;
                _lblWidth.Content = _pSurface.Width;
                _lblHeight.Content = _pSurface.Height;
                _lblAngle.Content = _pSurface.Angle;
                _chkAutomaticInject.IsChecked = _pSurface.AttemptMultiTouchInject;
                _txtDisplayResolution.Text = (_pSurface.ActiveDisplay != null) ? ((int)_pSurface.ActiveDisplay.RenderResolution.X) + "x" + ((int)_pSurface.ActiveDisplay.RenderResolution.Y) : "";
                _txtDisplayResolution.Foreground = Brushes.Black;

                // Update the toggle button.
                UpdateToggleDebug(_pSurface.ShowDebug);
            }
        }
        /// <summary>
        /// Set the button on the debug toggle to have the correct image.
        /// </summary>
        /// <param name="bDebug"></param>
        private void UpdateToggleDebug(bool bDebug)
        {
            if (bDebug)
            {
                _ImgDebugMode.Source = new BitmapImage(new Uri("pack://application:,,,/UbiDisplays;component/Interface/Images/shading.png"));
            }
            else
            {
                _ImgDebugMode.Source = new BitmapImage(new Uri("pack://application:,,,/UbiDisplays;component/Interface/Images/shape_square.png"));
            }
        }

        /// <summary>
        /// Handle the user pressing the load instruction button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_LoadInstruction(object sender, RoutedEventArgs e)
        {
            String sURL = "";
            try
            {
                var pURL = new Uri(_txtLoadInstruction.Text);
                sURL = pURL.ToString();
            }
            catch (Exception e1)
            {
                Model.Log.Write("Error parsing URL '"+sURL+"'. " + e1.Message, "Application", Model.Log.Type.AppWarning);
                return;
            }

            // Remove the current display.
            if (_pSurface.ActiveDisplay != null)
                Model.Authority.DeleteDisplay(_pSurface.ActiveDisplay);

            // Add a new one, if we have one.
            if (sURL != null && sURL != "")
                Model.Authority.ShowDisplay(new Model.Display(sURL), _pSurface);
        }

        /// <summary>
        /// Handle the user pressing the reload button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_Reload(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            if (_pSurface.ActiveDisplay != null)
            {
                _pSurface.ActiveDisplay.Reload(Keyboard.IsKeyDown(Key.LeftShift) ? true : false);
            }
        }


        /// <summary>
        /// Handle the user pressing the clockwise button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_Clockwise(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            _pSurface.RotateSurface(true);
        }

        /// <summary>
        /// Handle the user pressing the anti-clockwise button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_Anticlockwise(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            _pSurface.RotateSurface(false);
        }

        /// <summary>
        /// Handle the user pressing the debug mode button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_DebugMode(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            _pSurface.ShowDebug = !_pSurface.ShowDebug;
            UpdateToggleDebug(_pSurface.ShowDebug);
        }

        /// <summary>
        /// Handle the user pressing the delete button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_Delete(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            Model.Authority.DeleteSurface(_pSurface);
        }

        /// <summary>
        /// Handle keystrokes in the name field.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_SurfaceNameKeyUp(object sender, KeyEventArgs e)
        {
            if (_txtSurfaceName.Text != "" && _pSurface != null)
            {
                // If the name is the same as the current one, skip.
                if (_txtSurfaceName.Text == _pSurface.Identifier)
                {
                    _txtSurfaceName.Foreground = Brushes.Black;
                    return;
                }

                // If there is a problem changing the name, make the text red.
                if (!Model.Authority.RenameSurface(_pSurface.Identifier, _txtSurfaceName.Text))
                {
                    _txtSurfaceName.Foreground = Brushes.Red;
                }
            }
        }

        /// <summary>
        /// If the mouse moves over the text box, treat as if we were a keyboard event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_SurfaceNameMouseUp(object sender, MouseButtonEventArgs e)
        {
            Handle_SurfaceNameKeyUp(null, null);
        }

        /// <summary>
        /// Someone dropped a file on the load instruction bit. Let's load it!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _txtLoadInstruction_Drop(object sender, DragEventArgs e)
        {
            // File name.
            String sFile = null;

            // Get the file we are dragging on.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Select the file to load.
                string[] tFilePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                if (tFilePaths.Length != 1)
                    return;
                sFile = tFilePaths[0];
            }

            // Do we have a file to open.
            if (sFile == null)
                return;

            // Open it.
            if (_pSurface != null)
            {
                if (_pSurface.ActiveDisplay != null)
                    Model.Authority.DeleteDisplay(_pSurface.ActiveDisplay);
                Model.Authority.ShowDisplay(new Model.Display(sFile), _pSurface);
                MakeUIMatchSurface();
            }
        }


        /// <summary>
        /// Forward mouse up events in the display res box to key up events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextDisplayResolution_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextDisplayResolution_KeyUp(null, null);
        }

        /// <summary>
        /// Handle the user typing in a new display resolution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextDisplayResolution_KeyUp(object sender, KeyEventArgs e)
        {
            if (_txtDisplayResolution.Text != "" && _pSurface != null && _pSurface.ActiveDisplay != null)
            {
                // Try to get the resolution.
                var sParts = _txtDisplayResolution.Text.Split('x');

                // Bad number of parts.
                if (sParts.Length != 2)
                {
                    _txtDisplayResolution.Foreground = Brushes.Red;
                    return;
                }

                // Check for numbers.
                int iWidth = 0;
                int iHeight = 0;
                if (!(int.TryParse(sParts[0], out iWidth) && int.TryParse(sParts[1], out iHeight)))
                {
                    _txtDisplayResolution.Foreground = Brushes.Red;
                    return;
                }

                // Check they are large enough.
                iWidth = Math.Abs(iWidth);
                iHeight = Math.Abs(iHeight);
                if (iWidth < 100 || iHeight < 100)
                {
                    _txtDisplayResolution.Foreground = Brushes.Red;
                    return;
                }

                // If it is the same as the current one, skip.
                var pCurrent = _pSurface.ActiveDisplay.RenderResolution;
                if (iWidth == (int)pCurrent.X && iHeight == (int)pCurrent.Y)
                {
                    _txtDisplayResolution.Foreground = Brushes.Black;
                    return;
                }

                // Otherwise update!
                _pSurface.ActiveDisplay.RenderResolution = new Point(iWidth, iHeight);
                _txtDisplayResolution.Foreground = Brushes.Black;
                MakeUIMatchSurface();
            }
        }

        /// <summary>
        /// Change if we are supposed to attempt to automatically inject multi-touch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleAutomaticInject_Click(object sender, RoutedEventArgs e)
        {
            if (_pSurface != null)
                _pSurface.AttemptMultiTouchInject = _chkAutomaticInject.IsChecked == true ? true : false;
        }

        /// <summary>
        /// Remove the current display from this surface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_RemoveDisplay(object sender, RoutedEventArgs e)
        {
            if (_pSurface != null && _pSurface.ActiveDisplay != null)
                Model.Authority.DeleteDisplay(_pSurface.ActiveDisplay);
            MakeUIMatchSurface();
        }
    }
}
