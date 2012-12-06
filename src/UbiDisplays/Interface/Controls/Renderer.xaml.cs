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

using System.Windows.Media.Media3D;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// Interaction logic for Renderer.xaml
    /// </summary>
    public partial class Renderer : UserControl
    {
        public class Display
        {
            /// <summary>
            /// A reference back to the renderer.
            /// </summary>
            public Renderer Renderer { get; private set;}

            /// <summary>
            /// A reference to the visual we want to draw.
            /// </summary>
            private UIElement _Visual = null;

            /// <summary>
            /// Get or set the UIElement which should be rendered on the visual.
            /// </summary>
            public UIElement Content { get { return _Visual; } set { this._Visual = value; Sync(); } }

            /// <summary>
            /// Get the homography transform which is being used to transform the UIElement space into the renderer space.
            /// </summary>
            public MatrixTransform3D HomographyTransform { get; private set; }

            /// <summary>
            /// Get or set the homography matrix which 
            /// </summary>
            public Matrix3D HomographyMatrix { get { return HomographyTransform.Matrix; } set { HomographyTransform.Matrix = value; } }

            /// <summary>
            /// Get a reference to the visual brush that renders the component.
            /// </summary>
            /// <remarks>This allows other aspects of the application to view the content of this display.</remarks>
            public VisualBrush VisualBrush { get; private set; }

            /// <summary>
            /// Get a reference the viewport control.  Useful for HitTesting.
            /// </summary>
            public Viewport2DVisual3D Viewport2DVisual3D { get; private set; }

            /// <summary>
            /// Get or set if this display is currently visible.
            /// </summary>
            public bool Visible
            {
                get;// { this.Viewport2DVisual3D.Material. }
                set;// { }
            }

            /// <summary>
            /// Get the key that identifies this display to the renderer.
            /// </summary>
            public object Key { get; private set; }

            /// <summary>
            /// Get any user data that this display holds.
            /// </summary>
            public object UserData { get; set; }

            /// <summary>
            /// Construct a new display object.
            /// </summary>
            /// <param name="Key">The key it is stored at.</param>
            /// <param name="pRenderer">The renderer which created it.</param>
            internal Display(object Key, Renderer pRenderer)
            {
                // Store the renderer.
                this.Renderer = pRenderer;
                this.Key = Key;

                // Identities.
                HomographyTransform = new MatrixTransform3D();

                // Create the model visual.
                Viewport2DVisual3D = new Viewport2DVisual3D();

                // Geom (create a texturemapped rectangle).
                var pMeshGeometry3D = new MeshGeometry3D();
                pMeshGeometry3D.Positions.Add(new Point3D(0, 0, 0));
                pMeshGeometry3D.Positions.Add(new Point3D(0, 1, 0));
                pMeshGeometry3D.Positions.Add(new Point3D(1, 0, 0));
                pMeshGeometry3D.Positions.Add(new Point3D(1, 1, 0));

                pMeshGeometry3D.TextureCoordinates.Add(new Point(0, 0));
                pMeshGeometry3D.TextureCoordinates.Add(new Point(1, 0));
                pMeshGeometry3D.TextureCoordinates.Add(new Point(0, 1));
                pMeshGeometry3D.TextureCoordinates.Add(new Point(1, 1));
                //pMeshGeometry3D.TextureCoordinates.Add(new Point(0, 1));
                //pMeshGeometry3D.TextureCoordinates.Add(new Point(0, 0));
                //pMeshGeometry3D.TextureCoordinates.Add(new Point(1, 1));
                //pMeshGeometry3D.TextureCoordinates.Add(new Point(1, 0));

                pMeshGeometry3D.TriangleIndices.Add(0);
                pMeshGeometry3D.TriangleIndices.Add(2);
                pMeshGeometry3D.TriangleIndices.Add(1);
                pMeshGeometry3D.TriangleIndices.Add(2);
                pMeshGeometry3D.TriangleIndices.Add(3);
                pMeshGeometry3D.TriangleIndices.Add(1);
                Viewport2DVisual3D.Geometry = pMeshGeometry3D;

                // Create a visual brush which we apply to our material.
                VisualBrush = new VisualBrush();
                VisualBrush.Visual = Content;

                // Create a material which wraps this brush.
                var pDiffuse = new DiffuseMaterial();
                pDiffuse.Brush = Brushes.White;
                Viewport2DVisual3D.SetIsVisualHostMaterial(pDiffuse, true);
                Viewport2DVisual3D.Material = new DiffuseMaterial(VisualBrush);
                Viewport2DVisual3D.Visual = Content;

                // Transform.
                Viewport2DVisual3D.Transform = HomographyTransform;
            }

            /// <summary>
            /// Set the new UIElement to be drawn by the visual brush.
            /// </summary>
            private void Sync()
            {
                Viewport2DVisual3D.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Update the new visual in the visual brush.
                    if (VisualBrush != null)
                        VisualBrush.Visual = _Visual;

                    // Disconnect existing..
                    //if (Viewport2DVisual3D.Visual != null)
                    //    Viewport2DVisual3D.Visual = _Visual;

                    // If we have no content, bail.
                    if (Content == null)
                    {
                        Viewport2DVisual3D.Visual = null;
                        return;
                    }

                    // Check the child is not attached.
                    var pElement = VisualTreeHelper.GetParent(Content) as Viewport2DVisual3D;
                    if (pElement != null)
                        pElement.Visual = null;

                    // Apply the new one.
                    Viewport2DVisual3D.Visual = Content;
                }));

            }

            /// <summary>
            /// Get the bottom left point (0, 0, 0) in output space (i.e. rendered to the window).
            /// </summary>
            public Point3D BottomLeftTransformed { get { return TransformPoint(new Point3D(0, 0, 0)); } }

            /// <summary>
            /// Get the top left point (0, 1, 0) in output space (i.e. rendered to the window).
            /// </summary>
            public Point3D TopLeftTransformed { get { return TransformPoint(new Point3D(0, 1, 0)); } }

            /// <summary>
            /// Get the bottom right point (1, 0, 0) in output space (i.e. rendered to the window).
            /// </summary>
            public Point3D BottomRightTransformed { get { return TransformPoint(new Point3D(1, 0, 0)); } }

            /// <summary>
            /// Get the top right point (1, 1, 0) in output space (i.e. rendered to the window).
            /// </summary>
            public Point3D TopRightTransformed { get { return TransformPoint(new Point3D(1, 1, 0)); } }

            /// <summary>
            /// Get or set each transformed corner value.
            /// </summary>
            /// <remarks>
            /// 0 = Bottom Left
            /// 1 = Top Left
            /// 2 = Bottom Right
            /// 3 = Top Right
            /// </remarks>
            /// <param name="index"></param>
            /// <returns></returns>
            public Point3D this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return BottomLeftTransformed;//return new Point(BottomLeftTransformed.X, BottomLeftTransformed.X);
                        case 1:
                            return TopLeftTransformed;//return new Point(TopLeftTransformed.X, TopLeftTransformed.Y);
                        case 2:
                            return BottomRightTransformed;//return new Point(BottomRightTransformed.X, BottomRightTransformed.Y);
                        case 3:
                            return TopRightTransformed;//return new Point(TopRightTransformed.X, TopRightTransformed.Y);
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
                set
                {
                    if (index < 0 || index > 3)
                        throw new IndexOutOfRangeException();

                    Point3D[] tPoints = new Point3D[]
                    {
                        this.TransformPoint(new Point3D(0, 0, 0)),
                        this.TransformPoint(new Point3D(0, 1, 0)),
                        this.TransformPoint(new Point3D(1, 0, 0)),
                        this.TransformPoint(new Point3D(1, 1, 0))
                    };
                    value.Z = 0;
                    tPoints[index] = value;// new Point3D(value.X, value.Y, 0);
                    this.SetHomographyFromPoints(tPoints);

                    // BottomLeft, TopLeft, BottomRight, TopRight
                }
            }

            /// <summary>
            /// Find the closest transformed corner given a hit radius (only takes X and Y into account).
            /// </summary>
            /// <param name="tPoint">A 3D point in render viewport space.</param>
            /// <param name="fHitRadius">How close we need to be to the corner (pixels) before we call it a hit.</param>
            /// <returns>The index of the closest corner. Returns -1 if none are found to be close enough.</returns>
            public int FindClosestCornerIndex(Point3D tPoint, double fHitRadius)
            {
                // Look through each corner to see how close it is to this point.
                int iClosest = -1;
                double fSmallest = double.PositiveInfinity;

                for (int i = 0, n = 4; i < n; ++i)
                {
                    Point3D tCorner = this[i];
                    double fDistance = Vector.Subtract(new Vector(tPoint.X, tPoint.Y), new Vector(tCorner.X, tCorner.Y)).Length;
                    if (fDistance < fSmallest && fDistance < fHitRadius)
                    {
                        fSmallest = fDistance;
                        iClosest = i;
                    }
                }

                // Return the index.
                return iClosest;
            }

            /// <summary>
            /// Transform a 3D input point (0-1 space) into the output point space (i.e. the verts in WPF).
            /// </summary>
            /// <param name="tIn">The input point.</param>
            /// <returns>The transformed output point.</returns>
            public Point3D TransformPoint(Point3D tIn)
            {
                return HomographyMatrix.Transform(tIn);
            }

            /// <summary>
            /// Transform a 2D input point (0-1 space) into the output point space (i.e. the verts in WPF).
            /// Z is implied as 0.
            /// </summary>
            /// <param name="tIn">The input point.</param>
            /// <returns>The transformed output point.</returns>
            public Point TransformPoint(Point tIn)
            {
                var p = HomographyMatrix.Transform(new Point3D(tIn.X, tIn.Y, 0));
                return new Point(p.X, p.Y);
            }

            /// <summary>
            /// Compute a homography matrix (i.e. a non-affine matrix) from a set of output points.
            /// </summary>
            /// <remarks>
            /// The returned transform maps the points (0, 0, 0), (0, 1, 0), (1, 0, 0), and (1, 1, 0) to these points.
            /// </remarks>
            /// <param name="BottomLeft">The bottom left point.</param>
            /// <param name="TopLeft">The top left point.</param>
            /// <param name="BottomRight">The bottom right point.</param>
            /// <param name="TopRight">The top right point.</param>
            public void SetHomographyFromPoints(Point3D BottomLeft, Point3D TopLeft, Point3D BottomRight, Point3D TopRight)
            {
                this.HomographyMatrix = Display.HomographyFromPoints(BottomLeft, TopLeft, BottomRight, TopRight);
            }

            /// <summary>
            /// Compute a homography matrix (i.e. a non-affine matrix) from a set of output points.
            /// </summary>
            /// <remarks>
            /// The returned transform maps the points (0, 0, 0), (0, 1, 0), (1, 0, 0), and (1, 1, 0) to these points.
            /// </remarks>
            /// <param name="tPoints">The array of points in the format: (bottomleft, topleft, bottomright, topright).</param>
            public void SetHomographyFromPoints(Point3D[] tPoints)
            {
                this.HomographyMatrix = Display.HomographyFromPoints(tPoints);
            }

            /// <summary>
            /// Compute a homography matrix (i.e. a non-affine matrix) from a set of output points.
            /// </summary>
            /// <remarks>
            /// The returned transform maps the points (0, 0, 0), (0, 1, 0), (1, 0, 0), and (1, 1, 0) to these points.
            /// </remarks>
            /// <param name="tPoints">The array of SCREEN SPACE points in the format: (bottomleft, topleft, bottomright, topright).</param>
            public void SetHomographyFrom2DPoints(Point[] tPoints)
            {
                // Transform the 2D points by the camera.
                Point3D[] t3DPoints = new Point3D[tPoints.Length];
                for (int i = 0; i < tPoints.Length; ++i)
                    t3DPoints[i] = this.Renderer.TransformNormalToViewport(tPoints[i]);

                // Update the homography.
                this.HomographyMatrix = Display.HomographyFromPoints(t3DPoints);
            }

            /// <summary>
            /// Compute a homography matrix (i.e. a non-affine matrix) from a set of points.
            /// </summary>
            /// <remarks>
            /// The returned transform maps the points (0, 0, 0), (0, 1, 0), (1, 0, 0), and (1, 1, 0) to these points.
            /// </remarks>
            /// <param name="BottomLeft">The bottom left point.</param>
            /// <param name="TopLeft">The top left point.</param>
            /// <param name="BottomRight">The bottom right point.</param>
            /// <param name="TopRight">The top right point.</param>
            /// <returns>A homography matrix which transforms 0-1 space onto these points.</returns>
            public static Matrix3D HomographyFromPoints(Point3D BottomLeft, Point3D TopLeft, Point3D BottomRight, Point3D TopRight)
            {
                return HomographyFromPoints(new Point3D[] { BottomLeft, TopLeft, BottomRight, TopRight });
            }

            /// <summary>
            /// Compute a homography matrix (i.e. a non-affine matrix) from a set of points.
            /// </summary>
            /// <remarks>
            /// The returned transform maps the points (0, 0, 0), (0, 1, 0), (1, 0, 0), and (1, 1, 0) to these points.
            /// </remarks>
            /// <param name="tPoints">The array of points in the format: (bottomleft, topleft, bottomright, topright).</param>
            /// <returns>A homography matrix which transforms 0-1 space onto these points.</returns>
            public static Matrix3D HomographyFromPoints(Point3D[] tPoints)
            {
                // Create the affine transform (preserves straight lines).
                // (0, 0) --> (x0, y0)
                // (0, 1) --> (x1, y1)
                // (1, 0) --> (x2, y2)
                // (1, 1) --> (x2 + x1 + x0, y2 + y1 + y0)
                Matrix3D A = new Matrix3D();
                A.M11 = tPoints[2].X - tPoints[0].X;
                A.M12 = tPoints[2].Y - tPoints[0].Y;
                A.M21 = tPoints[1].X - tPoints[0].X;
                A.M22 = tPoints[1].Y - tPoints[0].Y;
                A.OffsetX = tPoints[0].X;
                A.OffsetY = tPoints[0].Y;

                // Calculate the point (a, b) that gets mapped by the affine transform to (x3, y3).
                double den = A.M11 * A.M22 - A.M12 * A.M21;
                double a = (A.M22 * tPoints[3].X - A.M21 * tPoints[3].Y + A.M21 * A.OffsetY - A.M22 * A.OffsetX) / den;
                double b = (A.M11 * tPoints[3].Y - A.M12 * tPoints[3].X + A.M12 * A.OffsetX - A.M11 * A.OffsetY) / den;

                // Non-affine transform
                // (0, 0) --> (0, 0)
                // (0, 1) --> (0, 1)
                // (1, 0) --> (1, 0)
                // (1, 1) --> (a, b)
                Matrix3D B = new Matrix3D();
                B.M11 = a / (a + b - 1);
                B.M22 = b / (a + b - 1);
                B.M14 = B.M11 - 1;
                B.M24 = B.M22 - 1;

                // Multiply the skew by the affine to get the homography.
                return B * A;
            }

            /*
            /// <summary>
            /// Compute a homography matrix (i.e. a non-affine matrix) from a set of points.
            /// </summary>
            /// <remarks>
            /// The returned transform maps the points (0, 0, 0), (0, 1, 0), (1, 0, 0), and (1, 1, 0) to these points.
            /// </remarks>
            /// <param name="tPoints">The array of points in the format: (bottomleft, topleft, bottomright, topright).</param>
            /// <returns>A homography matrix which transforms 0-1 space onto these points.</returns>
            public static Matrix3D HomographyFrom2DPoints(Point[] tPoints)
            {
                // Create the affine transform (preserves straight lines).
                // (0, 0) --> (x0, y0)
                // (0, 1) --> (x1, y1)
                // (1, 0) --> (x2, y2)
                // (1, 1) --> (x2 + x1 + x0, y2 + y1 + y0)
                Matrix3D A = new Matrix3D();
                A.M11 = tPoints[2].X - tPoints[0].X;
                A.M12 = tPoints[2].Y - tPoints[0].Y;
                A.M21 = tPoints[1].X - tPoints[0].X;
                A.M22 = tPoints[1].Y - tPoints[0].Y;
                A.OffsetX = tPoints[0].X;
                A.OffsetY = tPoints[0].Y;
                
                // Calculate the point (a, b) that gets mapped by the affine transform to (x3, y3).
                double den = A.M11 * A.M22 - A.M12 * A.M21;
                double a = (A.M22 * tPoints[3].X - A.M21 * tPoints[3].Y + A.M21 * A.OffsetY - A.M22 * A.OffsetX) / den;
                double b = (A.M11 * tPoints[3].Y - A.M12 * tPoints[3].X + A.M12 * A.OffsetX - A.M11 * A.OffsetY) / den;

                // Non-affine transform
                // (0, 0) --> (0, 0)
                // (0, 1) --> (0, 1)
                // (1, 0) --> (1, 0)
                // (1, 1) --> (a, b)
                Matrix3D B = new Matrix3D();
                B.M11 = a / (a + b - 1);
                B.M22 = b / (a + b - 1);
                B.M14 = B.M11 - 1;
                B.M24 = B.M22 - 1;

                // Multiply the skew by the affine to get the homography.
                return B * A;
            }
            */
        }

        
        /// <summary>
        /// Set the location of the test point based on normalised screen coordinates.
        /// </summary>
        public Point NormalisedTestPoint
        {
            set
            {
                var p = TransformNormalToViewport(value);
                this._TestPointerMatrix.OffsetX = p.X;
                this._TestPointerMatrix.OffsetY = p.Y;
            }
        }

        /// <summary>
        /// A reference to all the homography displays that are rendered.
        /// </summary>
        private Dictionary<object, Display> dDisplays = new Dictionary<object, Display>();

        /// <summary>
        /// Return a reference to the 3D viewport.
        /// </summary>
        public Viewport3D Viewport3D { get { return this._Viewport; } }

        /// <summary>
        /// Create a new renderer.
        /// </summary>
        public Renderer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Find a display under a point between 0 and 1.
        /// </summary>
        /// <param name="tNormalPoint">The point scaled between 0 and 1.</param>
        /// <param name="pDisplay">The display underneath the point.</param>
        /// <returns>True if a display was found, false if not.</returns>
        public bool FindDisplay(Point tNormalPoint, out Display pDisplay)
        {
            // Defaults.
            pDisplay = null;

            // Scale the normal point into viewport space.
            Point tScaled = new Point(tNormalPoint.X * Viewport3D.Width, tNormalPoint.Y * Viewport3D.Height);

            // Get the visual under the click.
            var result = VisualTreeHelper.HitTest(Viewport3D, tScaled);
            if (result == null || result.VisualHit == null)
                return false;

            // Get the display that contains that visual.
            pDisplay = GetDisplayByContent(result.VisualHit as UIElement);
            return true;
        }

        /// <summary>
        /// Find a display and the closest corner under a point between 0 and 1.
        /// </summary>
        /// <param name="tNormalPoint">The point scaled between 0 and 1.</param>
        /// <param name="fCloseness"></param>
        /// <param name="pDisplay">The display underneath the point.</param>
        /// <param name="iCornerIndex">The index of the corner.  -1 means one was not found.</param>
        /// <returns>True if a display was found, false if not.</returns>
        public bool FindClosestDisplayAndCornerIndex(Point tNormalPoint, float fCloseness, out Display pDisplay, out int iCornerIndex)
        {
            // Defaults.
            pDisplay = null;
            iCornerIndex = -1;

            // Scale the normal point into viewport space.
            Point tScaled = new Point(tNormalPoint.X * Viewport3D.ActualWidth, tNormalPoint.Y * Viewport3D.ActualHeight);

            // Get the visual under the click.
            var result = VisualTreeHelper.HitTest(Viewport3D, tScaled);
            if (result == null || result.VisualHit == null)
                return false;

            // Get the display that contains that visual.
            pDisplay = GetDisplayByContent(result.VisualHit as UIElement);

            // Find the index of the closest corner in that display.
            if (pDisplay != null)
                iCornerIndex = pDisplay.FindClosestCornerIndex(TransformToViewport(tScaled), fCloseness);

            // True if we found a corner, false if not.
            return iCornerIndex != -1;
        }

        /// <summary>
        /// Remove all the displays currently being rendered.
        /// </summary>
        public void ClearDisplays()
        {
            while (dDisplays.Count > 0)
            {
                this.RemoveDisplay(dDisplays.Values.First().Key);
            }
        }

        /// <summary>
        /// Create a new display that renderes the contents of a given UI element.
        /// </summary>
        /// <param name="kKey">The key that we can use to address this visual.</param>
        /// <param name="pVisual">The visual element we want to be rendererd.</param>
        /// <returns>The display that contains our data (such as the homography and the visual brush reference).</returns>
        public Display AddDisplay(object kKey, UIElement pVisual)
        {
            // Create a display.
            var d = new Display(kKey, this);
            d.Content = pVisual;

            // Add the display to the renderer. 
            //  n.b We use insert(0,) here rather than add because objects at the front are drawn first.
            //_Viewport.Children.Add(d.Viewport2DVisual3D);
            _Viewport.Children.Insert(0, d.Viewport2DVisual3D);

            // Add it to the dictionary.
            this.dDisplays[kKey] = d;

            // Return the display.
            return d;
        }

        /// <summary>
        /// Remove a display by key.
        /// </summary>
        /// <param name="kKey">The key that addresses the display we want to remove.</param>
        /// <returns>True if removed successfully, false if not.</returns>
        public bool RemoveDisplay(object kKey)
        {
            // Get the display.
            Display d = null;
            if (dDisplays.TryGetValue(kKey, out d))
            {
                // Remove us from the table and the renderer.
                dDisplays.Remove(kKey);
                _Viewport.Children.Remove(d.Viewport2DVisual3D);//Visual3D);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a display by content element.
        /// </summary>
        /// <param name="pVisual">A reference to the content element being displayed.</param>
        /// <returns>True if removed successfully, false if not.</returns>
        public bool RemoveDisplay(UIElement pVisual)
        {
            // For each display, find a matching visual.
            Display dTarget = null;
            foreach (var d in dDisplays.Values)
            {
                if (d.Content == pVisual)
                {
                    dTarget = d;
                    break;
                }
            }

            // If we have no display.
            if (dTarget == null)
                return false;

            // If we have a display, remove it by its key.
            return RemoveDisplay(dTarget.Key);
        }

        /// <summary>
        /// Remove a display by object reference.
        /// </summary>
        /// <param name="pDisplay">The display object reference we want to remove.</param>
        /// <returns>True if removed successfully, false if not.</returns>
        public bool RemoveDisplay(Display pDisplay)
        {
            if (pDisplay.Renderer == this)
                return RemoveDisplay(pDisplay.Key);
            throw new Exception("Cannot remove display because it is not attached to this renderer.");
        }

        /// <summary>
        /// Return the display stored at the key.
        /// </summary>
        /// <param name="kKey">The key which identifies this display.</param>
        /// <returns>Null if not found or the display reference if found.</returns>
        public Display GetDisplay(object kKey)
        {
            if (kKey == null) // hack
                return null;

            Display d = null;
            if (dDisplays.TryGetValue(kKey, out d))
                return d;
            return null;
        }

        /// <summary>
        /// Get a display by looking up it's visual element.
        /// </summary>
        /// <param name="pVisual">The display visual element that is being rendered.</param>
        /// <returns>Null if not found or the display reference if found.</returns>
        public Display GetDisplayByContent(UIElement pVisual)
        {
            // For each display, find a matching visual.
            foreach (var d in dDisplays.Values)
            {
                if (d.Content == pVisual)
                    return d;
            }
            return null;
        }

        /// <summary>
        /// Get a display by looking up it's visual 3d model.  This is useful for handling hit test results.
        /// </summary>
        /// <param name="pVisual3D">The 3D model.</param>
        /// <returns>Null if not found or the display reference if found.</returns>
        public Display GetDisplayByViewport(Viewport2DVisual3D pVisual3D)
        {
            // For each display, find a matching visual.
            foreach (var d in dDisplays.Values)
            {
                if (d.Viewport2DVisual3D == pVisual3D)
                    return d;
            }
            return null;
        }

        /// <summary>
        /// Try to get a display by key.
        /// </summary>
        /// <param name="kKey">The key that identifies the display.</param>
        /// <param name="pDisplay">The variable to write the display into.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGetDisplay(object kKey, out Display pDisplay)
        {
            return dDisplays.TryGetValue(kKey, out pDisplay);
        }

        /// <summary>
        /// Check to see if a display is referenced by a given key.
        /// </summary>
        /// <param name="kKey">The key that addresses the display.</param>
        /// <returns>True if contained, false if not.</returns>
        public bool ContainsDisplay(object kKey)
        {
            return dDisplays.ContainsKey(kKey);
        }

        /// <summary>
        /// Generate a list of all the displays contained in this renderer.
        /// </summary>
        /// <returns>A new list object which is a copy of all the display references.</returns>
        public List<Display> GenerateDisplayList()
        {
            return new List<Display>(dDisplays.Values);
        }


        #region Transform Helpers
        /// <summary>
        /// Converts a 2D point in device-independent coordinates relative to Viewport3D to 3D space.
        /// </summary>
        /// <remarks>This only works using an OrthographicCamera with LookDirection of (0, 0, -1) and UpDirection of (0, 1, 0).</remarks>
        /// <param name="pt">The point in 2D screen coordinates.</param>
        /// <returns>The point in 3D viewport coordinates (assuming a Z value of 0).</returns>
        public Point3D TransformNormalToViewport(Point pt)
        {
            // Scale the normal point into viewport space.
            Point tScaled = new Point(pt.X * Viewport3D.ActualWidth, pt.Y * Viewport3D.ActualHeight);

            double w = this.Viewport3D.ActualWidth;
            double h = this.Viewport3D.ActualHeight;

            OrthographicCamera cam = CheckRestrictions();
            double scale = cam.Width / w;
            double x = scale * (tScaled.X - w / 2) + cam.Position.X;
            double y = scale * (h / 2 - tScaled.Y) + cam.Position.Y;

            return new Point3D(x, y, 0);
        }

        /// <summary>
        /// Converts a 2D point in device-independent coordinates relative to Viewport3D to 3D space.
        /// </summary>
        /// <remarks>This only works using an OrthographicCamera with LookDirection of (0, 0, -1) and UpDirection of (0, 1, 0).</remarks>
        /// <param name="pt">The point in 2D screen coordinates.</param>
        /// <returns>The point in 3D viewport coordinates (assuming a Z value of 0).</returns>
        public Point3D TransformToViewport(Point pt)
        {
            double w = this.Viewport3D.ActualWidth;
            double h = this.Viewport3D.ActualHeight;

            OrthographicCamera cam = CheckRestrictions();
            double scale = cam.Width / w;
            double x = scale * (pt.X - w / 2) + cam.Position.X;
            double y = scale * (h / 2 - pt.Y) + cam.Position.Y;

            return new Point3D(x, y, 0);
        }

        /// <summary>
        /// Converts a 3D point to 2D in device-independent coordinates relative to Viewport3D.
        /// </summary>
        /// <remarks>This only works using an OrthographicCamera with LookDirection of (0, 0, -1) and UpDirection of (0, 1, 0).</remarks>
        /// <param name="point">The point in the viewport to project onto the screen.</param>
        /// <returns>The point in 2D screen coordinates.</returns>
        public Point TransformToScreen(Point3D point)
        {
            double w = this.Viewport3D.ActualWidth;
            double h = this.Viewport3D.ActualHeight;

            OrthographicCamera cam = CheckRestrictions();
            double scale = w / cam.Width;
            double x = w / 2 + scale * (point.X - cam.Position.X);
            double y = h / 2 - scale * (point.Y - cam.Position.Y);
            return new Point(x, y);
        }

        /// <summary>
        /// Helper to check that our camera meets the desired requirements for the transform functions.
        /// </summary>
        /// <returns></returns>
        private OrthographicCamera CheckRestrictions()
        {
            OrthographicCamera cam = this.Viewport3D.Camera as OrthographicCamera;

            if (cam == null)
                throw new ArgumentException("Camera must be OrthographicCamera");

            if (cam.LookDirection != new Vector3D(0, 0, -1))
                throw new ArgumentException("Camera LookDirection must be (0, 0, -1)");

            if (cam.UpDirection != new Vector3D(0, 1, 0))
                throw new ArgumentException("Camera UpDirection must be (0, 1, 0)");

            return cam;
        }
        #endregion

    }
}
