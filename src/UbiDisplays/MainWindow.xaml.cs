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
using System.Windows.Threading;
using System.Windows.Interop;

using System.Xml;
using System.Xml.Linq;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Microsoft.Kinect;
using Awesomium.Core;
using Awesomium.Windows;

using UbiDisplays.Interface.Controls;
using UbiDisplays.Model;

namespace UbiDisplays
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties
        /// <summary>
        /// The URI which links to the folder where the internal image resources are kept.
        /// </summary>
        private const String IMAGE_PACK_STRING = "pack://application:,,,/UbiDisplays;component/Interface/Images/";

        /// <summary>
        /// The render window we use to draw displays on the projector with.
        /// </summary>
        public Projector OutputWindow { get; private set; }

        /// <summary>
        /// The writable bitmap where we stream the kinect video feed into.
        /// </summary>
        private WriteableBitmap pKinectVideoTarget = null;

        /// <summary>
        /// The writable bitmap where we stream the kinect debug data into.
        /// </summary>
        private WriteableBitmap pKinectVideoDebugTarget = null;

        /// <summary>
        /// A back buffer for the 'pKinectVideoDebugTarget' writable bitmap.
        /// </summary>
        private byte[] tKinectVideoDebugBackBuffer = null;

        // /// <summary>
        // /// The active (i.e. selected kinect sensor).
        // /// </summary>
        // public KinectSensor ActiveSensor { get { return KinectProcessor.Sensor; } }

        /// <summary>
        /// The class which is responsible for processing and managing the Kinect data.
        /// </summary>
        public KinectProcessing KinectProcessor { get; private set; }

        /// <summary>
        /// Create a JohnnyLee homography matrix for mapping between the kinect video and the projector feed.
        /// </summary>
        private Utilities.Warper pHomography = new Utilities.Warper();
        #endregion

        #region Polygon Overlays
        /// <summary>
        /// The polygon overlay used to show which processing regions are enabled / disabled.
        /// </summary>
        private Polygon pAllowedSurfacePoly = null;

        /// <summary>
        /// The polyon overlay used to show where we are drawing a surface.
        /// </summary>
        private Polygon pDrawSurfacePoly = null;

        /// <summary>
        /// The polygon overlay used to show the surface sensing overlay.
        /// </summary>
        private Polygon pSurfaceCalibrationAdjustPoly = null;
        #endregion



        #region Window Events and Constructor
        /// <summary>
        /// Create a new MainWindow.
        /// </summary>
        public MainWindow()
        {
            // Load plugins.
            Authority.LoadPlugins();

            // Start up the web-core with custom options.
            WebCore.Initialize(new WebConfig()
            {
                RemoteDebuggingPort = Properties.Settings.Default.RemoteDebuggingPort, // 9222,
            }, true);

            // Load the XAML.
            InitializeComponent();
        }

        /// <summary>
        /// Handle the window load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Create a new kinect processor.
            this.KinectProcessor = new KinectProcessing();

            // Let us know when we get new frames.
            KinectProcessor.OnFrameReady += KinectProcessor_OnFrameReady;
            KinectProcessor.OnKinectStreamingStarted += KinectProcessor_OnKinectStreamingStarted;

            // Bind log messages.
            Log.OnNewLogMessage += new Action<Log.LogMessage>(Log_OnNewLogMessage);

            // Listen for surface changes.
            Authority.OnSurfaceListChanged += new Action(Authority_OnSurfaceListChanged);

            // Make the top bar show wpf glass.
            Glass.Extend(this, 32, 32);  // (int)topBar.ActualHeight

            // Create an output monitor window.
            OutputWindow = new Projector();
            OutputWindow.Hide();
            OutputWindow.ShowCalibrationPoint = false;

            // Push globals to the surface (so it can interface with the app).
            UbiDisplays.Model.Surface.KinectProcessor = this.KinectProcessor;
            UbiDisplays.Model.Surface.ProjectionRenderer = OutputWindow.Renderer;

            // Detect monitors and kinects.
            DetectMonitors();
            DetectKinects();

            #region Polygon Creation
            // Create region selection UI polygon.  Used on the calibration stage.
            pAllowedSurfacePoly = new Polygon();
            pAllowedSurfacePoly.Stroke = Brushes.LightBlue;
            pAllowedSurfacePoly.Fill = new SolidColorBrush(Color.FromArgb(56, 56, 56, 160));
            pAllowedSurfacePoly.StrokeThickness = 1.5;
            pAllowedSurfacePoly.Visibility = System.Windows.Visibility.Hidden;
            _pzCalibration.Children.Add(pAllowedSurfacePoly);

            // Create the region drawing UI polygon.  Used when drawing displays out.
            pDrawSurfacePoly = new Polygon();
            pDrawSurfacePoly.Visibility = System.Windows.Visibility.Hidden;
            pDrawSurfacePoly.Stroke = Brushes.Purple;
            pDrawSurfacePoly.Fill = new SolidColorBrush(Color.FromArgb(160, 56, 56, 56));
            pDrawSurfacePoly.StrokeThickness = 0.5;
            _pzManage.Children.Add(pDrawSurfacePoly);

            // The surface calibration adjust poly.
            pSurfaceCalibrationAdjustPoly = new Polygon();
            pSurfaceCalibrationAdjustPoly.Visibility = System.Windows.Visibility.Hidden;
            pSurfaceCalibrationAdjustPoly.Stroke = Brushes.LightBlue;
            pSurfaceCalibrationAdjustPoly.Fill = new SolidColorBrush(Color.FromArgb(150, 40, 40, 40));
            pSurfaceCalibrationAdjustPoly.StrokeThickness = 0.5;
            _pzManage.Children.Add(pSurfaceCalibrationAdjustPoly);
            #endregion

            // Bind Kinect Window clicks to dragging.
            _vidManage.MouseLeftButtonDown += CornerDrag_InnerMouseLeftButtonDown;

            // Flip the video feeds.
            _pzCalibration.RenderTransform = new ScaleTransform(-1, 1);
            _pzCalibration.RenderTransformOrigin = new Point(0.5, 0.5);
            _pzManage.RenderTransform = new ScaleTransform(-1, 1, 0.5, 0.5);
            _pzManage.RenderTransformOrigin = new Point(0.5, 0.5);

            // Now the window is loaded, see if we need to load any files from the command line.
            if (App.CommandLineArgs != null)
            {
                // For each argument.
                foreach (var sArg in App.CommandLineArgs)
                {
                    // If it is a command to hide the window after load.
                    if (sArg == "--hidden" || sArg == "-h")
                    {
                        this.Hide();
                    }

                    // It is none of our know types, try and load as a file.
                    else
                    {
                        #region Attempted Load
                        try
                        {
                            this.Load(true, true, true, sArg);
                            Log.Write("Loaded from " + sArg, "Application", Log.Type.AppInfo);
                        }
                        catch (ArgumentNullException excp)
                        {
                            Log.Write("Unable to load. Is all the required XML present?", "Application", Log.Type.AppWarning);
                            sConfigurationFile = "";
                            bSettingsChosen = false;
                        }
                        catch (Exception excp)
                        {
                            Log.Write("Unable to load. " + excp.Message, "Application", Log.Type.AppWarning);
                            MessageBox.Show("Unable to load. " + excp.Message, "Ubi Displays", MessageBoxButton.OK, MessageBoxImage.Warning);
                            sConfigurationFile = "";
                            bSettingsChosen = false;
                        }
                        #endregion
                    }
                }
            }

            // Say we are ready.
            Log.Write("Ready", "Application", Log.Type.AppInfo);
        }

        /// <summary>
        /// Signals that the kinect sensor has streamed its first frame.
        /// </summary>
        /// <param name="obj"></param>
        private void KinectProcessor_OnKinectStreamingStarted(KinectProcessing obj)
        {
            Dispatcher.BeginInvoke((Action)delegate()
            {
                _pzCalibration.FitContent();
                _pzManage.FitContent();
            });
        }

        /// <summary>
        /// Update the UI to reflect the latest log message.
        /// </summary>
        /// <param name="obj"></param>
        private void Log_OnNewLogMessage(Log.LogMessage obj)
        {
            // Do this on the dispatcher.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this._TopLogText.Text = obj.Message;
                this._TopLogText.ToolTip = obj.Source;
            }));
        }

        /// <summary>
        /// Handle the window close event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Close the projection window.
            if (OutputWindow != null)
            {
                OutputWindow.Close();
                OutputWindow = null;
            }

            try
            {
                for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                    App.Current.Windows[intCounter].Close();
            }
            catch (Exception exep)
            {
                // Do nothing...
            }

            // Close the active sensor.
            DeactivateKinect();

            // Force the app to quite.
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Handle window resize events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_WindowResize(object sender, SizeChangedEventArgs e)
        {
            _pzCalibration.FitContent();
            _pzManage.FitContent();
        }

        /// <summary>
        /// Handle the user clicking on the log text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TopLogText_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Show the debug tab.
            tabDebugging.IsSelected = true;
        }
        #endregion

        #region Kinect Processing + Thread Sync
        /// <summary>
        /// This is called to signal that the kinect has recieved a new frame.
        /// </summary>
        /// <remarks>It is called from the Kinect processing thread, so we need to dispatch this function to the GUI thread.</remarks>
        /// <param name="pKinect">The calling kinect processor.</param>
        private void KinectProcessor_OnFrameReady(KinectProcessing pKinect)
        {
            // If we have a valid image.
            if (pKinectVideoTarget == null)
                return;

            // Dispatch the following code to the UI thread.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // If we are showing the projector selection.
                if (tabProjector.IsSelected)
                    return;

                // If we are showing the calibration video.
                if (tabSettings.IsSelected || tabDisplayControl.IsSelected)
                {
                    // Copy all the pixels from the current buffer into the video stream target.
                    pKinect.ImageToWriteableBitmap(pKinectVideoTarget);
                }

                // Update the FPS.
                _FPSLabel.Content = KinectProcessor.FPS + " fps";
            }));


            // Signal the point cloud processor thread.
            Parallel.Invoke(() =>
            {
                // For each spatial query, begin a new frame.
                var tQueries = UbiDisplays.Model.Surface.SpatialQueries.ToArray();
                foreach (var pQuery in tQueries)
                    pQuery.BeginFrame();

                // Wipe all the debug pixels back to transparent.
                Array.Clear(tKinectVideoDebugBackBuffer, 0, tKinectVideoDebugBackBuffer.Length);

                // Perform the actual processing.
                KinectProcessor.QueryPointCloud(tQueries, tKinectVideoDebugBackBuffer);

                // End the frame.
                foreach (var pQuery in tQueries)
                    pQuery.EndFrame();

                // Then tell the dispatcher to blit the new pixels.
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Check we have data to write to.
                    if (pKinectVideoDebugTarget == null)
                        return;

                    // Now fill in the debug information.
                    pKinectVideoDebugTarget.WritePixels(
                        new Int32Rect(0, 0, pKinectVideoDebugTarget.PixelWidth, pKinectVideoDebugTarget.PixelHeight),
                        tKinectVideoDebugBackBuffer,
                        pKinectVideoDebugTarget.PixelWidth * 4,
                        0);
                }));
            });
        }
        #endregion


        #region Interface Completion Steps
        /// <summary>
        /// Check that we have completed all the tasks required to progress to the calibration.
        /// </summary>
        /// <param name="bSelectStep">Select the tab for step 2</param>
        private void TryCompleteStep1(bool bSelectStep = true)
        {
            // If we have selected a Kinect and a Projector and both are working.
            if (SelectedScreen == null) return;
            if (SelectedKinect == null || SelectedKinect.IsRunning == false) return;
            if (OutputWindow == null || OutputWindow.Visibility == System.Windows.Visibility.Hidden) return;
            
            // Enable step 2.
            _Step2.IsEnabled = true;
            tabSettings.IsEnabled = true;

            // If we are showing the step - do so and enable calibration.
            if (bSelectStep)
            {
                tabSettings.IsSelected = true;
                _pzCalibration.FitContent();

                // Do we want to start calibration.
                if (!bCalibrated && Properties.Settings.Default.AutoStartCalibrateOnSelection)
                    Calibrate();
            }
        }

        /// <summary>
        /// Check that we have completed all the tasks required to progress to the display management.
        /// </summary>
        private void TryCompleteStep2()
        {
            // Try and start step 1 to be sure.
            TryCompleteStep1(false);

            // Check we are calibrated.
            if (bCalibrated)
            {
                // Enable step 3.
                _Step3.IsEnabled = true;
                tabDisplayControl.IsEnabled = true;
                tabDisplayControl.IsSelected = true;
                _pzManage.FitContent();

                // Ok, this is really messy - but for some reason I cannot fit the content while 
                //  it is not drawn.  So I'm going to wait for tabDisplayControl to become selected
                //  and then call the layout function.. sorry about this.. grr.
                var pWaitThread = new BackgroundWorker();
                pWaitThread.DoWork += delegate
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.2));
                    Dispatcher.BeginInvoke((Action)delegate() { _pzManage.FitContent(); });
                };
                pWaitThread.RunWorkerAsync();


                Log.Write("Ready", "Application", Log.Type.AppInfo);
            }
        }
        #endregion

        #region STEP 1
        #region Projector Selection
        /// <summary>
        /// Get or set the screen which we want to display projector output on.
        /// </summary>
        public Utilities.MonitorDetection.DisplayInfo SelectedScreen
        {
            get
            {
                return _SelectedScreen;
            }
            set
            {
                // Store the var.
                _SelectedScreen = value;

                // Update the output display.
                OutputWindow.WindowState = WindowState.Minimized;
                OutputWindow.Show();
                OutputWindow.Top = value.WorkArea.Top;
                OutputWindow.Left = value.WorkArea.Left;

                // If it is topmost, update it.
                if (Properties.Settings.Default.RenderDisplaysTopmost)
                    OutputWindow.Topmost = true;
                else
                    OutputWindow.Topmost = false;

                // Prevent it being in the task bar.
                if (Properties.Settings.Default.ShowProjectorWindowInTaskbar)
                    OutputWindow.ShowInTaskbar = true;
                else
                    OutputWindow.ShowInTaskbar = false;

                // Start the window maximised.
                OutputWindow.ResizeMode  = ResizeMode.NoResize;
                OutputWindow.WindowStyle = WindowStyle.None;
                OutputWindow.WindowState = WindowState.Maximized;

                // De-select the other monitors and select the one which matches our device name.
                foreach (var m in _Monitors.Children)
                {
                    var pMonitor = m as Interface.Controls.Monitor;
                    if (pMonitor != null && pMonitor.MonitorData != null)
                    {
                        pMonitor.Selected = false;
                        if (pMonitor.MonitorData.DeviceName == SelectedScreen.DeviceName)
                            pMonitor.Selected = true;
                    }
                }
            }
        }
        /// <summary>
        /// The internal value for the selected screen.
        /// </summary>
        private Utilities.MonitorDetection.DisplayInfo _SelectedScreen = null;

        /// <summary>
        /// Detect the monitors available and store them.
        /// </summary>
        private void DetectMonitors()
        {
            // Get a list of monitors.
            var lMonitors = Utilities.MonitorDetection.QueryDisplays();
            if (lMonitors == null || lMonitors.Count == 0)
            {
                MessageBox.Show("Your computer is not reporting that any screens are attached.");
                Model.Log.Write("Your computer is not reporting any attached screens.", "Application", Model.Log.Type.AppError);
                return;
            }

            // If we have a currently selected device, store its name so we can re-select it later.
            var pSelectedDevice = "";
            if (SelectedScreen != null)
                pSelectedDevice = SelectedScreen.DeviceName;

            // Clear the panel which contains the monitors.
            _Monitors.Children.Clear();

            // Create the hint form.
            UbiDisplays.Interface.Controls.HintForm pHintForm = null;

            // For each detected screen:
            foreach (var pScreen in lMonitors)
            {
                // Create a new "monitor" control.
                String sText = "" + pScreen.ScreenWidth + "x" + pScreen.ScreenHeight;
                var pMonitor = new Interface.Controls.Monitor();
                pMonitor.FillOutInformation(sText, pScreen.ScreenWidth, pScreen.ScreenHeight);
                pMonitor.MonitorData = pScreen;

                // Log information about it here.
                Model.Log.Write(String.Format("Detected Screen: {0}x{1}", pScreen.ScreenWidth, pScreen.ScreenHeight), "Application", Model.Log.Type.AppInfo);

                // Mark it as selected if necessary.
                if (pSelectedDevice == pScreen.DeviceName)
                    pMonitor.Selected = true;

                // Bring the var into localscope for the delegate.
                var pTempScreen = pScreen;

                // Bind hover events if we want to show monitor hover hints.
                if (Properties.Settings.Default.HoverMonitorPreview)
                {
                    pMonitor.MouseEnter += (object sender, MouseEventArgs e) =>
                    {
                        if (pHintForm == null)
                        {
                            pHintForm = new UbiDisplays.Interface.Controls.HintForm();
                            pHintForm.Width = 160;
                            pHintForm.Height = 100;
                        }
                        pHintForm.Left = pTempScreen.MonitorArea.Right - (pHintForm.Width + 100);
                        pHintForm.Top  = pTempScreen.MonitorArea.Bottom - (pHintForm.Height + 100);
                        pHintForm.Show();
                    };
                    pMonitor.MouseLeave += (object sender, MouseEventArgs e) =>
                    {
                        pHintForm.Close();
                        pHintForm = null;
                    };
                }

                // Bind click events.
                pMonitor.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
                {
                    // De-select the others.
                    //foreach (var m in _Monitors.Children)
                    //    if (m as Interface.Controls.Monitor != null)
                    //        (m as Interface.Controls.Monitor).Selected = false;

                    // Select the one we want.
                    //pMonitor.Selected = true;

                    // Move the output to the right screen.
                    SelectedScreen = pTempScreen;

                    // Say which screen we selected.
                    Model.Log.Write(String.Format("Selected Screen: {0}x{1}", SelectedScreen.ScreenWidth, SelectedScreen.ScreenHeight), "Application", Model.Log.Type.AppInfo);

                    // Enter the calibration phase (calibrate if not already done).
                    TryCompleteStep1();
                };

                // Add it to the control.
                _Monitors.Children.Add(pMonitor);
            }
        }

        /// <summary>
        /// Handle the monitor refresh button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshMonitorSelection(object sender, MouseButtonEventArgs e)
        {
            // Refresh the monitor detection list.
            DetectMonitors();
        }
        #endregion

        #region Kinect Selection

        /// <summary>
        /// Get or set which kinect sensor is activated.
        /// </summary>
        /// <remarks>Internally this calls DeactivateKinect and ActivateKinect.  It also updates the UI.</remarks>
        public KinectSensor SelectedKinect
        {
            get
            {
                return KinectProcessor.Sensor;
            }
            set
            {
                // If it is null, deactivate.
                if (value == null)
                {
                    DeactivateKinect();
                    return;
                }

                // If we are the same, then skip.
                if (KinectProcessor.Sensor == value)
                {
                    Log.Write("Kinect already active.", "Application", Log.Type.AppInfo);
                }
                else
                {
                    // Try to activate it.
                    try
                    {
                        ActivateKinect(value);
                    }
                    catch (Exception pError)
                    {
                        DeactivateKinect();
                        Model.Log.Write(String.Format("Cannot select Kinect: {0}", value.DeviceConnectionId) + "Error: " + pError.Message, "Application", Model.Log.Type.AppWarning);
                        MessageBox.Show(pError.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                    }
                }

                // Update the UI.
                foreach (var m in _Kinects.Children)
                {
                    var pKinectControl = m as KinectConrol;
                    if (pKinectControl != null)
                    {
                        pKinectControl.Opacity = 0.6;
                        if (value != null && pKinectControl.SensorDeviceConnectionID == value.DeviceConnectionId)
                            pKinectControl.Opacity = 1.0;
                    }
                }
            }
        }

        /// <summary>
        /// Detect the kinect sensors available and store them.
        /// </summary>
        private void DetectKinects()
        {
            // Store the UUID of the currently connected one.
            String sConnected = null;
            if (SelectedKinect != null)
                sConnected = SelectedKinect.DeviceConnectionId;

            // De-activate the current sensor.
            DeactivateKinect();

            // Wipe the list.
            _Kinects.Children.Clear();

            // Get a list of monitors.
            if (KinectSensor.KinectSensors == null || KinectSensor.KinectSensors.Count == 0)
            {
                MessageBox.Show("Your computer is not reporting that any Kinects are attached.");
                Model.Log.Write("Your computer is not reporting any attached Kinects.", "Application", Model.Log.Type.AppError);
                return;
            }

            // Loop through and place them in the list.
            int iSensor = 0;
            foreach (var pSensor in KinectSensor.KinectSensors)
            {
                // Write in the log.
                Model.Log.Write(String.Format("Detected Kinect: {0}", pSensor.DeviceConnectionId), "Application", Model.Log.Type.AppInfo);

                // Create the Kinect control.
                var pKinectControl = new UbiDisplays.Interface.Controls.KinectConrol();
                pKinectControl.lblName.Content = "K" + (++iSensor) + " " + pSensor.Status;
                pKinectControl.SensorDeviceConnectionID = pSensor.DeviceConnectionId;

                // Bind click events.
                if (pSensor.Status == KinectStatus.Connected)
                {
                    pKinectControl.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
                    {
                        // Select this kinect.
                        SelectedKinect = pSensor;
                        
                        // Enter the calibration phase (calibrate if not already done).
                        TryCompleteStep1();

                        // Say which screen we selected.
                        Model.Log.Write(String.Format("Selected Kinect: {0}", pSensor.DeviceConnectionId), "Application", Model.Log.Type.AppInfo);
                    };

                    // Add it to the list.
                    _Kinects.Children.Add(pKinectControl);

                    // If this is our only sensor, select it ready.
                    if (Properties.Settings.Default.AutoSelectKinect)
                    {
                        if (KinectSensor.KinectSensors.Count == 1)
                        {
                            ActivateKinect(pSensor);
                        }
                    }

                    // If it was the previously connected sensor, then select it.
                    if (sConnected == pSensor.DeviceConnectionId)
                    {
                        ActivateKinect(pSensor);
                    }
                }
            }
        }

        /// <summary>
        /// Deactivate the active kinect sensor, stop it streaming and remove the reference.
        /// </summary>
        private void DeactivateKinect()
        {
            // Bail if we have nothing to do.
            if (KinectProcessor.Sensor == null)
                return;

            // If we are active, stop us.
            try
            {
                // Stop the sensor.
                KinectProcessor.Stop();

                // Remove the video stream reference.
                pKinectVideoTarget = null;
                pKinectVideoDebugTarget = null;
                _vidCalibration.Source = null;
                _vidManage.Source = null;
            }
            catch (Exception e)
            {
                Log.Write("Error deactivating Kinect. " + e.Message, "Application", Log.Type.AppWarning);
            }
        }

        /// <summary>
        /// Select a new kinect sensor, start it streaming.
        /// </summary>
        /// <param name="pSensor"></param>
        private void ActivateKinect(KinectSensor pSensor)
        {
            // If the specified sensor is the current one, bail.
            if (pSensor == KinectProcessor.Sensor)
                return;

            // De-activate the current one.
            DeactivateKinect();

            // Start streaming data.  Do this in a seperate thread?
            try
            {
                KinectProcessor.Start(pSensor, true);

                // Create the output target and bind to the UI image.
                pKinectVideoTarget = new WriteableBitmap((int)_vidCalibration.Width, (int)_vidCalibration.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                _vidCalibration.Source = pKinectVideoTarget;
                _vidManage.Source = pKinectVideoTarget;

                // Create one for the debug output which we will blend over the top.
                pKinectVideoDebugTarget = new WriteableBitmap((int)_vidCalibration.Width, (int)_vidCalibration.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                tKinectVideoDebugBackBuffer = new byte[pKinectVideoDebugTarget.PixelWidth * pKinectVideoDebugTarget.PixelHeight * 4]; //Bgra32
                _vidManageDebug.Source = pKinectVideoDebugTarget;
            }
            catch (System.IO.IOException e)
            {
                Log.Write("Error activating Kinect. " + e.Message, "Application", Log.Type.AppWarning);
                DeactivateKinect();
            }
        }

        /// <summary>
        /// Handle the kinect refresh button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshKinectSelection(object sender, MouseButtonEventArgs e)
        {
            // Refresh the list.
            DetectKinects();
        }

        #endregion
        #endregion

        #region STEP 2
        /// <summary>
        /// Handle the user resetting the calibration zoom panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleResetCalibrationZoom_Click(object sender, RoutedEventArgs e)
        {
            _pzCalibration.FitContent();
        }

        /// <summary>
        /// Toggle the whiteout.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleToggleBackground_Click(object sender, RoutedEventArgs e)
        {
            // Bail if no output window.
            if (OutputWindow == null)
                return;

            // Toggle the colours.
            if (OutputWindow.Background == Brushes.Black)
            {
                OutputWindow.Background = Brushes.White;
                _lblWhiteout.Content = "Turn Black";
                _imgWhiteout.Source = new BitmapImage(new Uri(IMAGE_PACK_STRING + "lightbulb.png"));
                
                // You know, its interesting.  When this icon was "sun" for on and "clouds" for off, it made sense.
                //  When I changed it to be a lightbulb, because the icon represents state that we are familiar with
                //  it no longer made sense to have the desired state on the button represented by the icon.  
                //  Instead, the current state should be reflected by the icon.
            }
            else
            {
                OutputWindow.Background = Brushes.Black;
                _lblWhiteout.Content = "Turn White";
                _imgWhiteout.Source = new BitmapImage(new Uri(IMAGE_PACK_STRING + "lightbulb_off.png"));
            }
            
        }

        /// <summary>
        /// Handle the user clicking the clear discarded area button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleClearSelectionArea_Click(object sender, RoutedEventArgs e)
        {
            // Take a copy of the current processed pixels.
            var pProcessedPixels = (System.Collections.BitArray)KinectProcessor.EnabledPixels.Clone();

            // For each pixel, allow if contained within the polygon.
            pProcessedPixels.SetAll(true);

            // Save the updated bit set back to the kinect processor.
            KinectProcessor.EnabledPixels = pProcessedPixels;
        }

        /// <summary>
        /// Handle the user clicking the accept area button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleAcceptArea_Click(object sender, RoutedEventArgs e)
        {
            EnterSelectProcessingRegionMode(true);
        }

        /// <summary>
        /// Handle the user clicking the discard area button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDiscardArea_Click(object sender, RoutedEventArgs e)
        {
            EnterSelectProcessingRegionMode(false);
        }

        /// <summary>
        /// Handle the user clicking the calibration button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleCalibrate_Click(object sender, RoutedEventArgs e)
        {
            // Start the calibration process.
            Calibrate();
        }

        /// <summary>
        /// Calling this method means that clicking on the calibration form will select/deselect regions for processing.
        /// </summary>
        /// <param name="bAllowValue">Do we want to allow the selected region.  True for allow, False for deny.</param>
        private void EnterSelectProcessingRegionMode(bool bAllowValue)
        {
            // Mouse events.
            MouseButtonEventHandler Down = null;
            MouseEventHandler Move       = null;
            MouseButtonEventHandler Up   = null;

            Move = new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                // If the mouse is captured.
                if (_vidCalibration.IsMouseCaptured)
                {
                    // Convert the click point into an X,Y reference into the sync frame.
                    var pClick = e.GetPosition(_vidCalibration);

                    // Add it to the visual render list.
                    pClick = new Point(Math.Max(0, Math.Min(pClick.X, _vidCalibration.Width)), Math.Max(0, Math.Min(pClick.Y, _vidCalibration.Height)));
                    pAllowedSurfacePoly.Points.Add(pClick);
                }
            });

            Up = new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) =>
            {
                // If the mouse is captured.
                if (_vidCalibration.IsMouseCaptured)
                {
                    // Remove all the handlers.
                    _vidCalibration.MouseLeftButtonDown -= Down;
                    _vidCalibration.MouseMove -= Move;
                    _vidCalibration.MouseLeftButtonUp -= Up;

                    //bDrawingNewSurface = false;
                    _vidCalibration.ReleaseMouseCapture();
                    pAllowedSurfacePoly.Visibility = System.Windows.Visibility.Hidden;

                    // Say we are doing an update.
                    Log.Write("Started updating processing area.", "Application", Log.Type.AppInfo);

                    // Copy some variables to avoid threading issues.
                    var lPoints = pAllowedSurfacePoly.Points.ToList();
                    var iWidth  = (int)_vidCalibration.Width;

                    // Bail if we don't have enough points.
                    if (lPoints.Count < 3)
                        return;

                    // Do the processing in another thread.
                    BackgroundWorker pWorker = new BackgroundWorker();
                    pWorker.DoWork += (object pSender, DoWorkEventArgs eWork) =>
                    {
                        // Take a copy of the current processed pixels.
                        var pProcessedPixels = (System.Collections.BitArray)KinectProcessor.EnabledPixels.Clone();

                        // Compute a bounding box - reduce the processing area.
                        int xMax = (int)lPoints[0].X;
                        int xMin = (int)lPoints[0].X;
                        int yMax = (int)lPoints[0].Y;
                        int yMin = (int)lPoints[0].Y;
                        for (int i = 1, n = lPoints.Count; i < n; ++i)
                        {
                            xMax = (int)Math.Max(lPoints[i].X, xMax);
                            yMax = (int)Math.Max(lPoints[i].Y, yMax);
                            xMin = (int)Math.Min(lPoints[i].X, xMin);
                            yMin = (int)Math.Min(lPoints[i].Y, yMin);
                        }

                        // Test if each pixel in the bounding box lies in the polygon - update the bit set.
                        for (int y = yMin; y < yMax; ++y)
                        {
                            for (int x = xMin; x < xMax; ++x)
                            {
                                if (Utilities.Polygon.IsPointInPolygon(x, y, lPoints))
                                    pProcessedPixels.Set(y * iWidth + x, bAllowValue);
                            }
                        }

                        // Save the updated bit set back to the kinect processor.
                        KinectProcessor.EnabledPixels = pProcessedPixels;
                    };

                    // Tell the log that we finished updating.
                    pWorker.RunWorkerCompleted += (object pSender, RunWorkerCompletedEventArgs eWork) =>
                    {
                        Log.Write("Finished updating processing area.", "Application", Log.Type.AppInfo);
                    };

                    // Kick off the work job.
                    pWorker.RunWorkerAsync();
                }
            });

            Down = new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) =>
            {
                // Add the handlers for the move and up clicks.
                _vidCalibration.MouseMove += Move;
                _vidCalibration.MouseLeftButtonUp += Up;

                // Capture mouse input.
                _vidCalibration.CaptureMouse();

                // Show the poly for drawing.
                pAllowedSurfacePoly.Visibility = System.Windows.Visibility.Visible;
                pAllowedSurfacePoly.Points.Clear();
            });

            // Add our handler.
            _vidCalibration.MouseLeftButtonDown += Down;
        }
        #endregion


        #region Calibration Code
        /// <summary>
        /// Is the app set to calibrated.
        /// </summary>
        private bool bCalibrated = false;

        /// <summary>
        /// Is the app currently in a calibrating state.
        /// </summary>
        private bool bCalibrating = false;

        /// <summary>
        /// The default calibration point corners in projector space.
        /// </summary>
        private Point[] tDefaultProjectorCorners = new Point[4] { new Point(0.1, 0.1), new Point(0.9, 0.1), new Point(0.1, 0.9), new Point(0.9, 0.9) };

        /// <summary>
        /// Starts the calibration process to obtain and set data.
        /// </summary>
        /// <remarks>Calls calibrationComplete when its done.</remarks>
        private void Calibrate()
        {
            // Say we are not calibrated.
            if (bCalibrating)
            {
                Log.Write("Cannot begin calibration.  Already calibrating.", "Application", Log.Type.AppWarning);
                return;
            }
            //this.bCalibrated = false;

            Log.Write("Started 4-point calibration.", "Application", Log.Type.AppInfo);

            // Prime the variables.
            int iCorner = 0;
            var tKinectCorners = new Point[4];      // The four points in the kinect depth image.
            var tProjectorCorners = new Point[4];   // The four points in the projected image.

            // Setup the calibration form on the renderer.
            OutputWindow.NormalisedCalibrationPoint = tDefaultProjectorCorners[iCorner];
            OutputWindow.ShowCalibrationPoint = true;

            // Handle each subsequent click on the video feed in this function.
            MouseButtonEventHandler pCalibrateHandler = null;
            pCalibrateHandler = new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) =>
            {
                // Convert the click point into an X,Y reference into the sync frame.
                var pClickPos = e.GetPosition(_vidCalibration);

                // Store the corners from each dimension.
                tProjectorCorners[iCorner] = OutputWindow.NormalisedCalibrationPoint;
                tKinectCorners[iCorner]    = new Point(pClickPos.X, pClickPos.Y);

                // Increment the corner.
                ++iCorner;

                // If there are more calibration points left to get.
                if (iCorner < tDefaultProjectorCorners.Length)
                {
                    // Set them up ready for the next click.
                    OutputWindow.NormalisedCalibrationPoint = tDefaultProjectorCorners[iCorner];
                    OutputWindow.ShowCalibrationPoint = true;
                    _pzCalibration.FitContent(); //ResetTransform();
                }

                // Otherwise the calibration process is finished.
                else
                {
                    // Zoom out the pan window.
                    _pzCalibration.FitContent();

                    // Hide the calibration point.
                    OutputWindow.ShowCalibrationPoint = false;

                    // Remove our handler.
                    _vidCalibration.MouseLeftButtonUp -= pCalibrateHandler;

                    // Pass it to the thingy that creates the homography matrix.
                    Calibrate(tProjectorCorners, tKinectCorners);
                    Log.Write("Finished 4-point calibration.", "Application", Log.Type.AppInfo);

                    bCalibrating = false;
                }
            });

            // Add our handler.
            bCalibrating = true;
            _vidCalibration.MouseLeftButtonUp += pCalibrateHandler;
        }

        /// <summary>
        /// Setup this interface to use specific calibration data.
        /// </summary>
        /// <param name="tProjector">The four calibration points in the projector coordinate space.</param>
        /// <param name="tKinect">The corresponding four calibration points in the kinect video coordinate space.</param>
        private void Calibrate(Point[] tProjector, Point[] tKinect)
        {
            // Map the kinect (pixel space).
            pHomography.Source = tKinect;
            pHomography.Destination = tProjector;

            // Compute the matrix.
            pHomography.ComputeWarp();

            // Compute the calibration plane (which goes into the KinectProcessor for relative rendering).
            KinectProcessor.CalibrationPlane = KinectProcessor.ImageCoordinatesToBestFitPlane(tKinect);

            // Say we are calibrated.
            this.bCalibrated = true;

            // Try to move us to the next step
            TryCompleteStep2();
        }
        #endregion

        #region Draw Surface
        /// <summary>
        /// Is the user currently in surface drawing mode.
        /// </summary>
        private bool bDrawingNewSurface = false;

        /// <summary>
        /// Handle starting drawing a new surface.
        /// </summary>
        private void EnterDrawSurfaceMode()
        {
            // If we are already drawing, bail.
            if (bDrawingNewSurface)
                return;

            // Make a mode which lets us draw a display.
            var lPoints = new List<Point>();

            // Bind a click event to the feed.
            MouseButtonEventHandler pDrawHandler_Down = null;
            MouseEventHandler pDrawHandler_Move = null;
            MouseButtonEventHandler pDrawHandler_Up = null;

            pDrawHandler_Move = new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                // If the mouse is captured.
                if (bDrawingNewSurface && _vidManage.IsMouseCaptured)
                {
                    // Convert the click point into an X,Y reference into the sync frame.
                    var pClick = e.GetPosition(_vidManage);

                    // Add the point to the list.
                    lPoints.Add(pClick);

                    // Add it to the visual render list.
                    pDrawSurfacePoly.Points.Add(pClick);
                }
            });

            pDrawHandler_Up = new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) =>
            {
                // If the mouse is captured.
                if (bDrawingNewSurface && _vidManage.IsMouseCaptured)
                {
                    // Remove all the handlers.
                    _vidManage.MouseLeftButtonDown -= pDrawHandler_Down;
                    _vidManage.MouseMove -= pDrawHandler_Move;
                    _vidManage.MouseLeftButtonUp -= pDrawHandler_Up;

                    bDrawingNewSurface = false;
                    _vidManage.ReleaseMouseCapture();

                    // If we don't have enough points to create a surface, bail.
                    if (lPoints.Count < 10)
                        return;

                    // Compute the minimum bounding rectangle from the selected points.
                    Point[] tRectangle = Utilities.RotatingCalipers.ComputeMinimumBoundingRectangle(lPoints, true);

                    // Update the UI rectangle to show the new surface rect.
                    pDrawSurfacePoly.Points.Clear();
                    pDrawSurfacePoly.Points.Add(new Point(tRectangle[0].X, tRectangle[0].Y));
                    pDrawSurfacePoly.Points.Add(new Point(tRectangle[1].X, tRectangle[1].Y));
                    pDrawSurfacePoly.Points.Add(new Point(tRectangle[2].X, tRectangle[2].Y));
                    pDrawSurfacePoly.Points.Add(new Point(tRectangle[3].X, tRectangle[3].Y));

                    // Pen up.
                    this.Cursor = Cursors.Arrow;
                    _pzManage.Cursor = Cursors.Arrow;

                    // Tell us to create a display surface here.
                    CreateSurface(tRectangle);
                }
            });

            pDrawHandler_Down = new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) =>
            {
                // Add the handlers for the move and up clicks.
                _vidManage.MouseMove += pDrawHandler_Move;
                _vidManage.MouseLeftButtonUp += pDrawHandler_Up;

                // Capture mouse input.
                _vidManage.CaptureMouse();
                bDrawingNewSurface = true;

                // Show the poly for drawing.
                pDrawSurfacePoly.Visibility = System.Windows.Visibility.Visible;
                pDrawSurfacePoly.Points.Clear();
                lPoints.Clear();
            });

            // Add our handler.
            _vidManage.MouseLeftButtonDown += pDrawHandler_Down;
            bDrawingNewSurface = true;

            // Draw mode.
            this.Cursor = Cursors.Pen;
            _pzManage.Cursor = Cursors.Pen;
        }

        /// <summary>
        /// Create a surface which a display can be placed on and setup basic properties.
        /// </summary>
        /// <param name="tRectangle">The points in the video feed that represent where we want to put the surface.</param>
        public void CreateSurface(Point[] tRectangle)
        {
            // Get a name for the surface from the user.
            int iSurfaceCount = 0;
            String sName = "Surface " + iSurfaceCount;
            while (sName == "" || Authority.FindSurface(sName) != null)
            {
                sName = "Surface " + (++iSurfaceCount);// Microsoft.VisualBasic.Interaction.InputBox("Please enter a unique name for this surface?", "Enter Surface Name", "Surface " + (iNextSurface++));
            }

            // Create the new surface and register it.
            var pSurface = new UbiDisplays.Model.Surface(sName);
            Authority.RegisterSurface(pSurface);

            // Estimate the projector points (transfrom using our user-homography matrix).
            var tProjector = new List<Point>(from f in tRectangle select pHomography.Transform((float)f.X, (float)f.Y)).ToArray();
            var t3DPoints = KinectProcessor.ImagePointsTo3DDepth(tRectangle).ToArray();

            // Update the surface with this information.
            pSurface.SetSpatialProperties(tProjector, t3DPoints, tRectangle);

            // Reset the Kinect Pan/Zoom matrix.
            _pzCalibration.FitContent();
            _pzManage.FitContent();

            // Hide the polygon.
            pDrawSurfacePoly.Visibility = System.Windows.Visibility.Hidden;

            // Show a test display.
            //var pView = new Display("http://www.google.com");
            //Authority.ShowDisplay(pView, pSurface);
        }

        #endregion

        #region Corner Drag Mouse Interaction Events
        /// <summary>
        /// Is a surface selected to drag.
        /// </summary>
        private bool bDraggingSurface = false;

        /// <summary>
        /// Find the closest corner on a surface in projector space.
        /// </summary>
        /// <param name="pPoint">The point relative to the image.  This function will transform this point into projector space.</param>
        /// <param name="pSelected">The destination for the selected surface, should one be found.</param>
        /// <param name="iClosestCorner">The corner index closest to the point on the selected surface, should one be found.</param>
        /// <param name="fThreshold">The threshold value for checking if we should disregard the corner entirely.</param>
        /// <returns>True if a corner was found, false if not.</returns>
        private bool FindClosestSurfaceAndCorner(Point pPoint, out UbiDisplays.Model.Surface pSelected, out int iClosestCorner, double fThreshold = 0.1)
        {
            // Look through each surface and try to find one with a close corner.
            double fSmallest = double.PositiveInfinity;

            // Set default values.
            pSelected = null;
            iClosestCorner = -1;

            // Corner vector.
            var pProjector = pHomography.Transform((float)pPoint.X, (float)pPoint.Y);
            var vProjector = new Vector(pProjector.X, pProjector.Y);

            // For each surface.
            foreach (var s in Authority.Surfaces)
            {
                // Look through each corner to see how close it is to this point.
                for (int i = 0, n = 4; i < n; ++i)
                {
                    Point tCorner = s.ProjectorSpace[i];
                    double fDistance = Vector.Subtract(vProjector, new Vector(tCorner.X, tCorner.Y)).Length;
                    if (fDistance < fSmallest && fDistance < fThreshold)
                    {
                        fSmallest = fDistance;
                        iClosestCorner = i;
                        pSelected = s;
                    }
                }
            }

            // Say if we found a corner.
            return (iClosestCorner != -1);
        }

        /// <summary>
        /// Called when the mouse is pressed down on the HStream canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CornerDrag_InnerMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only allow this if we are calibrated and not drawing.
            if (!this.bCalibrated || bDrawingNewSurface)
                return;

            // Bind a click event to the feed.
            MouseEventHandler Move = null;
            MouseButtonEventHandler Up = null;

            // Move both projector and sensor by default.
            bool bDragEntireDisplay = false;
            bool bDragProjector = true;
            bool bDragSensor = true;
            int iClosestCorner = -1;
            UbiDisplays.Model.Surface pSelectedSurface = null;

            // Get the current surface properties.
            Point[] tNewProjector = null;// pSelectedSurface.ProjectorSpace.ToArray();
            SlimMath.Vector3[] tNewSensor = null;// pSelectedSurface.SensorSpace.ToArray();
            Point[] tNewImageSpace = null;// pSelectedSurface.KinectSpace.ToArray();

            Point vCornerStart = new Point();
            Point[] tImageCoords = null;

            // Work out if we just want to move the projection or sensor.
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                // We are holding down shift, so disable projection move.
                bDragProjector = false;
                bDragSensor = true;

                // Show the sensor polygon?
                pSurfaceCalibrationAdjustPoly.Visibility = System.Windows.Visibility.Visible;
            }


            // Handle mouse move events.
            Move = new MouseEventHandler((object sender1, MouseEventArgs e1) =>
            {
                // If the mouse is captured.
                if (bDraggingSurface && _vidManage.IsMouseCaptured)
                {
                    // Get the point in projector space.
                    var pMovePoint = e.GetPosition(_vidManage);

                    // If we are dragging the entire display.
                    if (bDragEntireDisplay)
                    {
                        // Compute the movement delta.
                        var tDeltaX = vCornerStart.X - pMovePoint.X;
                        var tDeltaY = vCornerStart.Y - pMovePoint.Y;

                        // Copy the rectange coordindates.
                        var tLiveImageCoords = pSelectedSurface.KinectSpace.ToArray();
                        for (int i = 0; i < tLiveImageCoords.Length; ++i)
                        {
                            tLiveImageCoords[i].X = tImageCoords[i].X - tDeltaX;
                            tLiveImageCoords[i].Y = tImageCoords[i].Y - tDeltaY;
                        }
                        var tProjector = new List<Point>(from f in tLiveImageCoords select pHomography.Transform(f.X, f.Y)).ToArray();
                        var t3DPoints = KinectProcessor.ImagePointsTo3DDepth(tLiveImageCoords).ToArray();
                        pSelectedSurface.SetSpatialProperties(tProjector, t3DPoints, tLiveImageCoords);
                        return;
                    }

                    // Update the projector corner from the user homography.
                    if (bDragProjector)
                    {
                        tNewProjector[iClosestCorner] = pHomography.Transform(pMovePoint);
                    }

                    // Update the sensor corner from the sync pixel.
                    if (bDragSensor)
                    {
                        tNewSensor[iClosestCorner] = KinectProcessor.ImageTo3DDepth((int)pMovePoint.X, (int)pMovePoint.Y);
                        tNewImageSpace[iClosestCorner] = pMovePoint;
                    }

                    // Update the spatial properties.
                    pSelectedSurface.SetSpatialProperties(tNewProjector, tNewSensor, pSelectedSurface.KinectSpace);

                    // Update the surface calibration polygon.. just visual stuff here.. not important.
                    pSurfaceCalibrationAdjustPoly.Points.Clear();
                    pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[0]);
                    pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[1]);
                    pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[2]);
                    pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[3]);
                }
            });

            // Handle mouse up events.
            Up = new MouseButtonEventHandler((object sender1, MouseButtonEventArgs e1) =>
            {
                // If the mouse is captured.
                if (bDraggingSurface && _vidManage.IsMouseCaptured)
                {
                    // Remove all the handlers and update the flags.
                    _vidManage.MouseMove -= Move;
                    _vidManage.MouseLeftButtonUp -= Up;
                    bDraggingSurface = false;
                    pSelectedSurface = null;
                    iClosestCorner = -1;

                    // Release the mouse capture.
                    _vidManage.ReleaseMouseCapture();

                    // Dragging mouse.
                    this.Cursor = Cursors.Arrow;
                    _pzManage.Cursor = Cursors.Arrow;

                    // Hide the polygon for drawing the sensor bounds.
                    pSurfaceCalibrationAdjustPoly.Visibility = System.Windows.Visibility.Hidden;
                }
            });


            // Find the corner we are closest to.
            var pClick = e.GetPosition(_vidManage);
            if (FindClosestSurfaceAndCorner(pClick, out pSelectedSurface, out iClosestCorner, 0.05))
            {
                // Set the flag which says we are dragging the surface.
                bDraggingSurface = true;

                // Populate the arrays.
                tNewProjector = pSelectedSurface.ProjectorSpace.ToArray();
                tNewSensor = pSelectedSurface.SensorSpace.ToArray();
                tNewImageSpace = pSelectedSurface.KinectSpace.ToArray();

                // Capture the mouse, so we get all subsquent mouse events.
                _vidManage.MouseMove += Move;
                _vidManage.MouseLeftButtonUp += Up;
                _vidManage.CaptureMouse();

                // If we are holding ctrl, make us into drag mode.
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    vCornerStart = pClick;
                    tImageCoords = pSelectedSurface.KinectSpace.ToArray();
                    bDragEntireDisplay = true;
                    pSurfaceCalibrationAdjustPoly.Visibility = System.Windows.Visibility.Hidden;

                    this.Cursor = Cursors.Cross;
                    _pzManage.Cursor = Cursors.Cross;
                    return;
                }

                // Update the projector corner from the user homography.
                if (bDragProjector)
                {
                    tNewProjector[iClosestCorner] = pHomography.Transform(pClick);
                }

                // Update the sensor corner from the sync pixel.
                if (bDragSensor)
                {
                    tNewSensor[iClosestCorner] = KinectProcessor.ImageTo3DDepth((int)pClick.X, (int)pClick.Y);
                    tNewImageSpace[iClosestCorner] = pClick;
                }

                // Update the spatial properties.
                pSelectedSurface.SetSpatialProperties(tNewProjector, tNewSensor, tNewImageSpace);

                // Update the surface calibration polygon.. just visual stuff here.. not important.
                pSurfaceCalibrationAdjustPoly.Points.Clear();
                pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[0]);
                pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[1]);
                pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[2]);
                pSurfaceCalibrationAdjustPoly.Points.Add(tNewImageSpace[3]);

                // Dragging mouse.
                this.Cursor = Cursors.Cross;
                _pzManage.Cursor = Cursors.Cross;
            }
        }
        #endregion 


        #region Step 3
        /// <summary>
        /// Handle changes to the list of surfaces.
        /// </summary>
        private void Authority_OnSurfaceListChanged()
        {
            // Dispatch the following code to the UI thread.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PopulateSurfaceList();
            }));
        }

        /// <summary>
        /// Update the list of surfaces to reflect any changes.
        /// </summary>
        private void PopulateSurfaceList()
        {
            // For each surface.
            var lSurfaces = Authority.Surfaces;

            // Store the selected index.
            //int iSelected = _SurfaceList.SelectedIndex;

            // Clear the list.
            _SurfaceList.Children.Clear();

            // Re-create the list.
            foreach (var pSurface in lSurfaces)
            {
                // Add the view.
                var pView = new SurfaceListItem() { Surface = pSurface, Height = 30 };
                _SurfaceList.Children.Add(pView);

                // When any is clicked, unhighlight all then highlight it.
            }
        }

        /// <summary>
        /// A list of lines which are show over the closest corner to the mouse.
        /// </summary>
        private Ellipse pHoverEllipse = null;

        /// <summary>
        /// Handle the resize zoom button on the display management tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleResetManagmentZoom(object sender, RoutedEventArgs e)
        {
            _pzManage.FitContent();
        }

        /// <summary>
        /// Handle the begin drawing surface button on the display managment tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDrawSurface(object sender, RoutedEventArgs e)
        {
            EnterDrawSurfaceMode();
        }

        /// <summary>
        /// Handle the reload all displays and surfaces button on the display management tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleReloadDisplaysAndSurfaces(object sender, RoutedEventArgs e)
        {
            // Get a list of all the surfaces.
            var lSurfaces = Authority.Surfaces;
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
            Log.Write("User forced " + sType + " of all active displays.", "Application", Log.Type.AppInfo);
        }

        /// <summary>
        /// When we move the mouse over the kinect video on the display management tab, project the mouse over that point in the real world.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handle_DisplayManagmentVideoMouseMove(object sender, MouseEventArgs e)
        {
            // If we are calibrated.
            if (this.bCalibrated)
            {
                // Convert the click point into an X,Y reference into the sync frame.
                var pClick = e.GetPosition(_vidManage);

                // Measure the height at this distance?
                //var fDistance = Utilities.RatcliffPlane.Distance(KinectProcessor.ImageTo3DDepth((int)pClick.X, (int)pClick.Y), KinectProcessor.CalibrationPlane);
                //Log.Write(""+fDistance, "Application", Log.Type.AppInfo);

                // Draw the test point to test the accuracy of the calibration.
                OutputWindow.RenderSurface.NormalisedTestPoint = pHomography.Transform(pClick);

                // If we are NOT calibrating, drawing a display, dragging, or dragging a file in.
                if (bCalibrating == false && bDrawingNewSurface == false && bDraggingSurface == false && bDraggingFile == false)
                {
                    // Find the closest corner to our display.
                    int iCorner = -1;
                    UbiDisplays.Model.Surface pSurface = null;
                    if (FindClosestSurfaceAndCorner(pClick, out pSurface, out iCorner, 0.05))
                    {
                        /*
                        // Create lines if missing.
                        if (pHoverEllipse == null)
                        {
                            pHoverEllipse = new Ellipse()
                            {
                                Stroke = Brushes.Red,
                                StrokeThickness = 0.3,
                                Width = 2,
                                Height = 2,
                            };
                            _pzManage.Children.Add(pHoverEllipse);
                        }

                        // Position lines.
                        pHoverEllipse.Visibility = System.Windows.Visibility.Visible;
                        Canvas.SetLeft(pHoverEllipse, pSurface.KinectSpace[iCorner].X);
                        Canvas.SetTop(pHoverEllipse, pSurface.KinectSpace[iCorner].Y);
                        */

                        // If we are holding shift, indicate we are changing the touch points with the hand.
                        if (Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            this.Cursor = Cursors.Hand;
                            _pzManage.Cursor = Cursors.Hand;
                        }

                        // Otherwise use the arrow for the projection.
                        else
                        {
                            this.Cursor = Cursors.UpArrow;
                            _pzManage.Cursor = Cursors.UpArrow;
                        }

                        //
                    }
                    else
                    {
                        /*
                        if (pHoverEllipse != null)
                            pHoverEllipse.Visibility = System.Windows.Visibility.Hidden;
                        */
                        this.Cursor = Cursors.Arrow;
                        _pzManage.Cursor = Cursors.Arrow;
                    }
                }
                
            }
        }

        #region Drag Drop new views onto surfaces
        /// <summary>
        /// A list of polygons which represent the surfaces when we start dragging.
        /// </summary>
        private List<Polygon> lDragPolygons = new List<Polygon>();

        /// <summary>
        /// Is the user dragging a file into the feed.
        /// </summary>
        private bool bDraggingFile = false;

        /// <summary>
        /// Called to handle something being dropped on the image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDisplay_Drop(object sender, DragEventArgs e)
        {
            // Not dragging anymore.
            bDraggingFile = false;

            // Bail if not a file.
            var sFile = "http://google.com";

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Select the file to load.
                string[] tFilePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                if (tFilePaths.Length != 1)
                    return;
                sFile = tFilePaths[0];
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                sFile = e.Data.GetData(DataFormats.StringFormat) as String;
                if (sFile == null || (!sFile.StartsWith("http") && !sFile.StartsWith("file")))
                    return;
            }

            // Find which polygon we have dropped on.
            var pClick = e.GetPosition(_vidManage);

            // Check to see if our point is contained by any of our surfaces.
            UbiDisplays.Model.Surface pSurface = null;
            foreach (var s in Authority.Surfaces)
            {
                List<Point> lPoints = new List<Point>();
                lPoints.Add(new Point(s.KinectSpace[0].X, s.KinectSpace[0].Y));
                lPoints.Add(new Point(s.KinectSpace[1].X, s.KinectSpace[1].Y));
                lPoints.Add(new Point(s.KinectSpace[2].X, s.KinectSpace[2].Y));
                lPoints.Add(new Point(s.KinectSpace[3].X, s.KinectSpace[3].Y));

                // If we have dropped it into this polygon.
                if (Utilities.Polygon.IsPointInPolygon(pClick.X, pClick.Y, lPoints))
                {
                    pSurface = s;
                    break;
                }
            }

            // If we have missed all the surfaces.
            if (pSurface == null)
                return;

            // If there is already something on the surface, remove it.
            if (pSurface.ActiveDisplay != null)
            {
                Authority.DeleteDisplay(pSurface.ActiveDisplay);
            }

            // Create a new display.
            var pDisplay = new Display(sFile);
            Authority.ShowDisplay(pDisplay, pSurface);
            pSurface.ShowDebug = false;

            // Ensure debug mode is turned off.
            // TODO
            //pSurface.DebugMode = false;

            // Remove the polygons.
            foreach (var pPoly in lDragPolygons)
                _pzManage.Children.Remove(pPoly);
            lDragPolygons.Clear();
        }

        /// <summary>
        /// Called to handle a file being dragged over the image which contains the surfaces.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDisplay_DragEnter(object sender, DragEventArgs e)
        {
            // Check we are dragging a file..
            //if (!e.Data.GetDataPresent(DataFormats.FileDrop, true))
            //    return;

            // Signal we are dragging a file.
            bDraggingFile = true;

            // Remove any old polygons, just in case.
            foreach (var pPoly in lDragPolygons)
                _pzManage.Children.Remove(pPoly);
            lDragPolygons.Clear();

            // For each possible surface.
            foreach (var s in Authority.Surfaces)
            {
                // Add the surface creator poly.
                var pPoly = new Polygon();
                pPoly.Stroke = Brushes.LightGreen;
                pPoly.Fill = new SolidColorBrush(Color.FromArgb(78, 00, 0x7f, 0x0e));
                pPoly.StrokeThickness = 0.5;
                _pzManage.Children.Add(pPoly);
                lDragPolygons.Add(pPoly);

                pPoly.IsHitTestVisible = false;
                pPoly.Visibility = System.Windows.Visibility.Visible;
                pPoly.Points.Add(new Point(s.KinectSpace[0].X, s.KinectSpace[0].Y));
                pPoly.Points.Add(new Point(s.KinectSpace[1].X, s.KinectSpace[1].Y));
                pPoly.Points.Add(new Point(s.KinectSpace[2].X, s.KinectSpace[2].Y));
                pPoly.Points.Add(new Point(s.KinectSpace[3].X, s.KinectSpace[3].Y));
            }
        }

        /// <summary>
        /// Called to handle a file being no longer over the image which contains the surfaces.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDisplay_DragLeave(object sender, DragEventArgs e)
        {
            // Signal we are not dragging a file.
            bDraggingFile = false;

            // Remove any old ones.
            foreach (var pPoly in lDragPolygons)
                _pzManage.Children.Remove(pPoly);
            lDragPolygons.Clear();
        }

        /// <summary>
        /// Handle when a polygon is dragged over!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDisplay_DragOver(object sender, DragEventArgs e)
        {
            // Find which polygon we have dropped on.
            var pClick = e.GetPosition(_vidManage);

            // Check to see if our point is contained by any of our surfaces.
            foreach (var pPoly in lDragPolygons)
            {
                pPoly.Stroke = Brushes.LightGreen;
                if (Utilities.Polygon.IsPointInPolygon(pClick.X, pClick.Y, pPoly.Points))
                {
                    pPoly.Stroke = Brushes.Yellow;
                    break;
                }
            }
        }
        #endregion

        #endregion


        #region Debug Tab
        /// <summary>
        /// Handle the button click for launching the Google Chrome inspector.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_LaunchChromeInspector(object sender, RoutedEventArgs e)
        {
            // Chrome path.
            var sChromePath = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.ChromePath);

            // Check to see if we have chrome installed.
            if (!System.IO.File.Exists(sChromePath))
            {
                var eResult = MessageBox.Show("Google Chrome is not installed.  Would you like to download it?\nGoogle Chrome is required to interactively inspect and debug display content.", "Ubi Displays", MessageBoxButton.YesNo);
                if (eResult == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("http://google.com/chrome");
                }
                else
                {
                    // Do nothing.
                    Log.Write("Could not launch element inspector.  Google Chrome not installed.", "Application", Log.Type.AppInfo);
                }
            }
            else
            {
                // Otherwise, lets open it.
                var sInspectorPath = "http://localhost:" + Properties.Settings.Default.RemoteDebuggingPort;
                System.Diagnostics.Process.Start(sChromePath, sInspectorPath);
                Log.Write("Launching Element Inspector: " + sInspectorPath, "Application", Log.Type.AppInfo);
            }
        }
        #endregion

        #region Save Load
        /// <summary>
        /// A flag which checks if we have already chosen the export settings.
        /// </summary>
        private bool bSettingsChosen = false;

        /// <summary>
        /// Export settings.
        /// </summary>
        private bool bCalib, bSurfaces, bDisplays = true;

        /// <summary>
        /// Current save file, set by save as.  Used when saving.
        /// </summary>
        private String sConfigurationFile = null;

        /// <summary>
        /// Handle someone dropping a file on the load button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadButton_Drop(object sender, DragEventArgs e)
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

            // Open the file.
            if (sFile == null)
                return;

            // Get the new load settings.
            bool bTmpCalib, bTmpSurfaces, bTmpDisplays = true;
            if (!ImportExport.OpenImportDialog(out bTmpCalib, out bTmpSurfaces, out bTmpDisplays))
                return;

            // Attempt to load the data.
            try
            {
                this.Load(bTmpCalib, bTmpSurfaces, bTmpDisplays, sFile);
                Log.Write("Loaded from " + sFile, "Application", Log.Type.AppInfo);
            }
            catch (ArgumentNullException excp)
            {
                Log.Write("Unable to load. Is all the required XML present?", "Application", Log.Type.AppWarning);
                sConfigurationFile = "";
                bSettingsChosen = false;
            }
            catch (Exception excp)
            {
                Log.Write("Unable to load. " + excp.Message, "Application", Log.Type.AppWarning);
                MessageBox.Show("Unable to load. " + excp.Message, "Ubi Displays", MessageBoxButton.OK, MessageBoxImage.Warning);
                sConfigurationFile = "";
                bSettingsChosen = false;
            }
        }

        /// <summary>
        /// Handle the user pressing the load button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_LoadClick(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box 
            var pDialog = new Microsoft.Win32.OpenFileDialog();
            pDialog.FileName = "Untitled Configuration";    // Default file name 
            pDialog.DefaultExt = ".ubi";                    // Default file extension 
            pDialog.Filter = "Ubi Displays Configuration File (.ubi)|*.ubi"; // Filter files by extension 

            // Show open file dialog box 
            var hResult = pDialog.ShowDialog();
            if (hResult == null || hResult == false)
                return;

            // Get the new load settings.
            bool bTmpCalib, bTmpSurfaces, bTmpDisplays = true;
            if (!ImportExport.OpenImportDialog(out bTmpCalib, out bTmpSurfaces, out bTmpDisplays))
                return;

            // Attempt to load the data.
            //this.Load(bTmpCalib, bTmpSurfaces, bTmpDisplays, pDialog.FileName);
            try
            {
                this.Load(bTmpCalib, bTmpSurfaces, bTmpDisplays, pDialog.FileName);
                Log.Write("Loaded from " + pDialog.FileName, "Application", Log.Type.AppInfo);
            }
            catch (ArgumentNullException excp)
            {
                Log.Write("Unable to load. Is all the required XML present?", "Application", Log.Type.AppWarning);
                sConfigurationFile = "";
                bSettingsChosen = false;
            }
            catch (Exception excp)
            {
                Log.Write("Unable to load. " + excp.Message, "Application", Log.Type.AppWarning);
                MessageBox.Show("Unable to load. " + excp.Message, "Ubi Displays", MessageBoxButton.OK, MessageBoxImage.Warning);
                sConfigurationFile = "";
                bSettingsChosen = false;
            }
        }

        /// <summary>
        /// Handle save as clicking.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SaveAsClick(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box.
            var pDialog = new Microsoft.Win32.SaveFileDialog();
            pDialog.FileName = "Untitled";    // Default file name 
            pDialog.DefaultExt = ".ubi";      // Default file extension 
            pDialog.Filter = "Ubi Displays Configuration File (.ubi)|*.ubi"; // Filter files by extension

            // Show save file dialog box.
            var hResult = pDialog.ShowDialog();
            if (hResult == null || hResult == false)
                return;

            // Get the save settings.
            bool bTmpCalib, bTmpSurfaces, bTmpDisplays = true;
            if (!ImportExport.OpenExportDialog(out bTmpCalib, out bTmpSurfaces, out bTmpDisplays))
                return;

            // Save the settings.
            sConfigurationFile = pDialog.FileName;
            bCalib = bTmpCalib;
            bSurfaces = bTmpSurfaces;
            bDisplays = bTmpDisplays;
            bSettingsChosen = true;

            // Do the save.
            try
            {
                Save(bCalib, bSurfaces, bDisplays, sConfigurationFile);
                Log.Write("Saved to " + sConfigurationFile, "Application", Log.Type.AppInfo);
            }
            catch (Exception excp)
            {
                Log.Write("Unable to save. " + excp.Message, "Application", Log.Type.AppWarning);
                MessageBox.Show("Unable to save. " + excp.Message, "Ubi Displays", MessageBoxButton.OK, MessageBoxImage.Warning);
                sConfigurationFile = "";
                bSettingsChosen = false;
            }
        }

        /// <summary>
        /// Handle the user pressing the save button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SaveClick(object sender, RoutedEventArgs e)
        {
            // If we have not chosen our settings, save as.
            if (!bSettingsChosen)
            {
                Button_SaveAsClick(null, null);
                return;
            }

            // Do the save.
            try
            {
                Save(bCalib, bSurfaces, bDisplays, sConfigurationFile);
                Log.Write("Saved to " + sConfigurationFile, "Application", Log.Type.AppInfo);
            }
            catch (Exception excp)
            {
                Log.Write("Unable to save. " + excp.Message, "Application", Log.Type.AppWarning);
                MessageBox.Show("Unable to save. " + excp.Message, "Ubi Displays", MessageBoxButton.OK, MessageBoxImage.Warning);
                sConfigurationFile = "";
                bSettingsChosen = false;
            }
        }

        /// <summary>
        /// Convert a string (space delimited) to a Point.
        /// </summary>
        /// <param name="sString">The string to convert.  e.g. '3.2 5.6'</param>
        /// <returns>A Point using the args from the string.  Exception on error.</returns>
        private static Point PointFromString(String sString)
        {
            // Check we have 3 spaces.
            var tCoords = sString.Split(' ');
            if (tCoords.Length == 2)
                return new Point(float.Parse(tCoords[0]), float.Parse(tCoords[1]));
            throw new Exception("Cannot parse Point from string '" + sString + "'.");
        }
        /// <summary>
        /// Convert a string (space delimited) to a Vector3.
        /// </summary>
        /// <param name="sString">The string to convert.  e.g. '3.2 5.6 -2.0'</param>
        /// <returns>A Vector3 using the args from the string.  Exception on error.</returns>
        private static SlimMath.Vector3 Vector3FromString(String sString)
        {
            // Check we have 3 spaces.
            var tCoords = sString.Split(' ');
            if (tCoords.Length == 3)
                return new SlimMath.Vector3(float.Parse(tCoords[0]), float.Parse(tCoords[1]), float.Parse(tCoords[2]));
            throw new Exception("Cannot parse Vector3 from string '" + sString + "'.");
        }

        /// <summary>
        /// Save the surfaces and displays to a file.
        /// This will automatically ask for a filename.
        /// </summary>
        /// <param name="bCalibration">Save calibration.</param>
        /// <param name="bSurfaces">Save surfaces.</param>
        /// <param name="bDisplays">Save displays.</param>
        /// <param name="sFilename">The name of the file to save to.</param>
        private void Save(bool bCalibration, bool bSurfaces, bool bDisplays, String sFilename)
        {
            // Log data.
            String sSaveCalibration = "", sSaveSurfaces = "", sSaveDisplays = "";

            // Create the XML element with a data node.
            XDocument pDocument = new XDocument();
            var pRoot = new XElement("data");
            pDocument.Add(pRoot);

            // Write calibration data.
            if (bCalibration)
            {
                #region Calibration Write
                // Create a node for storing the calibration.
                var xCalibration = new XElement("calibration");

                // Write monitor and kinect ID.
                xCalibration.Add(new XElement("monitor", SelectedScreen.DeviceName));
                xCalibration.Add(new XElement("kinect", KinectProcessor.Sensor.DeviceConnectionId));

                // Write the source homography.
                var xSource = new XElement("kinectimage",
                        new XElement("point", String.Format("{0} {1}", pHomography.Source[0].X, pHomography.Source[0].Y)),
                        new XElement("point", String.Format("{0} {1}", pHomography.Source[1].X, pHomography.Source[1].Y)),
                        new XElement("point", String.Format("{0} {1}", pHomography.Source[2].X, pHomography.Source[2].Y)),
                        new XElement("point", String.Format("{0} {1}", pHomography.Source[3].X, pHomography.Source[3].Y))
                        );
                xCalibration.Add(xSource);

                // And the destination homography.
                var xDestination = new XElement("projectedimage",
                        new XElement("point", String.Format("{0} {1}", pHomography.Destination[0].X, pHomography.Destination[0].Y)),
                        new XElement("point", String.Format("{0} {1}", pHomography.Destination[1].X, pHomography.Destination[1].Y)),
                        new XElement("point", String.Format("{0} {1}", pHomography.Destination[2].X, pHomography.Destination[2].Y)),
                        new XElement("point", String.Format("{0} {1}", pHomography.Destination[3].X, pHomography.Destination[3].Y))
                        );
                xCalibration.Add(xDestination);
                pRoot.Add(xCalibration);
                sSaveCalibration = "calibration ";
                #endregion
            }

            // Write surfaces.
            if (bSurfaces)
            {
                #region Surface Write
                int iSurfaces = 0;
                foreach (var pSurface in Authority.Surfaces)
                {
                    var xSurf = new XElement("surface");
                    xSurf.Add(new XElement("name", pSurface.Identifier));
                    xSurf.Add(new XElement("inject_multitouch", pSurface.AttemptMultiTouchInject));
                    xSurf.Add(new XElement("projector",
                        new XElement("point", String.Format("{0} {1}", pSurface.ProjectorSpace[0].X, pSurface.ProjectorSpace[0].Y)),
                        new XElement("point", String.Format("{0} {1}", pSurface.ProjectorSpace[1].X, pSurface.ProjectorSpace[1].Y)),
                        new XElement("point", String.Format("{0} {1}", pSurface.ProjectorSpace[2].X, pSurface.ProjectorSpace[2].Y)),
                        new XElement("point", String.Format("{0} {1}", pSurface.ProjectorSpace[3].X, pSurface.ProjectorSpace[3].Y))
                        ));
                    xSurf.Add(new XElement("sensorspace",
                        new XElement("point", String.Format("{0} {1} {2}", pSurface.SensorSpace[0].X, pSurface.SensorSpace[0].Y, pSurface.SensorSpace[0].Z)),
                        new XElement("point", String.Format("{0} {1} {2}", pSurface.SensorSpace[1].X, pSurface.SensorSpace[1].Y, pSurface.SensorSpace[1].Z)),
                        new XElement("point", String.Format("{0} {1} {2}", pSurface.SensorSpace[2].X, pSurface.SensorSpace[2].Y, pSurface.SensorSpace[2].Z)),
                        new XElement("point", String.Format("{0} {1} {2}", pSurface.SensorSpace[3].X, pSurface.SensorSpace[3].Y, pSurface.SensorSpace[3].Z))
                        ));
                    xSurf.Add(new XElement("image",
                        new XElement("point", String.Format("{0} {1}", pSurface.KinectSpace[0].X, pSurface.KinectSpace[0].Y)),
                        new XElement("point", String.Format("{0} {1}", pSurface.KinectSpace[1].X, pSurface.KinectSpace[1].Y)),
                        new XElement("point", String.Format("{0} {1}", pSurface.KinectSpace[2].X, pSurface.KinectSpace[2].Y)),
                        new XElement("point", String.Format("{0} {1}", pSurface.KinectSpace[3].X, pSurface.KinectSpace[3].Y))
                        ));
                    pRoot.Add(xSurf);
                    iSurfaces++;
                }
                sSaveSurfaces = iSurfaces + " surfaces ";
                #endregion
            }

            // Displays.
            if (bDisplays)
            {
                #region Displays Write
                int iDisplays = 0;
                foreach (var pDisplay in Authority.Displays)
                {
                    // Skip displays without a surface.
                    if (pDisplay.ActiveSurface == null)
                        continue;

                    // Write it into the XML.
                    var xDisplay = new XElement("display",
                        new XElement("resolution", String.Format("{0} {1}", pDisplay.RenderResolution.X, pDisplay.RenderResolution.Y)),
                        new XElement("loadinstruction", pDisplay.LoadInstruction),
                        new XElement("surfacename", pDisplay.ActiveSurface.Identifier));
                    pRoot.Add(xDisplay);
                    iDisplays++;
                }
                sSaveDisplays = iDisplays + " displays ";
                #endregion
            }

            // Write it into the file.
            pDocument.Save(sFilename);

            // Say it is saved.
            Log.Write(String.Format("Saved {0}{1}{2}to: {3}.", 
                sSaveCalibration, sSaveSurfaces, sSaveDisplays, sFilename), "Application", Log.Type.AppInfo);
        }

        /// <summary>
        /// Attempt to load calibration data from a file.
        /// </summary>
        /// <param name="bCalibration">Do we want to load hardware and calibration settings.</param>
        /// <param name="bSurfaces">Do we want to load surface data.</param>
        /// <param name="bDisplays">Do we want to load displays.</param>
        /// <param name="sFile">The file we want to load the data from.</param>
        private void Load(bool bCalibration, bool bSurfaces, bool bDisplays, String sFile)
        {
            // Open the XML file and parse out the data we are interested in.
            var pDocument = XDocument.Load(sFile);

            #region Load Calibration Data
            if (bCalibration)
            {
                // Parse the surfaces from the file.
                var dCalibration = (from item in pDocument.Root.Elements("calibration")
                                    select new
                                    {
                                        MonitorDevice = item.Element("monitor").Value,
                                        KinectDevice = item.Element("kinect").Value,

                                        KinectImage = (from pt in item.Element("kinectimage").Elements("point") select PointFromString(pt.Value)).ToArray(),
                                        ProjectedImage = (from pt in item.Element("projectedimage").Elements("point") select PointFromString(pt.Value)).ToArray(),
                                    }).FirstOrDefault();

                // Check we have calibration data.
                if (dCalibration != null)
                {
                    #region Load Screen
                    // If we already have a monitor selected.
                    if (SelectedScreen != null)
                    {
                        // If the device names are the same, all is well.
                        if (SelectedScreen.DeviceName != dCalibration.MonitorDevice)
                        {
                            // We have one selected already, log and bail.
                            Log.Write("Screen already selected.  Ignoring '" + dCalibration.MonitorDevice + "'.", "Application", Log.Type.AppWarning);
                        }

                        // See if we can complete step 1.
                        TryCompleteStep1(false);
                    }

                    // If we don't have a montior selected.
                    else
                    {
                        // Attempt to find a montor with a matching device name.
                        var lAvailable = Utilities.MonitorDetection.QueryDisplays();
                        foreach (var pMonitor in lAvailable)
                        {
                            if (pMonitor.DeviceName == dCalibration.MonitorDevice)
                            {
                                SelectedScreen = pMonitor;
                                TryCompleteStep1(false);
                                break;
                            }
                        }

                        // If we didn't manage to select a screen, make a note in the log.
                        if (SelectedScreen == null)
                        {
                            Log.Write("Could not find screen that matches the one in the file. You will need to choose another one.", "Application", Log.Type.AppWarning);
                        }
                    }
                    #endregion

                    #region Load Kinect
                    // If we already have a kinect selected.
                    if (SelectedKinect != null)
                    {
                        // If the device names are the same, all is well.
                        if (SelectedKinect.DeviceConnectionId != dCalibration.KinectDevice)
                        {
                            // We have one selected already, log and bail.
                            Log.Write("Kinect already selected.  Ignoring '" + dCalibration.KinectDevice + "'.", "Application", Log.Type.AppWarning);
                        }

                        // See if we can complete step 1.
                        TryCompleteStep1(false);
                    }

                    // If we don't have a kinect selected.
                    else
                    {
                        // Attempt to find a montor with a matching device name.
                        var lAvailable = KinectSensor.KinectSensors;
                        foreach (var pKinect in lAvailable)
                        {
                            if (pKinect.DeviceConnectionId == dCalibration.KinectDevice)
                            {
                                SelectedKinect = pKinect;
                                TryCompleteStep1(false);
                                break;
                            }
                        }

                        // If we didn't manage to select a screen, make a note in the log.
                        if (SelectedKinect == null)
                        {
                            Log.Write("Could not find kinect that matches the one in the file. You will need to choose another one.", "Application", Log.Type.AppWarning);
                        }
                    }
                    #endregion

                    #region Load Calibration Settings
                    // If we are NOT calibrated but are ready to be used.
                    if (bCalibrated == false && SelectedKinect != null && SelectedScreen != null)
                    {
                        Calibrate(dCalibration.ProjectedImage, dCalibration.KinectImage);
                    }

                    // If we ARE calibrated and are ready to be used.
                    else if (bCalibrated == true && SelectedKinect != null && SelectedScreen != null)
                    {
                        // Do we want to overwrite the existing calibration.
                        var hResult = MessageBox.Show("Do you want to overwrite current calibration with imported one?", "Ubi Displays", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (hResult == MessageBoxResult.Yes)
                        {
                            Calibrate(dCalibration.ProjectedImage, dCalibration.KinectImage);
                        }
                    }

                    // Otherwise we are not ready!
                    else
                    {
                        Log.Write("Cannot import calibration because hardware is not selected.", "Application", Log.Type.AppWarning);
                    }
                    #endregion
                }
            }
            #endregion

            // Only import these if we are calibrated.
            if (!bCalibrated && (bSurfaces || bDisplays))
            {
                Log.Write("Cannot import surfaces and displays.  Please calibrate and try again.", "Application", Log.Type.AppWarning);
                return;
            }

            #region Load Surfaces
            if (bSurfaces)
            {
                // Parse the surfaces from the file.
                var lSurfaces = from item in pDocument.Root.Elements("surface")
                                select new
                                {
                                    Identifier = item.Element("name").Value,
                                    InjectMT = (item.Element("inject_multitouch").Value.ToLower() == "true") ? true : false,

                                    Projector = (from pt in item.Element("projector").Elements("point") select PointFromString(pt.Value)).ToArray(),
                                    Sensor = (from pt in item.Element("sensorspace").Elements("point") select Vector3FromString(pt.Value)).ToArray(),
                                    KinectImage = (from pt in item.Element("image").Elements("point") select PointFromString(pt.Value)).ToArray(),
                                };

                // For each surface, register one with the authority.
                foreach (var dSurfaceData in lSurfaces)
                {
                    // Check the surface name is good.
                    if (dSurfaceData.Identifier == null || dSurfaceData.Identifier == "")
                    {
                        Log.Write("Cannot import surface.  Surface is missing a name.", "Application", Log.Type.AppWarning);
                        continue;
                    }

                    // If the name is already taken, bail.
                    if (Authority.FindSurface(dSurfaceData.Identifier) != null)
                    {
                        Log.Write("Cannot import surface '" + dSurfaceData.Identifier + "'.  Surface with the same name already exists.", "Application", Log.Type.AppWarning);
                        continue;
                    }

                    // Check we have valid data.
                    if ((dSurfaceData.Projector == null || dSurfaceData.Projector.Length != 4) ||
                        (dSurfaceData.Sensor == null || dSurfaceData.Sensor.Length != 4) ||
                        (dSurfaceData.KinectImage == null || dSurfaceData.KinectImage.Length != 4))
                    {
                        Log.Write("Cannot import surface '" + dSurfaceData.Identifier + "'.  It does not contain valid data.", "Application", Log.Type.AppWarning);
                        continue;
                    }

                    // Create the surface.
                    var pSurface = new Model.Surface(dSurfaceData.Identifier);
                    pSurface.AttemptMultiTouchInject = dSurfaceData.InjectMT;
                    pSurface.SetSpatialProperties(dSurfaceData.Projector, dSurfaceData.Sensor, dSurfaceData.KinectImage);
                    Authority.RegisterSurface(pSurface);
                }
            }
            #endregion

            #region Load Displays
            if (bDisplays)
            {
                // For each display in the surface file, attach it to the surface.
                var lDisplays = from item in pDocument.Root.Elements("display")
                                select new
                                {
                                    SurfaceName = item.Element("surfacename").Value,
                                    LoadInstruction = item.Element("loadinstruction").Value,
                                    Resolution = PointFromString(item.Element("resolution").Value),
                                };

                // Create the displays.
                foreach (var dDisplayData in lDisplays)
                {
                    // Find the surface to place it on.
                    var pSurface = Authority.FindSurface(dDisplayData.SurfaceName);
                    if (pSurface == null)
                    {
                        Log.Write("Cannot import display '" + dDisplayData.LoadInstruction + "'.  Could not find host surface '" + dDisplayData.SurfaceName + "'.", "Application", Log.Type.AppWarning);
                        continue;
                    }

                    // Create the display.
                    var pDisplay = new Display(dDisplayData.LoadInstruction, dDisplayData.Resolution);

                    // Disable debug mode on the surface.
                    pSurface.ShowDebug = false;

                    // Show the display.
                    Authority.ShowDisplay(pDisplay, pSurface);
                }
            }
            #endregion
        }


        #endregion



        /// <summary>
        /// Once the user has agreed to the non-commercial bit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_NonCommercialClick(object sender, RoutedEventArgs e)
        {
            // Remove the non-commercial overlay from the UI.
            _Wrapper.Children.Remove(_NoneCommercial);
        }

        /// <summary>
        /// Handle people clicking on the non-commercial hyperlinks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Hyperlink) != null)
            {
                System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri.ToString());
            }
        }

    }
}
