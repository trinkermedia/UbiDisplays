using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Drawing = System.Drawing;


namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// This class contains helpers for adding the fancy glass effect to windows.
    /// </summary>
    public abstract class Glass
    {
        /// <summary>
        /// A structure which describes how far to bring in each margin.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;      // width of left border that retains its size
            public int cxRightWidth;     // width of right border that retains its size
            public int cyTopHeight;      // height of top border that retains its size
            public int cyBottomHeight;   // height of bottom border that retains its size
        };

        /// <summary>
        /// DwmExtendFrameIntoClientArea _udwm_dwmextendframeintoclientarea is the DWM function that extends the 
        /// frame into the client area.
        /// </summary>
        /// <param name="hwnd">The window handle of interest.</param>
        /// <param name="pMarInset">Used to tell the DWM how much extra the frame should be extended into the client area.</param>
        /// <returns></returns>
        [DllImport("DwmApi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        /// <summary>
        /// Extend the glass from the top of the window into the application space.
        /// </summary>
        /// <param name="pWindow">The window whos glass we want to extend.</param>
        /// <param name="iMargin">The amount to extend the glass by.</param>
        /// <param name="bTop">True to extend from the top down, false for buttom up.</param>
        public static void Extend(Window pWindow, int iTopMargin, int iBottomMargin)
        {
            try
            {
                // Obtain the window handle for WPF application.
                IntPtr mainWindowPtr = new WindowInteropHelper(pWindow).Handle;
                HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
                mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

                // Get System Dpi.
                Drawing.Graphics desktop = Drawing.Graphics.FromHwnd(mainWindowPtr);
                float DesktopDpiX = desktop.DpiX;
                float DesktopDpiY = desktop.DpiY;

                // Set Margins.
                // Extend glass frame into client area
                // Note that the default desktop Dpi is 96dpi. The  margins are adjusted for the system Dpi.
                // The default margin size for the window content is 5.
                MARGINS tMargins = new MARGINS();
                tMargins.cxLeftWidth    = Convert.ToInt32(5 * (DesktopDpiX / 96));
                tMargins.cxRightWidth   = Convert.ToInt32(5 * (DesktopDpiX / 96));
                tMargins.cyTopHeight    = Convert.ToInt32((5 + iTopMargin) * (DesktopDpiY / 96));
                tMargins.cyBottomHeight = Convert.ToInt32((5 + iBottomMargin) * (DesktopDpiY / 96));

                // Make the call to extend the frame.
                int hr = DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref tMargins);
                if (hr < 0)
                {
                    //DwmExtendFrameIntoClientArea Failed
                    pWindow.Background = Brushes.LightGray;
                }
            }

            // If not Vista, paint background light gray.
            catch (DllNotFoundException)
            {
                pWindow.Background = Brushes.LightGray;
            }
        }
    }
}
