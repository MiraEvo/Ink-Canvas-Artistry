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

        [LibraryImport("ole32.dll", EntryPoint = "GetRunningObjectTable")]
        private static partial int NativeGetRunningObjectTable(int reserved, out IntPtr runningObjectTable);

        [LibraryImport("ole32.dll", EntryPoint = "CreateBindCtx")]
        private static partial int NativeCreateBindCtx(int reserved, out IntPtr bindContext);

        [LibraryImport("oleaut32.dll", EntryPoint = "GetActiveObject")]
        private static partial int NativeGetActiveObject(ref Guid rclsid, IntPtr reserved, out IntPtr comObject);

        [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: COM active-object lookup requires oleaut32/ole32 interop; no managed equivalent.")]
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

            hResult = NativeGetActiveObject(ref clsid, IntPtr.Zero, out IntPtr comObjectPointer);
            if (hResult < 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            object? comObject = comObjectPointer != IntPtr.Zero ? Marshal.GetObjectForIUnknown(comObjectPointer) : null;
            if (comObjectPointer != IntPtr.Zero)
            {
                Marshal.Release(comObjectPointer);
            }

            if (comObject is not T typedObject)
            {
                throw new InvalidCastException($"The active COM object for '{progId}' is not assignable to {typeof(T).FullName}.");
            }

            return typedObject;
        }

        [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: ROT access requires COM interop; no managed equivalent.")]
        public static bool TryGetRunningObjectTable(out IRunningObjectTable? runningObjectTable)
        {
            int result = NativeGetRunningObjectTable(0, out IntPtr runningObjectTablePointer);
            if (result != 0 || runningObjectTablePointer == IntPtr.Zero)
            {
                runningObjectTable = null;
                return false;
            }

            try
            {
                runningObjectTable = Marshal.GetObjectForIUnknown(runningObjectTablePointer) as IRunningObjectTable;
                return runningObjectTable != null;
            }
            finally
            {
                Marshal.Release(runningObjectTablePointer);
            }
        }

        [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: COM bind-context creation requires ole32 interop; no managed equivalent.")]
        public static bool TryCreateBindContext(out IBindCtx? bindContext)
        {
            int result = NativeCreateBindCtx(0, out IntPtr bindContextPointer);
            if (result != 0 || bindContextPointer == IntPtr.Zero)
            {
                bindContext = null;
                return false;
            }

            try
            {
                bindContext = Marshal.GetObjectForIUnknown(bindContextPointer) as IBindCtx;
                return bindContext != null;
            }
            finally
            {
                Marshal.Release(bindContextPointer);
            }
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
