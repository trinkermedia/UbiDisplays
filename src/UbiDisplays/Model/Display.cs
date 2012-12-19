using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;

using Awesomium.Core;
using Awesomium.Windows.Controls;

namespace UbiDisplays.Model
{
    /// <summary>
    /// The Display class represents (web) content which can be shown on a surface.
    /// </summary>
    /// <remarks>It also manages the rendering of that content (its creation, deletion, etc) and the interface to the API methods.</remarks>
    [Serializable()]
    public class Display : ResourceOwner, IResource
    {
        #region Properties
        /// <summary>
        /// The instruction which loads or unloads the view web link.
        /// </summary>
        public String LoadInstruction { get; set; }

        /// <summary>
        /// This is the current title (i.e. the html 'title' element).
        /// </summary>
        //[NonSerialized]
        public String Title { get; internal set; }

        /// <summary>
        /// A reference to the active surface this view is bound too.
        /// </summary>
        //[NonSerialized]
        public Surface ActiveSurface { get; private set; }

        /// <summary>
        /// Return a reference to the web view which is currently rendering this display.
        /// </summary>
        //[NonSerialized]
        protected WebControl ActiveControl { get; private set; }

        /// <summary>
        /// Get the visual used for rendering.
        /// </summary>
        //[NonSerialized]
        public UIElement Visual
        {
            get
            {
                if (this.ActiveControl == null)
                    throw new Exception("Cannot access display visual because it is not yet created.  Did you call Surface_BindToSurface?");
                return this.ActiveControl;
            }
        }

        /// <summary>
        /// Get or set the render resolution for this display.
        /// </summary>
        //[NonSerialized]
        public Point RenderResolution
        {
            get
            {
                return _RenderResolution;
            }
            set
            {
                // Sanity check.
                if (value.X < 2 || value.Y < 2)
                    throw new Exception("Bad render resolution.  Must be greater than 2x2.");

                // Store the value.
                _RenderResolution = value;

                // Push an update to the surface if active.  If not it will be done when next created.
                if (ActiveControl != null)
                {
                    ActiveControl.Width = value.X;
                    ActiveControl.Height = value.Y;
                }
                if (ActiveSurface != null)
                {
                    ActiveSurface.Display_SetVisualRenderSize(new Size(value.X, value.Y));
                }
            }
        }
        /// <summary>
        /// The internal render resolution.
        /// </summary>
        private Point _RenderResolution = new Point(800, 600);
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new display.
        /// </summary>
        /// <param name="sLoadInstruction">The load instruction. e.g. http://google.com</param>
        public Display(String sLoadInstruction)
        {
            // The load instruction.
            LoadInstruction = sLoadInstruction;

            // The render resolution.
            RenderResolution = new Point(800, 600);
        }

        /// <summary>
        /// Create a new display.
        /// </summary>
        /// <param name="sLoadInstruction">The load instruction. e.g. http://google.com</param>
        /// <param name="tRenderResolution">The render resolution for this display.</param>
        public Display(String sLoadInstruction, Point tRenderResolution)
            : this(sLoadInstruction)
        {
            // Base class.
            this.RenderResolution = tRenderResolution;
        }
        #endregion

        #region Surface Control Functions
        /// <summary>
        /// Called by a surface to signal that this display has attached to it.
        /// </summary>
        /// <remarks>Here it should create the visual to be shown (if not already existing) and place it on the surface by calling: pSurface.Display_SetVisual(visual).</remarks>
        /// <param name="pSurface">The surface which this display is about to be shown on.</param>
        public void Surface_BindToSurface(Surface pSurface)
        {
            // Error checking.
            if (pSurface == null)
                throw new ArgumentNullException("Cannot bind to a null surface.");

            // If we already have an active surface.
            if (ActiveSurface != null)
                throw new Exception("Display is already bound to a surface.");

            // Store the surface reference.
            ActiveSurface = pSurface;

            // Create the visual to go on the surface.
            ActiveControl = CreateRenderable();
            ActiveSurface.Display_SetVisual(ActiveControl);
            ActiveSurface.Display_SetVisualRenderSize(new Size(ActiveControl.Width, ActiveControl.Height));

            // Tell the JS that we have been attached to a surface (i.e. its properties have changed).
            //this.SignalSurfacePropertiesChanged();

            // Bind to spatial update events.
            pSurface.OnSurfacePropertiesUpdated += Surface_OnSpatialPropertiesUpdated;
        }

        /// <summary>
        /// Called by a surface to signal that this display should detach from it.
        /// </summary>
        /// <remarks>Here it could destroy the visual to be shown and remove it from the surface by calling: pSurface.Display_SetVisual(null).</remarks>
        /// <param name="pSurface"></param>
        public void Surface_UnbindFromSurface(Surface pSurface)
        {
            // Error checking.
            if (pSurface == null)
                throw new ArgumentNullException("Cannot unbind from a null surface.");

            // Check the surface is our active one.
            if (pSurface != ActiveSurface)
                throw new Exception("Surface to deatch from and stored surface do not match.");

            // Unbind from spatial update events.
            ActiveSurface.OnSurfacePropertiesUpdated -= Surface_OnSpatialPropertiesUpdated;

            // Remove the renderable from the surface.
            ActiveSurface.Display_SetVisual(null);
            ActiveSurface.Display_ResetVisualRenderSize();

            // Destroy the renderable.
            //  n.b. With a few small changes (i.e. not calling this) we could preserve the WebView while not attached to a surface.
            //       this would be good for moving displays without losing state.
            ActiveControl.Dispose();
            ActiveControl = null;

            // Remove any resources we have attached (i.e. spatial queries).
            this.DeleteResources();

            // Remove the reference to the active surface.
            ActiveSurface = null;
        }

        /// <summary>
        /// Called by the active surface when its spatial properties have been updated.
        /// </summary>
        /// <remarks>This is because we subscribe to the event when we bind/unbind.</remarks>
        private void Surface_OnSpatialPropertiesUpdated(Surface pSurface)
        {
            this.SignalSurfacePropertiesChanged();
        }
        #endregion

        #region API Helpers
        /// <summary>
        /// Helper function to bind the API functions to a web control.
        /// </summary>
        /// <param name="pControl">The web control to bind the functions too.</param>
        private void BindAPIFunctions(WebControl pControl)
        {
            // Create and acquire a global Javascript object - this will persist for the lifetime of the web-view.
            using (JSObject pAuthority = pControl.CreateGlobalJavascriptObject(Authority.APIObject_Authority))
            {
                // Handle requests for Authority.request.
                pAuthority.Bind(Authority.APIObject_AuthorityRequest, false, (s, e) =>
                {
                    // Process the arguments.
                    var sHandler = (e.Arguments.Length > 0 && e.Arguments[0].IsString) ? ((String)e.Arguments[0]) : null;
                    var dObject = (e.Arguments.Length > 1 && e.Arguments[1].IsObject) ? e.Arguments[1] : new JSValue();

                    // Return the result.
                    var bResult = Authority.ProcessRequest(this, this.ActiveSurface, sHandler, dObject);
                    //e.Result = new JSValue(bResult);
                });

                // Handle requests for Authority.request.
                pAuthority.Bind(Authority.APIObject_AuthorityCall, false, (s, e) =>
                {
                    // Process the arguments.
                    var sTargetSurface = (e.Arguments.Length > 0 && e.Arguments[0].IsString) ? ((String)e.Arguments[0]) : null;
                    var sTargetFunction = (e.Arguments.Length > 1 && e.Arguments[1].IsString) ? ((String)e.Arguments[1]) : null;
                    //var dObject = (e.Arguments.Length > 2 && e.Arguments[2].IsObject) ? e.Arguments[2] : new JSValue();

                    List<JSValue> lArgs = new List<JSValue>();
                    for (int iArg = 2; iArg < e.Arguments.Length; ++iArg)
                    {
                        lArgs.Add(e.Arguments[iArg]);
                    }

                    // Return the result.
                    var bResult = Authority.ProcessRMICall(this, this.ActiveSurface, sTargetSurface, sTargetFunction, lArgs.ToArray());
                    //e.Result = new JSValue(bResult);
                });

                // Handle requests for Authority.request.
                pAuthority.Bind(Authority.APIObject_AuthorityLog, false, (s, e) =>
                {
                    // Process the arguments.
                    StringBuilder sOut = new StringBuilder();
                    for (int iArg = 0; iArg < e.Arguments.Length; ++iArg)
                    {
                        sOut.Append(ToJSON(e.Arguments[iArg]));

                        if (iArg < (e.Arguments.Length-1))
                            sOut.Append(" --- ");   
                    }

                    // Return the result.
                    Log.Write("JS Message: " + sOut.ToString(), this.ToString(), Log.Type.DisplayInfo);
                });
            }

            // Create a surface object.
            using (JSObject pSurface = pControl.CreateGlobalJavascriptObject(Authority.APIObject_Surface))
            {
                pSurface["Name"] = new JSValue(ActiveSurface.Identifier);
                pSurface["Width"] = new JSValue(ActiveSurface.Width);
                pSurface["Height"] = new JSValue(ActiveSurface.Height);
                pSurface["AspectRatio"] = new JSValue(ActiveSurface.AspectRatio);
                pSurface["Angle"] = new JSValue(ActiveSurface.Angle);
            }

            // Signal that the properties have changed too.
            //SignalSurfacePropertiesChanged();
        }

        /// <summary>
        /// Tell the display logic (Javascript) that there has been an update to the surface.
        /// </summary>
        public void SignalSurfacePropertiesChanged()
        {
            // If we have no surface or web control, bail.
            if (ActiveSurface == null || ActiveControl == null)
                return;

            // If the web control is not live, skip.
            if (!ActiveControl.IsProcessCreated)
                return;

            /*
            // ASYNC HACK!
            var pSurfaceObject = new JSObject();
            pSurfaceObject["Name"] = new JSValue(ActiveSurface.Identifier);
            pSurfaceObject["Width"] = new JSValue(ActiveSurface.Width);
            pSurfaceObject["Height"] = new JSValue(ActiveSurface.Height);
            pSurfaceObject["AspectRatio"] = new JSValue(ActiveSurface.AspectRatio);
            pSurfaceObject["Angle"] = new JSValue(ActiveSurface.Angle);
            
            ActiveControl.ExecuteJavascript("window.Surface = "+ToJSON(pSurfaceObject)+";");
            //Log.Write("SignalSurfacePropertiesChanged", this.ToString(), Log.Type.AppError);
            */

            // Create a surface object.
            using (JSObject pSurface = ActiveControl.CreateGlobalJavascriptObject(Authority.APIObject_Surface))
            {
                // Say if we couldn't create the object.
                if (pSurface == null)
                {
                    Log.Write("Error setting surface properties.  Should be fixed in the next version.", this.ToString(), Log.Type.AppError);
                }

                pSurface["Name"] = new JSValue(ActiveSurface.Identifier);
                pSurface["Width"] = new JSValue(ActiveSurface.Width);
                pSurface["Height"] = new JSValue(ActiveSurface.Height);
                pSurface["AspectRatio"] = new JSValue(ActiveSurface.AspectRatio);
                pSurface["Angle"] = new JSValue(ActiveSurface.Angle);
            }

            // Call a method to say we have updated the surface properties.
            AsyncCallGlobalFunction("Surface_PropertiesChanged");
        }

        /// <summary>
        /// Asynchronously invoke a Javascript method on this display.
        /// </summary>
        /// <param name="sFunction">The name of the global function to call in the JS.</param>
        /// <param name="tArguments">A list of parameters to pass.</param>
        public void AsyncCallGlobalFunction(String sFunction, params JSValue[] tArguments)
        {
            // If we do not have an active web control.
            if (ActiveControl == null)
            {
                Log.Write("Cannot call method on display (" + this.ToString() + ") which does not have a web visual.", Authority.AUTHORITY_LOG_SOURCE, Log.Type.AppError);
                return;
            }

            // FIXME ASAP
            // THIS IS VERY SLOW BECAUSE IT USES SYNC COMMUNICATION TO GET THE OBJECT TO CALL
            // BUT APPARENTLY THIS IS THE ONLY WAY TO DO IT
            // http://forums.awesomium.com/viewtopic.php?f=4&t=1167&p=1470&hilit=CallJavascriptFunction#p1470
            // THE WAY I AM GOING TO SOLVE THIS FOR NOW IS TO CONVERT ALL THE ARGS TO A JS STRING AND EXECUTE IT.

            // FIXME: Make me properly Async.
            // TODO: Test my little converter function.. its probably crap!
            StringBuilder pString = new StringBuilder();
            if (tArguments != null)
            {
                for (int i = 0, n = tArguments.Length; i < n; ++i)
                {
                    pString.Append(ToJSON(tArguments[i]));
                    if (i < (n - 1))
                        pString.Append(",");
                }
            }
            var s = pString.ToString();

            // Push it to the dispatcher (we need to be in the calling thread).
            ActiveControl.Dispatcher.BeginInvoke((Action)delegate() 
                {
                    try
                    {
                        if (ActiveControl == null || !ActiveControl.IsProcessCreated)
                            return;

                        var sJavascript = @"if ('" + sFunction + @"' in window) {" + sFunction + @"(" + s + @"); }
else {  }"; // Authority.log('" + sFunction + @" function not found.');
                        //Console.WriteLine(sJavascript);
                        ActiveControl.ExecuteJavascript(sJavascript);
                    }
                    catch (Exception e)
                    {
                        // Just drop exceptions.. icky..
                    }
                });
        }
        #endregion

        #region Control Visual Creation and Managment
        /// <summary>
        /// A reference to our options which we use to create all webviews.
        /// </summary>
        private static WebSession pWebSession = null;

        /// <summary>
        /// Create a new visual to render.
        /// </summary>
        /// <returns></returns>
        private WebControl CreateRenderable()
        {
            // Get the web control.
            var pControl = new WebControl();

            // Create a web-session with our settings.
            if (pWebSession == null)
            {
                pWebSession = WebCore.CreateWebSession(new WebPreferences()
                {
                    EnableGPUAcceleration = Properties.Settings.Default.EnableGPUAcceleration,
                    Databases = Properties.Settings.Default.EnableWebDatabases,
                    WebGL = Properties.Settings.Default.EnableWebGL,
                    WebSecurity = Properties.Settings.Default.EnableWebSecurity,
                    FileAccessFromFileURL = Properties.Settings.Default.EnableWebFileAccessFromFileURL,
                    Plugins = true,
                });
            }
            pControl.WebSession = pWebSession;

            // Set the render dimensions.
            pControl.Width = this.RenderResolution.X;
            pControl.Height = this.RenderResolution.Y;

            // Hide the surface while we load.
            if (ActiveSurface != null)
                ActiveSurface.ContentOpacity = 0.01;

            // When the process has been createad, bind the global JS objects.
            // http://awesomium.com/docs/1_7_rc3/sharp_api/html/M_Awesomium_Core_IWebView_CreateGlobalJavascriptObject.htm
            pControl.ProcessCreated += (object sender, EventArgs e) =>
            {
                // CALLING window.Surface = {} in JS here has no effect.

                // Bind all the Authority.request and Authority.call methods.
                Log.Write("Display Process Created (1)", this.ToString(), Log.Type.AppError);
                BindAPIFunctions(pControl);
            };

            UrlEventHandler p = null;
            p = new UrlEventHandler((object senderOuter, UrlEventArgs eOuter) =>
                {
                    // Unbind this handler.
                    pControl.DocumentReady -= p;

                    // Force a re-load so the $(document).ready will have access to our Authority.request etc.
                    pControl.Reload(false);

                    // CALLING window.Surface = {} in JS here has no effect.
                    //SignalSurfacePropertiesChanged();
                    Log.Write("Display DocReady (1)", this.ToString(), Log.Type.AppError);
                    

                    // Bind the other events.
                    #region Events
                    // Handle navigating away from the URL in the load instruction.
                    pControl.AddressChanged += (object sender, UrlEventArgs e) =>
                    {
                        // Rebind the API methods?
                        Log.Write("Display has changed web address. " + e.Url, this.ToString(), Log.Type.DisplayInfo);
                    };

                    // Handle console messages.
                    pControl.TitleChanged += (object sender, TitleChangedEventArgs e) =>
                    {
                        this.Title = e.Title;
                    };

                    // Document ready.. do we beat JQuery?
                    pControl.DocumentReady += (object sender, UrlEventArgs e) =>
                    {
                        // Show the surface now we are loaded.
                        if (ActiveSurface != null)
                            ActiveSurface.ContentOpacity = 1.0;

                        // CALLING window.Surface = {} in JS here does not work quick enough.
                        Log.Write("Display DocReady (2)", this.ToString(), Log.Type.AppError);
                        SignalSurfacePropertiesChanged();

                        // EXPERIMENTAL - this sort of works.. depends on how the page detects touch events..
                        // SEE: Nice example: http://paulirish.com/demo/multi
                        // SEE: Nicer example: Bing maps!
                        // Try to inject multi-touch into the page.
                        if (ActiveSurface.AttemptMultiTouchInject)
                        {
                            pControl.ExecuteJavascript(Properties.Resources.MultiTouchInject);   
                        }
                        
                    };

                    // Handle changes in responsiveness.
                    pControl.ResponsiveChanged += (object sender, ResponsiveChangedEventArgs e) =>
                    {
                        // If it is not responsive, log the problem.
                        if (!e.IsResponsive)
                        {
                            Log.Write("Display is not responsive.  Try reloading.", this.ToString(), Log.Type.DisplayError);
                            //this.Reload(false, true);
                        }
                        else
                        {
                            Log.Write("Ready", this.ToString(), Log.Type.DisplayError);
                        }
                    };

                    // Handle crashes by reloading.
                    pControl.Crashed += (object sender, CrashedEventArgs e) =>
                    {
                        // Log the crash.
                        Log.Write("Display crashed - forcing reload. Termination Status = " + e.Status.ToString(), this.LoadInstruction, Log.Type.DisplayError);

                        // Force a hard-reload.  This will remove then re-create the entire web control.
                        this.Reload(true);
                    };
                    /*
                    // Handle javascript updates.
                    pControl.JSConsoleMessageAdded += (object sender, Awesomium.Core.JSConsoleMessageEventArgs e) =>
                    {
                        Log.Write("JS line " + e.LineNumber + ": " + e.Message, pActiveDisplay.ToString(), Log.Type.DisplayInfo);
                    };
            
                    // Handle pop-up messages (like alert).
                    pControl.ShowPopupMenu += (object sender, PopupMenuEventArgs e) =>
                    {
                    
                    };
                    */
                    #endregion
                });
            pControl.DocumentReady += p;

            // Set the load instruction.
            //   n.b. we have to then re-load it later to get access to our value
            pControl.Source = new Uri(this.LoadInstruction);

            // Return the created control.
            return pControl;
        }

        /// <summary>
        /// Return a string based representation of this display.
        /// </summary>
        /// <returns>A string representation of this display.  This is just the load instruction.</returns>
        public override string ToString()
        {
            return this.LoadInstruction;
        }

        /// <summary>
        /// Reload the display and its content.  Reloading will also delete any other resources we have.
        /// </summary>
        /// <param name="bHard">True if we want to remove and then re-create the webcontrol.  False if we just want to do a web-refresh.</param>
        /// <param name="bIgnoreCache">If this is just a web-refresh, do we want to ignore the cache.</param>
        public void Reload(bool bHard, bool bIgnoreCache = true)
        {
            // If it is active.
            if (ActiveControl == null)
                return;

            // If it is on a surface.
            if (ActiveSurface == null)
                return;

            // If it is a hard reset.
            if (bHard)
            {
                // Remove the control and re-add it.
                ActiveSurface.Display_SetVisual(null);
                ActiveControl.Dispose();
                ActiveControl = null;
                
                this.DeleteResources();
                ActiveControl = CreateRenderable();
                ActiveSurface.Display_SetVisual(ActiveControl);
            }
            else
            {
                this.DeleteResources();
                ActiveControl.Reload(bIgnoreCache);
            }
        }
        #endregion

        #region Coordinate Helpers
        /// <summary>
        /// Convert a SlimDX coordiante into a JSObject.
        /// </summary>
        /// <param name="tCoordinate">The SlimMath Vector3 to convert.</param>
        /// <returns>A JSObject of the format { x : N, y : N, z : N } where N is the input coordinate.</returns>
        private JSObject MakeCoordinate(SlimMath.Vector3 tCoordinate)
        {
            var pCoord = new JSObject();
            pCoord["x"] = tCoordinate.X;
            pCoord["y"] = tCoordinate.Y;
            pCoord["z"] = tCoordinate.Z;
            return pCoord;
        }

        /// <summary>
        /// Convert a Windows.Point into a JSObject.
        /// </summary>
        /// <param name="tCoordinate">The Windows.Point coordinate to convert.</param>
        /// <returns>A JSObject of the format { x : N, y : N } where N is the input coordinate.</returns>
        private JSObject MakeCoordinate(Point tCoordinate)
        {
            var pCoord = new JSObject();
            pCoord["x"] = tCoordinate.X;
            pCoord["y"] = tCoordinate.Y;
            return pCoord;
        }
        #endregion

        #region Deletion Pattern
        /// <summary>
        /// A flag which says if this display has been deleted or not.
        /// </summary>
        [NonSerialized]
        private bool bDeleted = false;

        /// <summary>
        /// An event which is raised when this is deleted.
        /// </summary>
        public event Action<IResource> OnDeleted;

        /// <summary>
        /// Call this to tell the display to delete.  This works by calling Authority.DeleteDisplay.
        /// </summary>
        public void Delete()
        {
            // Try to remove us from the authority.  This should take care of most things.
            try
            {
                Authority.DeleteDisplay(this);
            }
            catch (Exception e)
            {
                Log.Write("Error deleting display '" + this.ToString() + "'. " + e.Message, Authority.AUTHORITY_LOG_SOURCE, Log.Type.AppWarning);
            }
        }

        /// <summary>
        /// Determine if this surface has been deleted.
        /// </summary>
        public bool IsDeleted()
        {
            return bDeleted;
        }

        /// <summary>
        /// Called by the authority to signal that this surface has been deleted.
        /// </summary>
        internal void Authority_Delete()
        {
            // If we are still attached to a surface, throw an error.
            if (ActiveSurface != null)
                throw new Exception("Cannot delete display while still attached to surface.");

            // Remove the web control.
            if (ActiveControl != null)
            {
                ActiveControl.Dispose();
                ActiveControl = null;
            }
            
            // Free up any other resources we may have created.
            this.DeleteResources();

            // And'were done - set the deleted flag.
            bDeleted = true;

            // Say we are deleted.
            if (OnDeleted != null)
                OnDeleted(this);
        }
        #endregion

        #region Helpers for Awesomium ASYNC function use.
        /// <summary>
        /// Convert a JSValue into a string so we can execute it async.
        /// </summary>
        /// <param name="pValue">The value we want to convert.</param>
        /// <returns>A string representation of our value.</returns>
        public static String ToJSON(JSValue pValue)
        {
            // Parse out the primitive (ish) types.
            if (pValue.IsString)
                return EncodeJsString(pValue.ToString());

            if (pValue.IsDouble)
                return ((double)pValue).ToString(System.Globalization.CultureInfo.InvariantCulture); // fixes it not working on german systems

            if (pValue.IsInteger || pValue.IsNull || pValue.IsNumber || pValue.IsUndefined)
                return pValue.ToString();

            if (pValue.IsBoolean)
                return ((bool)pValue) ? "true" : "false";

            // Parse out the arrays.
            if (pValue.IsArray)
            {
                var pOut = new StringBuilder();
                pOut.Append("[");
                var tArray = (JSValue[])pValue;
                for (int i = 0, n = tArray.Length; i<n; ++i)
                {
                    pOut.Append(ToJSON(tArray[i]));
                    if (i < (n-1))
                        pOut.Append(", ");
                }
                pOut.Append("]");
                return pOut.ToString();
            }

            // Parse out the objects.
            if (pValue.IsObject)
            {
                var pOut = new StringBuilder();
                pOut.Append("{");
                var pObject = (JSObject)pValue;
                var tPropertyNames = pObject.GetPropertyNames();
                for (int i = 0, n = tPropertyNames.Length; i < n; ++i)
                {
                    pOut.Append(EncodeJsString(tPropertyNames[i]));
                    pOut.Append(":");
                    pOut.Append(ToJSON(pObject[tPropertyNames[i]]));
                    if (i < (n - 1))
                        pOut.Append(", ");
                }
                pOut.Append("}");
                return pOut.ToString();
            }

            // Unrecognised type.
            throw new ArgumentException("Unrecognised JSValue type.");
        }

        /// <summary>
        /// Encodes a string to be represented as a string literal. The format
        /// is essentially a JSON string.
        /// 
        /// The string returned includes outer quotes 
        /// Example Output: "Hello \"Rick\"!\r\nRock on"
        /// </summary>
        /// <remarks>Found here: http://stackoverflow.com/questions/806944/escape-quote-in-c-sharp-for-javascript-consumption </remarks>
        /// <param name="s">The string to encode.</param>
        /// <returns>The encoded string.</returns>
        private static string EncodeJsString(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        int i = (int)c;
                        if (i < 32 || i > 127)
                        {
                            sb.AppendFormat("\\u{0:X04}", i);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append("\"");

            return sb.ToString();
        }
        #endregion
    }
}


/* // NOTE: THIS NEEDS TO BE ASYNC!
// Create and acquire a global Javascript object - this will persist for the lifetime of the web-view.
using (JSObject pSurfaceObject = ActiveControl.CreateGlobalJavascriptObject(Authority.APIObject_Surface))
{
    // Handle requests for Authority.request.
    pSurfaceObject["Name"] = new JSValue(ActiveSurface.Identifier);
    pSurfaceObject["Width"] = new JSValue(ActiveSurface.Width);
    pSurfaceObject["Height"] = new JSValue(ActiveSurface.Height);
    pSurfaceObject["AspectRatio"] = new JSValue(ActiveSurface.AspectRatio);
    pSurfaceObject["Angle"] = new JSValue(ActiveSurface.Angle);

    // Add world coordinates.
    var pWorld = new JSObject();
    pWorld["topleft"]       = MakeCoordinate(ActiveSurface.SensorSpace[Surface.TOPLEFT_INDEX]);
    pWorld["topright"]      = MakeCoordinate(ActiveSurface.SensorSpace[Surface.TOPRIGHT_INDEX]);
    pWorld["bottomleft"]    = MakeCoordinate(ActiveSurface.SensorSpace[Surface.BOTTOMLEFT_INDEX]);
    pWorld["bottomright"]   = MakeCoordinate(ActiveSurface.SensorSpace[Surface.BOTTOMRIGHT_INDEX]);
    pWorld["normal"]        = MakeCoordinate(ActiveSurface.Plane.Normal);
    pSurfaceObject["World"] = pWorld;

    // Add kinect coordinates.
    var pKinect = new JSObject();
    pKinect["topleft"]       = MakeCoordinate(ActiveSurface.KinectSpace[Surface.TOPLEFT_INDEX]);
    pKinect["topright"]      = MakeCoordinate(ActiveSurface.KinectSpace[Surface.TOPRIGHT_INDEX]);
    pKinect["bottomleft"]    = MakeCoordinate(ActiveSurface.KinectSpace[Surface.BOTTOMLEFT_INDEX]);
    pKinect["bottomright"]   = MakeCoordinate(ActiveSurface.KinectSpace[Surface.BOTTOMRIGHT_INDEX]);
    pKinect["width"]         = 320;
    pKinect["height"]        = 240;
    pSurfaceObject["Kinect"] = pKinect;
}
*/

/*
// Do the processing in another thread.
Parallel.Invoke(() =>
{
    // Get the function.
    var pJSValue = ActiveControl.ExecuteJavascriptWithResult(sFunction);
    if (pJSValue.IsObject)
    {
        // Repack the arguments.
        JSValue[] tArgs = new JSValue[tArguments.Length + 1];
        Array.Copy(tArguments, 0, tArgs, 1, tArguments.Length);
        tArgs[0] = pJSValue;

        // Invoke the function.
        var pObject = (JSObject)pJSValue;
        pObject.Invoke("call", pObject, tArguments);
    }
});

            
// Do the processing in another thread.
BackgroundWorker pWorker = new BackgroundWorker();
pWorker.DoWork += (object pSender, DoWorkEventArgs eWork) =>
{
    // Get the function.
    var pJSValue = ActiveControl.ExecuteJavascriptWithResult(sFunction);
    if (pJSValue.IsObject)
    {
        // Repack the arguments.
        JSValue[] tArgs = new JSValue[tArguments.Length + 1];
        Array.Copy(tArguments, 0, tArgs, 1, tArguments.Length);
        tArgs[0] = pJSValue;

        // Invoke the function.
        var pObject = (JSObject)pJSValue;
        pObject.Invoke("call", pObject, tArguments);
    }
};
//pWorker.RunWorkerCompleted += (object pSender, RunWorkerCompletedEventArgs eWork) =>
//{
//};
// Perform the processing.
pWorker.RunWorkerAsync();
*/