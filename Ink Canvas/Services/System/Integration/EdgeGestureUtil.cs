using global::System;
using global::System.Diagnostics.CodeAnalysis;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Services.System.Integration
{
    /// <summary>
    /// <para>用於實現修改系統屬性<c>System.EdgeGesture.DisableTouchWhenFullscreen</c>達到暫時停用邊緣手勢的效果</para>
    /// <para>僅支持Windows10和Windows11，具體請查閱微軟官方MSDN：<see href="https://learn.microsoft.com/en-us/windows/win32/properties/props-system-edgegesture-disabletouchwhenfullscreen"/></para>
    /// <para><c>========================</c></para>
/// <para>該代碼最初來自 Ink Canvas Modern 的 Fork 上游項目：InkCanvasForClass</para>
    /// <para>
    ///     ICC開源地址(Gitea)：<see href="https://gitea.bliemhax.com/kriastans/InkCanvasForClass"/><br/>
    ///     ICC開源地址(Github)：<see href="https://github.com/kriastans/InkCanvasForClass"/><br/>
    ///     ICC官網：<see href="https://icc.bliemhax.com"/>
    /// </para>
    /// </summary>
    [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "受限 Win32/COM 边界，无托管替代，调用已集中封装并受保护。")]
    public static class EdgeGestureUtil
    {
        private static readonly Guid DISABLE_TOUCH_SCREEN = new("32CE38B2-2C9A-41B1-9BC5-B3784394AA44");
        private static readonly Guid IID_PROPERTY_STORE = new("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99");

        private const short VT_BOOL = 11;

        #region "Structures"

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PropertyKey
        {
            public PropertyKey(Guid guid, uint pid)
            {
                fmtid = guid;
                this.pid = pid;
            }

            [MarshalAs(UnmanagedType.Struct)]
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PropVariant
        {
            [FieldOffset(0)]
            public short vt;
            [FieldOffset(2)]
            private short wReserved1;
            [FieldOffset(4)]
            private short wReserved2;
            [FieldOffset(6)]
            private short wReserved3;
            [FieldOffset(8)]
            private sbyte cVal;
            [FieldOffset(8)]
            private byte bVal;
            [FieldOffset(8)]
            private short iVal;
            [FieldOffset(8)]
            public ushort uiVal;
            [FieldOffset(8)]
            private int lVal;
            [FieldOffset(8)]
            private uint ulVal;
            [FieldOffset(8)]
            private int intVal;
            [FieldOffset(8)]
            private uint uintVal;
            [FieldOffset(8)]
            private long hVal;
            [FieldOffset(8)]
            private long uhVal;
            [FieldOffset(8)]
            private float fltVal;
            [FieldOffset(8)]
            private double dblVal;
            [FieldOffset(8)]
            public bool boolVal;
            [FieldOffset(8)]
            private int scode;
            [FieldOffset(8)]
            private DateTime date;
            [FieldOffset(8)]
            private global::System.Runtime.InteropServices.ComTypes.FILETIME filetime;
            [FieldOffset(8)]
            private Blob blobVal;
            [FieldOffset(8)]
            private IntPtr pwszVal;

            private byte[] GetBlob()
            {
                byte[] result = new byte[blobVal.Length];
                Marshal.Copy(blobVal.Data, result, 0, result.Length);
                return result;
            }

            public object Value
            {
                get
                {
                    VarEnum ve = (VarEnum)vt;
                    return ve switch
                    {
                        VarEnum.VT_I1 => bVal,
                        VarEnum.VT_I2 => iVal,
                        VarEnum.VT_I4 => lVal,
                        VarEnum.VT_I8 => hVal,
                        VarEnum.VT_INT => iVal,
                        VarEnum.VT_UI4 => ulVal,
                        VarEnum.VT_LPWSTR => Marshal.PtrToStringUni(pwszVal),
                        VarEnum.VT_BLOB => GetBlob(),
                        _ => throw new NotImplementedException($"PropVariant {ve}")
                    };
                }
            }
        }

        internal struct Blob
        {
            public int Length;
            public IntPtr Data;

            private void FixCS0649()
            {
                Length = 0;
                Data = IntPtr.Zero;
            }
        }

        #endregion

        #region "Interfaces"

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyStore
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCount([Out, In] ref uint cProps);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetAt([In] uint iProp, ref PropertyKey pkey);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetValue([In] ref PropertyKey key, ref PropVariant pv);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetValue([In] ref PropertyKey key, [In] ref PropVariant pv);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Commit();

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Release();
        }

        #endregion

        #region "Methods"

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHGetPropertyStoreForWindow(IntPtr handle, ref Guid riid, ref IPropertyStore propertyStore);

        public static void DisableEdgeGestures(IntPtr hwnd, bool enable)
        {
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            IPropertyStore pPropStore = null;
            Guid propertyStoreId = IID_PROPERTY_STORE;

            try
            {
                int hr = SHGetPropertyStoreForWindow(hwnd, ref propertyStoreId, ref pPropStore);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                if (pPropStore == null)
                {
                    throw new InvalidOperationException("SHGetPropertyStoreForWindow returned success without an IPropertyStore instance.");
                }

                PropertyKey propKey = new()
                {
                    fmtid = DISABLE_TOUCH_SCREEN,
                    pid = 2
                };
                PropVariant var = new()
                {
                    vt = VT_BOOL,
                    boolVal = enable
                };
                pPropStore.SetValue(ref propKey, ref var);
            }
            finally
            {
                if (pPropStore != null)
                {
                    ComInteropHelper.SafeFinalRelease(pPropStore);
                }
            }
        }

        #endregion
    }
}
