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
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// Interaction logic for Monitor.xaml
    /// </summary>
    [ContentProperty("Children")]
    public partial class Monitor : UserControl
    {
        /// <summary>
        /// The default blue colour.  First part of the gradiant.
        /// </summary>
        public static readonly Color DefaultBlue = Color.FromRgb(50, 79, 136);

        /// <summary>
        /// The default blue colour.  Second part of the gradiant.
        /// </summary>
        public static readonly Color DefaultLightBlue = Color.FromRgb(127, 149, 197);

        /// <summary>
        /// The normal (unselected) colour for the monitor background.  First part of the gradiant.
        /// </summary>
        public Color Colour { get { return (Color)GetValue(ColourProperty); } set { SetValue(ColourProperty, value); } }
        public static readonly DependencyProperty ColourProperty = DependencyProperty.Register("Colour", typeof(Color), typeof(Monitor));

        /// <summary>
        /// The normal (unselected) colour for the monitor background.  Second part of the gradiant.
        /// </summary>
        public Color LightColour { get { return (Color)GetValue(LightColourProperty); } set { SetValue(LightColourProperty, value); } }
        public static readonly DependencyProperty LightColourProperty = DependencyProperty.Register("LightColour", typeof(Color), typeof(Monitor));

        /// <summary>
        /// The selected colour for the monitor background.  First part of the gradiant.
        /// </summary>
        public Color SelectedColour { get { return (Color)GetValue(SelectedColourProperty); } set { SetValue(SelectedColourProperty, value); } }
        public static readonly DependencyProperty SelectedColourProperty = DependencyProperty.Register("SelectedColour", typeof(Color), typeof(Monitor));

        /// <summary>
        /// The selected colour for the monitor background.  Second part of the gradiant.
        /// </summary>
        public Color SelectedLightColour { get { return (Color)GetValue(SelectedLightColourProperty); } set { SetValue(SelectedLightColourProperty, value); } }
        public static readonly DependencyProperty SelectedLightColourProperty = DependencyProperty.Register("SelectedLightColour", typeof(Color), typeof(Monitor));

        /// <summary>
        /// Handle the click event (a proxy for MouseUp).
        /// </summary>
        public event MouseButtonEventHandler Click
        {
            add { this.MouseUp += value; }
            remove { this.MouseUp -= value; }
        }

        /// <summary>
        /// Get or set the monitor we reflect.  This will not layout the control, it is just for storage.
        /// </summary>
        public Utilities.MonitorDetection.DisplayInfo MonitorData { get; set; }

        /// <summary>
        /// Create a monitor with no content.
        /// </summary>
        public Monitor()
        {
            InitializeComponent();
            LightColour = DefaultLightBlue;
            Colour = DefaultBlue;

            SelectedColour = Colors.Black;
            SelectedLightColour = Color.FromRgb(51, 51, 51); 
        }

        /// <summary>
        /// Create a monitor with a label and size.
        /// </summary>
        /// <param name="sText">The text to display on the monitor.</param>
        /// <param name="xRes"></param>
        /// <param name="yRes"></param>
        public Monitor(String sText, int xRes, int yRes)
            : this()
        {
            FillOutInformation(sText, xRes, yRes);
        }

        /// <summary>
        /// The property which our children are added too.
        /// </summary>
        public UIElementCollection Children { get { return _Container.Children; } }

        /// <summary>
        /// If the first child is a label, get or set the text on it.
        /// </summary>
        public String Text
        {
            get
            {
                if (Children.Count == 1)
                {
                    var pLabel = Children[0] as Label;
                    if (pLabel == null)
                        return null;
                    return pLabel.Content as String;
                }
                return null;
            }
            set
            {
                if (Children.Count == 1)
                {
                    var pLabel = Children[0] as Label;
                    if (pLabel == null)
                        return;
                    pLabel.Content = value;
                }
                return;
            }
        }

        /// <summary>
        /// Fill out monitor information.  This will delete any children currently on it.
        /// </summary>
        /// <param name="sText"></param>
        /// <param name="xRes"></param>
        /// <param name="yRes"></param>
        public void FillOutInformation(String sText, int xRes, int yRes)
        {
            // Set the label content.
            var pLabel = new Label();
            pLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pLabel.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            pLabel.Foreground = Brushes.White;
            pLabel.FontSize = 14;
            pLabel.Content = sText;
            Children.Add(pLabel);
            
            // Compute the desired size.
            // 163x102
            double x = ((double)xRes / 1280.0) * 100.0;
            double y = ((double)yRes / 1024.0) * 70.0;

            this.Width = x;
            this.Height = y;
        }

        /// <summary>
        /// Get or set if the display is black (i.e. the selected colour) or not.
        /// </summary>
        public bool Selected
        {
            get
            {
                return gs1.Color == SelectedColour;
            }
            set
            {
                if (value)
                {
                    gs1.Color = SelectedColour;
                    gs0.Color = SelectedLightColour;
                }
                else
                {
                    gs1.Color = Colour;
                    gs0.Color = LightColour;
                }
            }
        }
    }
}
