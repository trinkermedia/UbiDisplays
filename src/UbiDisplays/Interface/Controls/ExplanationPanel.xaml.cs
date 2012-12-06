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
    /// Interaction logic for ExplanationPanel.xaml
    /// The ExplanationPanel is a control which allows an image, title and subtitle to be combined into a visually pleasing standard control.
    /// </summary>
    [ContentProperty("ExplanationBlock")]
    public partial class ExplanationPanel : UserControl
    {
        public ExplanationPanel()
        {
            // Load the XAML.
            InitializeComponent();
        }

        /// <summary>
        /// Called when this panel is clicked.
        /// </summary>
        public event MouseEventHandler Click
        {
            // Add the input delegate to the collection.
            add
            {
                this.Click += value;
            }
            // Remove the input delegate from the collection.
            remove
            {
                this.Click -= value;
            }
        }

        /// <summary>
        /// Get or set the main title text (large) on this explanation panel.
        /// </summary>
        public String Title
        {
            get
            {
                return _Title.Text;
            }
            set
            {
                _Title.Text = value;
            }
        }

        /// <summary>
        /// Get or set the additional text (small) on this explanation panel.
        /// </summary>
        public String Explanation
        {
            get
            {
                return _Explanation.Text;
            }
            set
            {
                _Explanation.Text = value;
            }
        }

        /// <summary>
        /// Return the text block which contains the explanation.
        /// This is also our content variable.
        /// </summary>
        public InlineCollection ExplanationBlock
        {
            get
            {
                return _Explanation.Inlines;
            }
            set
            {
                _Explanation.Inlines.Clear();
                _Explanation.Inlines.AddRange(value);
            }
        }

        /// <summary>
        /// Get or set the source image to be displayed on the panel.
        /// </summary>
        public ImageSource Image
        {
            get
            {
                return _Image.Source;
            }
            set
            {
                _Image.Source = value;
            }
        }

        /// <summary>
        /// Get or set the image width.
        /// </summary>
        public double ImageSize
        {
            get
            {
                return _Image.Width;
            }
            set
            {
                _Image.Width = value;
            }
        }

        /// <summary>
        /// A flag which controls the state of the button (i.e. attached or not).
        /// </summary>
        private bool bButton = false;

        /// <summary>
        /// A reference to the button which may or may not be rendered.
        /// </summary>
        private Button pButton = new Button();

        /// <summary>
        /// Do we want this panel to behave like a button (i.e. hover colour change etc).
        /// </summary>
        public bool AsButton
        {
            get
            {
                return bButton;
            }
            set
            {
                // If yes.
                if (value)
                {
                    _Container.Children.Remove(_Content);
                    _Container.Children.Add(pButton);
                    pButton.Content = _Content;
                    pButton.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                    bButton = true;
                }

                // If no.
                else
                {
                    _Container.Children.Remove(pButton);
                    pButton.Content = null;
                    _Container.Children.Add(_Content);
                    bButton = false;
                }
            }
        }
    }
}
