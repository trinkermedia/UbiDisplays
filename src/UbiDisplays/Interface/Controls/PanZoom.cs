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
using System.Windows.Media.Animation;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// The PanZoom control allows a user to pan and zoom a canvas inside it.
    /// </summary>
    [ContentProperty("Children")]
    public class PanZoom : UserControl
    {
        /// <summary>
        /// The matrix translation when dragging started.
        /// </summary>
        private Point origin;

        /// <summary>
        /// The mouse point relative to the control top when dragging started.
        /// </summary>
        private Point start;

        /// <summary>
        /// Is this control currently panning.
        /// </summary>
        public bool IsPanning { get; private set; }

        /// <summary>
        /// The canvas which is dragged around.
        /// </summary>
        private Canvas _Inner = null;

        /// <summary>
        /// Return a reference to the inner canvas that is dragged arround.
        /// </summary>
        public Canvas Inner { get { return _Inner; } }

        /// <summary>
        /// Get or set the scale factor on mouse wheel move.
        /// </summary>
        public double ScaleSpeed { get; set; }


        /// <summary>
        /// Raised when the mouse is press down on the inner canvas.
        /// </summary>
        public event MouseButtonEventHandler InnerMouseLeftButtonDown
        {
            add { _Inner.MouseLeftButtonDown += value; }
            remove { _Inner.MouseLeftButtonDown -= value; }
        }
        /// <summary>
        /// Raised when the mouse is let up on the inner canvas.
        /// </summary>
        public event MouseButtonEventHandler InnerMouseLeftButtonUp
        {
            add { _Inner.MouseLeftButtonUp += value; }
            remove { _Inner.MouseLeftButtonUp -= value; }
        }

        /// <summary>
        /// Raised when the mouse is let up on the inner canvas.
        /// </summary>
        public event MouseEventHandler InnerMouseMove
        {
            add { _Inner.MouseMove += value; }
            remove { _Inner.MouseMove -= value; }
        }

        public UIElementCollection Children { get { return _Inner.Children; } }
        

        /// <summary>
        /// Create a new pan-zoom control.
        /// </summary>
        public PanZoom()
        {
            // Create the draggable canvas.
            _Inner = new Canvas();
            _Inner.RenderTransformOrigin = new Point(0.5, 0.5);
            this.Content = _Inner;
            ScaleSpeed = 0.2;

            // Make it not so screwy when we resize the pan-zoom control.
            this.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
            this.VerticalContentAlignment = System.Windows.VerticalAlignment.Top;

            // Checkerboard background.
            ResourceDictionary resource = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/UbiDisplays;component/Interface/Controls/BrushTable.xaml")
                //Source = new Uri("/UbiDisplays.Interface.Controls;component/BrushTable.xaml", UriKind.RelativeOrAbsolute)
            };
            this.Background = resource["CheckerBrush"] as Brush;
            this.ClipToBounds = true;

            // Create the initial transform.
            ResetTransform();

            // Bind events.
            _Inner.MouseWheel += image_MouseWheel;
            _Inner.MouseRightButtonDown += image_MouseRightButtonDown;
            _Inner.MouseRightButtonUp += image_MouseRightButtonUp;
            _Inner.MouseMove += image_MouseMove;

            this.SizeChanged += new SizeChangedEventHandler(PanZoom_SizeChanged);
        }

        /// <summary>
        /// Called when the control is resized.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanZoom_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // If we are scaling the size of the visual brush.
            var el = GetVisualFromBrush();
            if (el != null && ScaleContentToFrame)
            {
                // Compute the desired size.
                double fScale = this.ActualWidth / (el.ActualWidth + 0.0000001);
                InnerWidth  = fScale * el.ActualWidth;
                InnerHeight = fScale * el.ActualHeight;
            }
        }

        /// <summary>
        /// Handle the mouse button being let up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void image_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_Inner.IsMouseCaptured && IsPanning)
            {
                _Inner.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
                IsPanning = false;
            }
        }

        /// <summary>
        /// Handle the mouse moving.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            // If the mouse is captured, handle as a pan motion.
            if (_Inner.IsMouseCaptured && IsPanning)
            {
                // Get the transform group, but if it does not exist, bail.
                var mGroup = InnerTransformGroup;
                if (mGroup == null)
                    return;

                // Get the translate transform and update the dragging data.
                var mTranslate = (TranslateTransform)InnerTransformGroup.Children.First(tr => tr is TranslateTransform);
                if (mTranslate != null)
                {
                    Vector v = start - e.GetPosition(this);
                    mTranslate.X = origin.X - v.X;
                    mTranslate.Y = origin.Y - v.Y;
                }
            }
        }

        /// <summary>
        /// Handle the left mouse button being clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the transform group, but if it does not exist, bail.
            var mGroup = InnerTransformGroup;
            if (mGroup == null)
                return;

            // Get the translate transform and store data.
            var mTranslate = (TranslateTransform)InnerTransformGroup.Children.First(tr => tr is TranslateTransform);
            if (mTranslate != null)
            {
                // Capture the mouse.
                _Inner.CaptureMouse();

                // Store data.
                start = e.GetPosition(this);
                origin = new Point(mTranslate.X, mTranslate.Y);
                IsPanning = true;

                // Set the cursor.
                this.Cursor = Cursors.SizeAll;
            }
        }

        /// <summary>
        /// Handle the mouse wheel moving.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Get the transform group, but if it does not exist, bail.
            var mGroup = InnerTransformGroup;
            if (mGroup == null)
                return;

            // Get the translate and scale transforms.
            var mTranslate = (TranslateTransform)InnerTransformGroup.Children.First(tr => tr is TranslateTransform);
            var mScale = (ScaleTransform)InnerTransformGroup.Children.First(tr => tr is ScaleTransform);

            // Compute the scale.
            var fZoomX = mScale.ScaleX + (e.Delta > 0.0 ? ScaleSpeed : -ScaleSpeed);
            var fZoomY = mScale.ScaleY + (e.Delta > 0.0 ? ScaleSpeed : -ScaleSpeed);

            // Compute the translation offset for this new zoom.
            var vPhysicalPoint = e.GetPosition(this);
            var vRelativePoint = InnerTransform.Inverse.Transform(vPhysicalPoint);
            mTranslate.X = -1 * (vRelativePoint.X * fZoomX - vPhysicalPoint.X);
            mTranslate.Y = -1 * (vRelativePoint.Y * fZoomY - vPhysicalPoint.Y);

            // Set the scale factor.
            mScale.ScaleX = fZoomX;
            mScale.ScaleY = fZoomY;
        }

        /// <summary>
        /// Reset the transforms by creating a new scale and translate transform.
        /// </summary>
        public void ResetTransform()
        {
            TransformGroup mGroup = new TransformGroup();
            mGroup.Children.Add(new ScaleTransform());
            mGroup.Children.Add(new TranslateTransform());
            InnerTransform = mGroup;
        }

        /// <summary>
        /// Fits the content to the viewport size.
        /// </summary>
        public void FitContent()
        {
            // Get the size of all the children.
            var pRect = VisualTreeHelper.GetDescendantBounds(Inner);
            if (pRect.Width == 0 || pRect.Height == 0)
                return;

            // Update the scale transform.
            var mScale = (ScaleTransform)InnerTransformGroup.Children.First(tr => tr is ScaleTransform);

            // If we want to take the smallest (width/height)
            if (false)
            {
                mScale.ScaleX = Math.Min(this.ActualWidth / pRect.Width, this.ActualHeight / pRect.Height);
                mScale.ScaleY = mScale.ScaleX;
            }
            else
            {
                mScale.ScaleX = this.ActualWidth / pRect.Width;
                mScale.ScaleY = mScale.ScaleX;
            }

            // Update the translate so we are in the middle.
            var mTranslate = (TranslateTransform)InnerTransformGroup.Children.First(tr => tr is TranslateTransform);
            mTranslate.X = 0;
            mTranslate.Y = 0;
        }

        /// <summary>
        /// Reset the transforms by returning the scale to 1,1 and the translation to 0,0,0.
        /// </summary>
        /// <param name="fTime">The animation time to take.</param>
        public void AnimatedResetTransform(double fTime)
        {
            this.ResetTransform();

            /*
            var mTranslate = (TranslateTransform)InnerTransformGroup.Children.First(tr => tr is TranslateTransform);
            var mScale = (ScaleTransform)InnerTransformGroup.Children.First(tr => tr is ScaleTransform);

            mTranslate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(mTranslate.X, 0.0, TimeSpan.FromMilliseconds(fTime), FillBehavior.Stop));
            mTranslate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(mTranslate.Y, 0.0, TimeSpan.FromMilliseconds(fTime), FillBehavior.Stop));

            mScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(mScale.ScaleX, 1.0, TimeSpan.FromMilliseconds(fTime), FillBehavior.Stop));
            mScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(mScale.ScaleY, 1.0, TimeSpan.FromMilliseconds(fTime), FillBehavior.Stop));
            */
        }

        /// <summary>
        /// Get or set the transform that pans and zooms the image.
        /// </summary>
        public Transform InnerTransform
        {
            get { return _Inner.RenderTransform; }
            set { _Inner.RenderTransform = value; }
        }

        /// <summary>
        /// Get or set the transform that pans and zooms the image.
        /// </summary>
        private TransformGroup InnerTransformGroup
        {
            get { return _Inner.RenderTransform as TransformGroup; }
        }

        /// <summary>
        /// Get or set the width of the image.
        /// </summary>
        public double InnerWidth
        {
            get { return _Inner.Width; }
            set { _Inner.Width = value; }
        }

        /// <summary>
        /// Get or set the height of the image.
        /// </summary>
        public double InnerHeight
        {
            get { return _Inner.Height; }
            set { _Inner.Height = value; }
        }

        /// <summary>
        /// Get or set the brush which paints the background.
        /// </summary>
        public Brush InnerBackground
        {
            get { return _Inner.Background; }
            set
            {
                _Inner.Background = value;
                var el = GetVisualFromBrush();
                if (el != null && ScaleInnerSizeToContent)
                {
                    InnerWidth = el.ActualWidth;
                    InnerHeight = el.ActualHeight;
                    PanZoom_SizeChanged(null, null);
                    return;
                }

                // Otherwise, adopt the default size.
                //InnerWidth = this.ActualWidth;
                //InnerHeight = this.ActualHeight;

                // Update the size.
                PanZoom_SizeChanged(null, null);
            }
        }

        /// <summary>
        /// Internal store for the match size property.
        /// </summary>
        private bool bMatchSize = false;

        /// <summary>
        /// Do we want the container within to size to the window or the container size.
        /// </summary>
        public bool ScaleInnerSizeToContent
        {
            get
            {
                return bMatchSize;
            }
            set
            {
                bMatchSize = value;
                InnerBackground = InnerBackground;
            }
        }

        /// <summary>
        /// Do we want to scale the size of the content to fit the frame (width wise).
        /// </summary>
        public bool ScaleContentToFrame
        {
            get;
            set;
        }

        /// <summary>
        /// If the background is a visual brush, get the visual being rendered.
        /// </summary>
        /// <returns>A reference to the visual OR null.</returns>
        private FrameworkElement GetVisualFromBrush()
        {
            var vb = this.InnerBackground as VisualBrush;
            if (vb != null)
                return vb.Visual as FrameworkElement;
            return null;
        }
    }
}
