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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// Interaction logic for Projector.xaml
    /// </summary>
    public partial class Projector : Window
    {
        Renderer.Display dDragging = null;
        int indexDragging;

        public Projector()
        {
            // Load the XAML.
            InitializeComponent();

            // Bind events to the movement of the calibration point.

        }

        public Renderer RenderSurface { get { return this.Renderer; } }

        // Add the ability to display calibration dots on this screen. :)

        private Point tStart;
        private Point tOrigin;

        private void HandleCalibrationMouseDown(object sender, MouseButtonEventArgs e)
        {
            uctlFollow.CaptureMouse();
            tStart = e.GetPosition(null);
            tOrigin = new Point(Canvas.GetLeft(uctlFollow), Canvas.GetTop(uctlFollow));
        }

        private void HandleCalibrationMouseMove(object sender, MouseEventArgs e)
        {
            if (uctlFollow.IsMouseCaptured)
            {
                Vector v = tStart - e.GetPosition(null);
                Canvas.SetLeft(uctlFollow, tOrigin.X - v.X);
                Canvas.SetTop(uctlFollow, tOrigin.Y - v.Y);
            }
        }

        private void HandleCalibrationMouseUp(object sender, MouseButtonEventArgs e)
        {
            uctlFollow.ReleaseMouseCapture();
        }

        /// <summary>
        /// Show or hide the calibration point on the screen.
        /// </summary>
        public bool ShowCalibrationPoint
        {
            get
            {
                return (uctlFollow.Visibility == System.Windows.Visibility.Visible);
            }
            set
            {
                uctlFollow.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
        }

        /// <summary>
        /// Get or set the normalised coordinates for the calibration point on the screen.
        /// </summary>
        public Point NormalisedCalibrationPoint
        {
            get
            {
                var pt = this.CalibrationPoint;
                pt.X /= this.ActualWidth;
                pt.Y /= this.ActualHeight;
                return pt;
            }
            set
            {
                this.CalibrationPoint = new Point(value.X * this.ActualWidth, value.Y * this.ActualHeight);
            }
        }

        /// <summary>
        /// Get or set the point that the calibration pointer is placed at on screen.
        /// </summary>
        public Point CalibrationPoint
        {
            get
            {
                // Return the location.
                return new Point(Canvas.GetLeft(uctlFollow), Canvas.GetTop(uctlFollow));
            }

            set
            {
                // Check we will not create problems.
                if (uctlFollow.IsMouseCaptured)
                    throw new Exception("Cannot set calibration pointer position while it is being moved by hand.");

                // Set the location.
                Canvas.SetLeft(uctlFollow, value.X);
                Canvas.SetTop(uctlFollow, value.Y);
            }
        }



        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the mouse in screen space.
            Point pt = e.GetPosition(Renderer.Viewport3D);

            // Obtain the Visual3D objects under the mouse pointer.
            var result = VisualTreeHelper.HitTest(Renderer.Viewport3D, pt);
            var resultMesh = result as RayMeshGeometry3DHitTestResult;
            if (resultMesh == null)
                return;

            // Look up the display from all those rendered.
            dDragging = Renderer.GetDisplay(resultMesh.VisualHit as ModelVisual3D);
            indexDragging = -1;

            // Find the index of the closest corner in that display.
            if (dDragging != null)
                indexDragging = dDragging.FindClosestCornerIndex(Renderer.TransformToViewport(pt), 0.3);

            // If we have a corner, update the homography.
            if (indexDragging != -1)
                dDragging[indexDragging] = Renderer.TransformToViewport(pt);

            // Capture the mouse.
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // If the mouse is captured and we are already dragging.
            if (IsMouseCaptured && (dDragging != null && indexDragging != -1))
            {
                // Get the mouse point and update the homography.
                Point ptMouse = e.GetPosition(Renderer.Viewport3D);
                dDragging[indexDragging] = Renderer.TransformToViewport(ptMouse);
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // If the mouse is captured, release the capture and update the settings.
            if (IsMouseCaptured && (dDragging != null && indexDragging != -1))
            {
                dDragging = null;
                indexDragging = -1;
                ReleaseMouseCapture();
            }
        }
    }
}
