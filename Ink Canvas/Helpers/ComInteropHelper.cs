using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Ink_Canvas.Helpers
{
    internal static partial class ComInteropHelper
    {
        [LibraryImport("ole32.dll", EntryPoint = "CLSIDFromProgIDEx", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int CLSIDFromProgIDEx(string progId, out Guid clsid);

        [LibraryImport("ole32.dll", EntryPoint = "CLSIDFromProgID", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int CLSIDFromProgID(string progId, out Guid clsid);

        [DllImport("oleaut32.dll")]
        private static extern int GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object comObject);

        public static T GetActiveObject<T>(string progId) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);

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

            if (comObject is not T typedObject)
            {
                throw new InvalidCastException($"The active COM object for '{progId}' is not assignable to {typeof(T).FullName}.");
            }

            return typedObject;
        }

        public static void SafeRelease(object? comObject)
        {
            if (comObject == null || !Marshal.IsComObject(comObject))
            {
                return;
            }

            try
            {
                Marshal.ReleaseComObject(comObject);
            }
            catch
            {
            }
        }

        public static void SafeFinalRelease(object? comObject)
        {
            if (comObject == null || !Marshal.IsComObject(comObject))
            {
                return;
            }

            try
            {
                Marshal.FinalReleaseComObject(comObject);
            }
            catch
            {
            }
        }

        public static bool AreSameComObjects(object? left, object? right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (ReferenceEquals(left, right))
            {
                return true;
            }

            IntPtr leftUnknown = IntPtr.Zero;
            IntPtr rightUnknown = IntPtr.Zero;
            try
            {
                leftUnknown = Marshal.GetIUnknownForObject(left);
                rightUnknown = Marshal.GetIUnknownForObject(right);
                return leftUnknown == rightUnknown;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (leftUnknown != IntPtr.Zero)
                {
                    Marshal.Release(leftUnknown);
                }

                if (rightUnknown != IntPtr.Zero)
                {
                    Marshal.Release(rightUnknown);
                }
            }
        }
    }
}
