using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using JL.Windows.Utilities;
using static JL.Windows.WinApi.NativeMethods;

namespace JL.Windows;

internal sealed class WinApi
{
#pragma warning disable IDE1006
    internal static class NativeMethods
    {
        internal const int GWL_EXSTYLE = -20;
        // public const nint HTCAPTION = 2;
        internal const nint HWND_TOPMOST = -1;
        // internal const nint HWND_TOP = 0;
        // internal const nint HWND_NOTOPMOST = -2;
        internal const int SWP_NOACTIVATE = 0x0010;
        internal const int SWP_NOCOPYBITS = 0x0100;
        internal const int SWP_NOMOVE = 0x0002;
        internal const int SWP_NOSIZE = 0x0001;
        internal const int SWP_SHOWWINDOW = 0x0040;
        internal const int SW_SHOWNOACTIVATE = 4;
        internal const int SW_SHOWMINNOACTIVE = 7;
        internal const int WM_CLIPBOARDUPDATE = 0x031D;
        internal const int WM_ERASEBKGND = 0x0014;
        internal const int WM_HOTKEY = 0x0312;
        // internal const int WM_NCCALCSIZE = 0x0083;
        // internal const int WM_NCHITTEST = 0x0084;
        internal const int WM_SYSCOMMAND = 0x0112;
        internal const int WM_WINDOWPOSCHANGING = 0x0046;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        // public const nint WVR_VALIDRECTS = 0x0400;

        // RECT Structure
        // [StructLayout(LayoutKind.Sequential)]
        // internal struct RECT
        // {
        //     public int left, top, right, bottom;
        // }

        //NCCALCSIZE_PARAMS Structure
        // [StructLayout(LayoutKind.Sequential)]
        // internal struct NCCALCSIZE_PARAMS
        // {
        //     public RECT rgrc0, rgrc1, rgrc2;
        //     public WINDOWPOS lppos;
        // }

        //WINDOWPOS Structure
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public nint hwnd;
            public nint hwndinsertafter;
            public int x, y, cx, cy;
            public int flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LPPOINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", EntryPoint = "AddClipboardFormatListener", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AddClipboardFormatListener(nint hwnd);

        [DllImport("user32.dll", EntryPoint = "RemoveClipboardFormatListener", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveClipboardFormatListener(nint hwnd);

        [DllImport("user32.dll", EntryPoint = "GetClipboardSequenceNumber", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern ulong GetClipboardSequenceNumber();

        [DllImport("user32.dll", EntryPoint = "SendMessageW", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

        internal static nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong)
        {
            return Environment.Is64BitProcess
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int GetWindowLongPtr32(nint hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern nint GetWindowLongPtr64(nint hWnd, int nIndex);

        internal static nint GetWindowLongPtr(nint hWnd, int nIndex)
        {
            return Environment.Is64BitProcess
                ? GetWindowLongPtr64(hWnd, nIndex)
                : GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern nint DefWindowProc(nint hWnd, int msg, nint wParam, nint lParam);

        [DllImport("user32.dll", EntryPoint = "RegisterHotKey", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(nint hWnd, int id);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "SetActiveWindow", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern nint SetActiveWindow(nint hWnd);

        [DllImport("user32.dll", EntryPoint = "GetCursorPos", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref LPPOINT pt);
    }
#pragma warning restore IDE1006

    public event EventHandler? ClipboardChanged;

    public void SubscribeToWndProc(Window windowSource)
    {
        if (PresentationSource.FromVisual(windowSource) is not HwndSource source)
        {
            throw new ArgumentException(
                "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler.",
                nameof(windowSource));
        }

        source.AddHook(WndProc);
    }

    public static void SubscribeToClipboardChanged(nint windowHandle)
    {
        _ = AddClipboardFormatListener(windowHandle);
    }

    public static void UnsubscribeFromClipboardChanged(nint windowHandle)
    {
        _ = RemoveClipboardFormatListener(windowHandle);
    }

    public static ulong GetClipboardSequenceNo()
    {
        return GetClipboardSequenceNumber();
    }

    public static void AddHotKeyToKeyGestureDict(string hotkeyName, KeyGesture keyGesture)
    {
        int id = KeyGestureUtils.KeyGestureDict.Count;

        ModifierKeys modifierKeys = keyGesture.Modifiers is ModifierKeys.Windows
            ? ModifierKeys.None
            : keyGesture.Modifiers;

        if (modifierKeys is not ModifierKeys.None || KeyGestureUtils.ValidKeys.Contains(keyGesture.Key))
        {
            KeyGesture newKeyGesture = keyGesture;

            if (modifierKeys is ModifierKeys.None)
            {
                newKeyGesture = new KeyGesture(keyGesture.Key, ModifierKeys.None);
            }

            KeyGestureUtils.KeyGestureDict.Add(id, newKeyGesture);
            KeyGestureUtils.KeyGestureNameToIntDict.Add(hotkeyName, id);
        }
    }

    public static void RegisterAllHotKeys(nint windowHandle)
    {
        foreach (KeyValuePair<int, KeyGesture> keyValuePair in KeyGestureUtils.KeyGestureDict)
        {
            _ = RegisterHotKey(windowHandle, keyValuePair.Key, (uint)keyValuePair.Value.Modifiers, (uint)KeyInterop.VirtualKeyFromKey(keyValuePair.Value.Key));
        }
    }

    public static void UnregisterAllHotKeys(nint windowHandle)
    {
        foreach (int id in KeyGestureUtils.KeyGestureDict.Keys)
        {
            _ = UnregisterHotKey(windowHandle, id);
        }
    }

    public static void UnregisterAllHotKeys(nint windowHandle, int keyGestureIdToIgnore)
    {
        foreach (int id in KeyGestureUtils.KeyGestureDict.Keys)
        {
            if (keyGestureIdToIgnore == id)
            {
                continue;
            }

            _ = UnregisterHotKey(windowHandle, id);
        }
    }

    public static void UnregisterAllHotKeys(nint windowHandle, List<int> keyGestureIdsToIgnore)
    {
        foreach (int id in KeyGestureUtils.KeyGestureDict.Keys)
        {
            if (keyGestureIdsToIgnore.Contains(id))
            {
                continue;
            }

            _ = UnregisterHotKey(windowHandle, id);
        }
    }

    private void OnClipboardChanged()
    {
        ClipboardChanged!.Invoke(this, EventArgs.Empty);
    }

    public static void ResizeWindow(nint windowHandle, nint wParam)
    {
        _ = SendMessage(windowHandle, WM_SYSCOMMAND, wParam, 0);
    }

    public static void BringToFront(nint windowHandle)
    {
        _ = SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
    }

    public static void MinimizeWindow(nint windowHandle)
    {
        _ = ShowWindow(windowHandle, SW_SHOWMINNOACTIVE);
    }

    public static void RestoreWindow(nint windowHandle)
    {
        _ = ShowWindow(windowHandle, SW_SHOWNOACTIVATE);
    }

    public static void PreventActivation(nint windowHandle)
    {
        _ = SetWindowLongPtr(windowHandle, GWL_EXSTYLE, GetWindowLongPtr(windowHandle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
    }

    public static void AllowActivation(nint windowHandle)
    {
        _ = SetWindowLongPtr(windowHandle, GWL_EXSTYLE, 0);
    }

    public static void ActivateWindow(nint windowHandle)
    {
        _ = SetActiveWindow(windowHandle);
    }

    public static Point GetMousePosition()
    {
        LPPOINT lpPoint = new();
        _ = GetCursorPos(ref lpPoint);

        return new Point(lpPoint.X, lpPoint.Y);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_CLIPBOARDUPDATE:
                OnClipboardChanged();
                handled = true;
                break;

            case WM_HOTKEY:
                if (KeyGestureUtils.KeyGestureDict.TryGetValue((int)wParam, out KeyGesture? keyGesture))
                {
                    _ = KeyGestureUtils.HandleHotKey(keyGesture).ConfigureAwait(false);
                }

                break;

            case WM_ERASEBKGND:
                handled = true;
                return 1;

            case WM_WINDOWPOSCHANGING:
                _ = DefWindowProc(hwnd, msg, wParam, lParam);
                WINDOWPOS windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                windowPos.flags |= SWP_NOCOPYBITS;
                Marshal.StructureToPtr(windowPos, lParam, true);
                handled = true;
                break;

            default:
                return 0;

                //case WM_NCCALCSIZE:
                //    if (wParam is not 0)
                //    {
                //        NCCALCSIZE_PARAMS calcSizeParams = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
                //        calcSizeParams.rgrc1.left = 0;
                //        calcSizeParams.rgrc1.right = 1;
                //        calcSizeParams.rgrc1.top = 0;
                //        calcSizeParams.rgrc1.bottom = 1;

                //        calcSizeParams.rgrc2.left = 0;
                //        calcSizeParams.rgrc2.right = 1;
                //        calcSizeParams.rgrc2.top = 0;
                //        calcSizeParams.rgrc2.bottom = 1;

                //        Marshal.StructureToPtr(calcSizeParams, lParam, true);
                //        handled = true;
                //        return WVR_VALIDRECTS;
                //    }
                //    break;

                //case NativeMethods.WM_NCHITTEST:
                //    if (MainWindow.Instance.IsMouseOnTitleBar(lParam.ToInt32()))
                //    {
                //        handled = true;
                //        return HTCAPTION;
                //    }
                //    break;
        }

        return 0;
    }
}
