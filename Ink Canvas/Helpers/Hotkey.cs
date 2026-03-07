using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Ink_Canvas
{
    static partial class Hotkey
    {
        #region 系统api
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, HotkeyModifiers fsModifiers, uint vk);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion

        /// <summary>
        /// 注册快捷键
        /// </summary>
        /// <param name="window">持有快捷键窗口</param>
        /// <param name="fsModifiers">组合键</param>
        /// <param name="key">快捷键</param>
        /// <param name="callBack">回调函数</param>
        public static bool Regist(Window window, HotkeyModifiers fsModifiers, Key key, HotKeyCallBackHanlder callBack)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(callBack);

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            var _hwndSource = HwndSource.FromHwnd(hwnd);
            if (_hwndSource == null)
            {
                return false;
            }

            if (keyid == InitialKeyId)
            {
                _hwndSource.AddHook(WndProc);
            }

            int id = keyid++;

            var vk = KeyInterop.VirtualKeyFromKey(key);
            if (!RegisterHotKey(hwnd, id, fsModifiers, (uint)vk))
            {
                //throw new Exception("regist hotkey fail.");
                return false;
            }
            keymap[id] = callBack;
            return true;
        }

        /// <summary> 
        /// 快捷键消息处理 
        /// </summary> 
        static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (keymap.TryGetValue(id, out var callback))
                {
                    callback();
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary> 
        /// 注销快捷键 
        /// </summary> 
        /// <param name="hWnd">持有快捷键窗口的句柄</param> 
        /// <param name="callBack">回调函数</param> 
        public static void UnRegist(IntPtr hWnd, HotKeyCallBackHanlder callBack)
        {
            List<int> idsToRemove = new List<int>();
            foreach (KeyValuePair<int, HotKeyCallBackHanlder> entry in keymap)
            {
                if (entry.Value == callBack)
                {
                    UnregisterHotKey(hWnd, entry.Key);
                    idsToRemove.Add(entry.Key);
                }
            }

            foreach (int id in idsToRemove)
            {
                keymap.Remove(id);
            }
        }

        const int WM_HOTKEY = 0x312;
        const int InitialKeyId = 10;
        static int keyid = InitialKeyId;
        static readonly Dictionary<int, HotKeyCallBackHanlder> keymap = new Dictionary<int, HotKeyCallBackHanlder>();

        public delegate void HotKeyCallBackHanlder();
    }

    enum HotkeyModifiers
    {
        MOD_ALT = 0x1,
        MOD_CONTROL = 0x2,
        MOD_SHIFT = 0x4,
        MOD_WIN = 0x8
    }
}
