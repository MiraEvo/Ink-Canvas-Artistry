using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;

namespace Ink_Canvas.Helpers
{
    [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: required Win32/COM boundary; no managed alternative; owned by ComInteropHelper.")]
    [SuppressMessage("Reliability", "cs/unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: required Win32/COM boundary; no managed alternative; owned by ComInteropHelper.")]
    internal static partial class ComInteropHelper
    {
        [LibraryImport("ole32.dll", EntryPoint = "CLSIDFromProgIDEx", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int CLSIDFromProgIDEx(string progId, out Guid clsid);

        [LibraryImport("ole32.dll", EntryPoint = "CLSIDFromProgID", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int CLSIDFromProgID(string progId, out Guid clsid);

        [DllImport("ole32.dll", EntryPoint = "GetRunningObjectTable")]
        private static extern int NativeGetRunningObjectTable(int reserved, out IRunningObjectTable? runningObjectTable);

        [DllImport("ole32.dll", EntryPoint = "CreateBindCtx")]
        private static extern int NativeCreateBindCtx(int reserved, out IBindCtx? bindContext);

        // Source-generated COM marshalling does not currently cover this late-bound IUnknown shape well.
        [DllImport("oleaut32.dll", EntryPoint = "GetActiveObject")]
        private static extern int NativeGetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object? comObject);

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

            hResult = NativeGetActiveObject(ref clsid, IntPtr.Zero, out object? comObject);
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

        public static bool TryGetRunningObjectTable(out IRunningObjectTable? runningObjectTable)
        {
            int result = NativeGetRunningObjectTable(0, out runningObjectTable);
            return result == 0 && runningObjectTable != null;
        }

        public static bool TryCreateBindContext(out IBindCtx? bindContext)
        {
            int result = NativeCreateBindCtx(0, out bindContext);
            return result == 0 && bindContext != null;
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
            catch (ArgumentException)
            {
                Debug.WriteLine("ComInteropHelper | SafeRelease ignored ArgumentException.");
            }
            catch (InvalidComObjectException)
            {
                Debug.WriteLine("ComInteropHelper | SafeRelease ignored InvalidComObjectException.");
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
            catch (ArgumentException)
            {
                Debug.WriteLine("ComInteropHelper | SafeFinalRelease ignored ArgumentException.");
            }
            catch (InvalidComObjectException)
            {
                Debug.WriteLine("ComInteropHelper | SafeFinalRelease ignored InvalidComObjectException.");
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
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidComObjectException)
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
