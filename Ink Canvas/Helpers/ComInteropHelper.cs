using System;
using System.Runtime.InteropServices;

namespace Ink_Canvas.Helpers
{
    internal static class ComInteropHelper
    {
        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        private static extern int CLSIDFromProgIDEx(string progId, out Guid clsid);

        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        private static extern int CLSIDFromProgID(string progId, out Guid clsid);

        [DllImport("oleaut32.dll")]
        private static extern int GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object comObject);

        public static T GetActiveObject<T>(string progId) where T : class
        {
            int hResult = CLSIDFromProgIDEx(progId, out Guid clsid);
            if (hResult < 0)
            {
                hResult = CLSIDFromProgID(progId, out clsid);
            }

            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            hResult = GetActiveObject(ref clsid, IntPtr.Zero, out object comObject);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            return (T)comObject;
        }
    }
}
