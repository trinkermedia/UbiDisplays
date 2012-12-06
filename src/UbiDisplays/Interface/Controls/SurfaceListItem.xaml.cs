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
    /// Interaction logic for SurfaceListItem.xaml
    /// </summary>
    public partial class SurfaceListItem : UserControl
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
                    //_pSurface.OnDebugModeChange
                }

                // Store value.
                _pSurface = value;

                // Bind events.
                if (_pSurface != null)
                {
                    _pSurface.OnSurfacePropertiesUpdated += Surface_OnPropertiesUpdated;
                    _pSurface.OnDeleted += Surface_OnDeleted;
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
        /// Create a new surface list item.
        /// </summary>
        public SurfaceListItem()
        {
            // Start it up.
            InitializeComponent();

            // Make it match the surface.
            MakeUIMatchSurface();
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
            }
            else
            {
                // Enable the control.
                this.IsEnabled = true;

                // Update the name.
                this._lblName.Text = _pSurface.Identifier;

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
                _imgToggleDebug.Source = new BitmapImage(new Uri("pack://application:,,,/UbiDisplays;component/Interface/Images/shading.png"));
            }
            else
            {
                _imgToggleDebug.Source = new BitmapImage(new Uri("pack://application:,,,/UbiDisplays;component/Interface/Images/shape_square.png"));
            }
        }

        /// <summary>
        /// Handle updates to the surface's spatial properties.
        /// </summary>
        /// <param name="obj"></param>
        private void Surface_OnPropertiesUpdated(Model.Surface obj)
        {
            this.Dispatcher.BeginInvoke((Action) delegate()
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
        /// Tell this surface to show debug mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleDebugView(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            _pSurface.ShowDebug = !_pSurface.ShowDebug;
            UpdateToggleDebug(_pSurface.ShowDebug);
        }

        /// <summary>
        /// Tell the authority to delete this surface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSurface(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            Model.Authority.DeleteSurface(_pSurface);
        }

        /// <summary>
        /// Handle double clicking on the label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Open the dialog box to rename/edit the surface (and an active display if there is one).
            new SurfaceEditor(){ Surface = _pSurface }.Show();
        }

        /// <summary>
        /// Rotate the surface by 90 degrees.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RotateSurface(object sender, RoutedEventArgs e)
        {
            if (_pSurface == null)
                return;
            _pSurface.RotateSurface(false);
        }
    }
}
