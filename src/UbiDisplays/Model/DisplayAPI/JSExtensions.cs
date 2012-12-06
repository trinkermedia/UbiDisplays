using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awesomium.Core;

namespace UbiDisplays.Model.DisplayAPI
{
    /// <summary>
    /// This class adds some helper methods to the common awesomium JSValue and JSObject operations.
    /// </summary>
    /// <remarks>This exists to simply make writing the API quicker.</remarks>
    public static class JSExtensions
    {
        /// <summary>
        /// Check if a value is contained by a JSObject.  If it is, the value is returned.  If it is not, a default is returned.
        /// </summary>
        /// <param name="pObj">The instance object.</param>
        /// <param name="sProperty">The property to check for.</param>
        /// <param name="kDefault">The defaut value to return if it does not exist.</param>
        /// <returns>The value, default or stored.</returns>
        public static String GetValueOrDefault(this JSObject pObj, String sProperty, String kDefault)
        {
            if (pObj.HasProperty(sProperty))
            {
                var pValue = pObj[sProperty];
                if (pValue.IsString)
                    return (String)pValue;
            }
            return kDefault;
        }

        /// <summary>
        /// Check if a value is contained by a JSObject.  If it is, the value is returned.  If it is not, a default is returned.
        /// </summary>
        /// <param name="pObj">The instance object.</param>
        /// <param name="sProperty">The property to check for.</param>
        /// <param name="kDefault">The defaut value to return if it does not exist.</param>
        /// <returns>The value, default or stored.</returns>
        public static double GetValueOrDefault(this JSObject pObj, String sProperty, double kDefault)
        {
            if (pObj.HasProperty(sProperty))
            {
                var pValue = pObj[sProperty];
                if (pValue.IsDouble)
                    return (double)pValue;
            }
            return kDefault;
        }

        /// <summary>
        /// Check if a value is contained by a JSObject.  If it is, the value is returned.  If it is not, a default is returned.
        /// </summary>
        /// <param name="pObj">The instance object.</param>
        /// <param name="sProperty">The property to check for.</param>
        /// <param name="kDefault">The defaut value to return if it does not exist.</param>
        /// <returns>The value, default or stored.</returns>
        public static float GetValueOrDefault(this JSObject pObj, String sProperty, float kDefault)
        {
            if (pObj.HasProperty(sProperty))
            {
                var pValue = pObj[sProperty];
                if (pValue.IsDouble)
                    return (float)((double)pValue);
            }
            return kDefault;
        }

        /// <summary>
        /// Check if a value is contained by a JSObject.  If it is, the value is returned.  If it is not, a default is returned.
        /// </summary>
        /// <param name="pObj">The instance object.</param>
        /// <param name="sProperty">The property to check for.</param>
        /// <param name="kDefault">The defaut value to return if it does not exist.</param>
        /// <returns>The value, default or stored.</returns>
        public static int GetValueOrDefault(this JSObject pObj, String sProperty, int kDefault)
        {
            if (pObj.HasProperty(sProperty))
            {
                var pValue = pObj[sProperty];
                if (pValue.IsInteger)
                    return (int)pValue;
            }
            return kDefault;
        }

        /// <summary>
        /// Check if a value is contained by a JSObject.  If it is, the value is returned.  If it is not, a default is returned.
        /// </summary>
        /// <param name="pObj">The instance object.</param>
        /// <param name="sProperty">The property to check for.</param>
        /// <param name="kDefault">The defaut value to return if it does not exist.</param>
        /// <returns>The value, default or stored.</returns>
        public static bool GetValueOrDefault(this JSObject pObj, String sProperty, bool kDefault)
        {
            if (pObj.HasProperty(sProperty))
            {
                var pValue = pObj[sProperty];
                if (pValue.IsBoolean)
                    return (bool)pValue;
            }
            return kDefault;
        }

        /// <summary>
        /// Check if a value is contained by a JSObject.  If it is, the value is returned.  If it is not, a default is returned.
        /// </summary>
        /// <param name="pObj">The instance object.</param>
        /// <param name="sProperty">The property to check for.</param>
        /// <param name="kDefault">The defaut value to return if it does not exist.</param>
        /// <returns>The value, default or stored.</returns>
        public static JSValue[] GetValueOrDefault(this JSObject pObj, String sProperty, JSValue[] kDefault)
        {
            if (pObj.HasProperty(sProperty))
            {
                var pValue = pObj[sProperty];
                if (pValue.IsArray)
                    return (JSValue[])pValue;
            }
            return kDefault;
        }

        /// <summary>
        /// Convert a SlimDX coordiante into a JSObject.
        /// </summary>
        /// <remarks>X,Y,Z becomes x,y,z</remarks>
        /// <param name="tCoordinate">The SlimMath Vector3 to convert.</param>
        /// <returns>A JSObject of the format { x : N, y : N, z : N } where N is the input coordinate.</returns>
        public static JSObject MakeCoordinate(SlimMath.Vector3 tCoordinate)
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
        /// <remarks>X,Y becomes x,y</remarks>
        /// <param name="tCoordinate">The Windows.Point coordinate to convert.</param>
        /// <returns>A JSObject of the format { x : N, y : N } where N is the input coordinate.</returns>
        public static JSObject MakeCoordinate(System.Windows.Point tCoordinate)
        {
            var pCoord = new JSObject();
            pCoord["x"] = tCoordinate.X;
            pCoord["y"] = tCoordinate.Y;
            return pCoord;
        }

        /// <summary>
        /// Convert a SlimDX coordiante into a JSObject.
        /// </summary>
        /// <remarks>X,Y,Z becomes x,y,z</remarks>
        /// <param name="tCoordinate">The SlimMath Vector3 to convert.</param>
        /// <returns>The instance JSObject with properties in the format { x : N, y : N, z : N } where N is the input coordinate.</returns>
        public static JSObject StoreCoordinate(this JSObject pCoord, SlimMath.Vector3 tCoordinate)
        {
            pCoord["x"] = tCoordinate.X;
            pCoord["y"] = tCoordinate.Y;
            pCoord["z"] = tCoordinate.Z;
            return pCoord;
        }

        /// <summary>
        /// Make a JSObject store the values from a System.Windows.Point.
        /// </summary>
        /// <remarks>X,Y becomes x,y</remarks>
        /// <param name="tCoordinate">The Windows.Point coordinate to convert.</param>
        /// <returns>The instance JSObject with properties in the format { x : N, y : N } where N is the input coordinate.</returns>
        public static JSObject StoreCoordinate(this JSObject pCoord, System.Windows.Point tCoordinate)
        {
            pCoord["x"] = tCoordinate.X;
            pCoord["y"] = tCoordinate.Y;
            return pCoord;
        }
    }  
}
