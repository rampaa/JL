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
        internal const int WM_WINDOWPOSCHANGING = 0x0046;
        internal const int SWP_NOCOPYBITS = 0x0100;
        internal const int WM_CLIPBOARDUPDATE = 0x031D;
        internal const int WM_ERASEBKGND = 0x0014;
        internal const int WM_SYSCOMMAND = 0x0112;
        // internal const int WM_NCCALCSIZE = 0x0083;
        // internal const int WM_NCHITTEST = 0x0084;
        internal const int SWP_NOSIZE = 0x0001;
        internal const int SWP_NOMOVE = 0x0002;
        internal const int SWP_SHOWWINDOW = 0x0040;
        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        internal const int WM_HOTKEY = 0x0312;
        // public static readonly IntPtr WVR_VALIDRECTS = new(0x0400);
        public static readonly IntPtr HWND_TOPMOST = new(-1);

        internal enum ResizeDirection
        {
            Left = 61441,
            Right = 61442,
            Top = 61443,
            TopLeft = 61444,
            TopRight = 61445,
            Bottom = 61446,
            BottomLeft = 61447,
            BottomRight = 61448
        }

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
            public IntPtr hwnd;
            public IntPtr hwndinsertafter;
            public int x, y, cx, cy;
            public int flags;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return Environment.Is64BitProcess
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        internal static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return Environment.Is64BitProcess
                ? GetWindowLongPtr64(hWnd, nIndex)
                : new IntPtr(GetWindowLongPtr32(hWnd, nIndex));
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
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

    public static void SubscribeToClipboardChanged(IntPtr windowHandle)
    {
        _ = AddClipboardFormatListener(windowHandle);
    }

    public static void AddHotKeyToKeyGestureDict(string hotkeyName, KeyGesture keyGesture)
    {
        int id = KeyGestureUtils.KeyGestureDict.Count;
        KeyGestureUtils.KeyGestureNameToIntDict.Add(hotkeyName, id);

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
        }
    }

    public static void RegisterAllHotKeys(IntPtr windowHandle)
    {
        foreach (KeyValuePair<int, KeyGesture> keyValuePair in KeyGestureUtils.KeyGestureDict)
        {
            _ = RegisterHotKey(windowHandle, keyValuePair.Key, (uint)keyValuePair.Value.Modifiers, (uint)KeyInterop.VirtualKeyFromKey(keyValuePair.Value.Key));
        }
    }

    public static void UnregisterAllHotKeys(IntPtr windowHandle)
    {
        foreach (int id in KeyGestureUtils.KeyGestureDict.Keys)
        {
            _ = UnregisterHotKey(windowHandle, id);
        }
    }

    public static void UnregisterAllHotKeys(IntPtr windowHandle, int idToIgnore)
    {
        foreach (int id in KeyGestureUtils.KeyGestureDict.Keys)
        {
            if (id == idToIgnore)
            {
                continue;
            }

            _ = UnregisterHotKey(windowHandle, id);
        }
    }

    private void OnClipboardChanged()
    {
        ClipboardChanged?.Invoke(this, EventArgs.Empty);
    }

    public static void ResizeWindow(IntPtr windowHandle, string borderName)
    {
        IntPtr wParam = borderName switch
        {
            "LeftBorder" => (IntPtr)ResizeDirection.Left,
            "RightBorder" => (IntPtr)ResizeDirection.Right,
            "TopBorder" => (IntPtr)ResizeDirection.Top,
            "TopRightBorder" => (IntPtr)ResizeDirection.TopRight,
            "BottomBorder" => (IntPtr)ResizeDirection.Bottom,
            "BottomLeftBorder" => (IntPtr)ResizeDirection.BottomLeft,
            "BottomRightBorder" => (IntPtr)ResizeDirection.BottomRight,
            "TopLeftBorder" => (IntPtr)ResizeDirection.TopLeft,
            _ => IntPtr.Zero
        };

        _ = SendMessage(windowHandle, WM_SYSCOMMAND, wParam, IntPtr.Zero);
    }

    public static void BringToFront(IntPtr windowHandle)
    {
        _ = SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
    }

    public static void PreventActivation(IntPtr windowHandle)
    {
        _ = SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr(GetWindowLongPtr(windowHandle, GWL_EXSTYLE).ToInt32() | WS_EX_NOACTIVATE));
    }

    public static void AllowActivation(IntPtr windowHandle)
    {
        _ = SetWindowLongPtr(windowHandle, GWL_EXSTYLE, IntPtr.Zero);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_CLIPBOARDUPDATE:
                OnClipboardChanged();
                handled = true;
                break;

            case WM_ERASEBKGND:
                handled = true;
                return new IntPtr(1);

            case WM_WINDOWPOSCHANGING:
                _ = DefWindowProc(hwnd, msg, wParam, lParam);
                WINDOWPOS windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                windowPos.flags |= SWP_NOCOPYBITS;
                Marshal.StructureToPtr(windowPos, lParam, true);
                handled = true;
                break;

            case WM_HOTKEY:
                if (KeyGestureUtils.KeyGestureDict.TryGetValue(wParam.ToInt32(), out KeyGesture? keygesture))
                {
                    _ = KeyGestureUtils.HandleHotKey(keygesture).ConfigureAwait(false);
                }
                break;

            default:
                return IntPtr.Zero;

                //case WM_NCCALCSIZE:
                //    if (wParam != IntPtr.Zero)
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
                //        return (IntPtr)2; // HTCAPTION
                //    }
                //    break;
        }

        return IntPtr.Zero;
    }
}
