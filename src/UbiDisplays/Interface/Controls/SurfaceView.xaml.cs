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
using System.Windows.Markup;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// Interaction logic for SurfaceView.xaml
    /// </summary>
    [ContentProperty("Children")]
    public partial class SurfaceView : UserControl
    {
        /// <summary>
        /// Show or hide the debug image.
        /// </summary>
        public bool ShowDebug
        {
            get
            {
                return _DebugImage.Visibility == Visibility.Visible;
            }
            set
            {
                _DebugImage.Visibility = value ? Visibility.Visible : Visibility.Hidden;
            }
        }
        private bool bDebug = false;

        /// <summary>
        /// Get or set the opacity of the content control.
        /// </summary>
        public double ContentOpacity
        {
            get
            {
                return _InnerContent.Opacity;
            }
            set
            {
                _InnerContent.Opacity = value;
            }
        }

        /// <summary>
        /// The property which our children are added too.
        /// </summary>
        public UIElementCollection Children { get { return _InnerContent.Children; } }

        /// <summary>
        /// Create a new surface view.
        /// </summary>
        public SurfaceView()
        {
            // Load the XAML.
            InitializeComponent();

            // Start by hiding the debug image.
            ShowDebug = false;

            
        }
    }
}
