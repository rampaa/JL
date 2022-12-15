using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace JL.Windows;

public class WinApi
{
    internal static class NativeMethods
    {
        internal const int WM_CLIPBOARDUPDATE = 0x031D;
        internal const int WM_ERASEBKGND = 0x0014;
        internal const int WM_SYSCOMMAND = 0x0112;
        internal const int WM_NCCALCSIZE = 0x0083;
        internal const int WVR_VALIDRECTS = 0x0400;
        // internal const int WM_NCHITTEST = 0x0084;
        internal const int SWP_NOSIZE = 0x0001;
        internal const int SWP_NOMOVE = 0x0002;
        internal const int SWP_SHOWWINDOW = 0x0040;
        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        public static readonly IntPtr HWND_TOPMOST = new(-1);

        //RECT Structure
        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int left, top, right, bottom;
        }

        //NCCALCSIZE_PARAMS Structure
        [StructLayout(LayoutKind.Sequential)]
        internal struct NCCALCSIZE_PARAMS
        {
            public RECT rgrc0, rgrc1, rgrc2;
            public WINDOWPOS lppos;
        }

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

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    }

    private enum ResizeDirection
    {
        Left = 61441,
        Right = 61442,
        Top = 61443,
        TopLeft = 61444,
        TopRight = 61445,
        Bottom = 61446,
        BottomLeft = 61447,
        BottomRight = 61448,
    }

    private readonly IntPtr _windowHandle;

    public event EventHandler? ClipboardChanged;

    public WinApi(Window windowSource)
    {
        if (PresentationSource.FromVisual(windowSource) is not HwndSource source)
        {
            throw new ArgumentException(
                "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler."
                , nameof(windowSource));
        }

        source.AddHook(WndProc);

        // get window handle for interop
        _windowHandle = new WindowInteropHelper(windowSource).Handle;

        // register for clipboard events
        NativeMethods.AddClipboardFormatListener(_windowHandle);
    }

    private void OnClipboardChanged()
    {
        ClipboardChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResizeWindow(string borderName)
    {
        IntPtr wParam = IntPtr.Zero;

        switch (borderName)
        {
            case "LeftBorder":
                wParam = (IntPtr)ResizeDirection.Left;
                break;
            case "RightBorder":
                wParam = (IntPtr)ResizeDirection.Right;
                break;
            case "TopBorder":
                wParam = (IntPtr)ResizeDirection.Top;
                break;
            case "TopRightBorder":
                wParam = (IntPtr)ResizeDirection.TopRight;
                break;
            case "BottomBorder":
                wParam = (IntPtr)ResizeDirection.Bottom;
                break;
            case "BottomLeftBorder":
                wParam = (IntPtr)ResizeDirection.BottomLeft;
                break;
            case "BottomRightBorder":
                wParam = (IntPtr)ResizeDirection.BottomRight;
                break;
            case "TopLeftBorder":
                wParam = (IntPtr)ResizeDirection.TopLeft;
                break;
        }

        NativeMethods.SendMessage(_windowHandle, NativeMethods.WM_SYSCOMMAND, wParam, IntPtr.Zero);
    }

    public void KeepTopmost()
    {
        NativeMethods.SetWindowPos(_windowHandle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
    }

    public void PreventFocus()
    {
        NativeMethods.SetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE, NativeMethods.GetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE) | NativeMethods.WS_EX_NOACTIVATE);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case NativeMethods.WM_CLIPBOARDUPDATE:
                OnClipboardChanged();
                handled = true;
                break;

            case NativeMethods.WM_NCCALCSIZE:
                if (wParam != IntPtr.Zero)
                {
                    var calcSizeParams = (NativeMethods.NCCALCSIZE_PARAMS)Marshal.PtrToStructure(lParam, typeof(NativeMethods.NCCALCSIZE_PARAMS))!;
                    calcSizeParams.rgrc1.left = 1;
                    calcSizeParams.rgrc2.left = 1;
                    Marshal.StructureToPtr(calcSizeParams, lParam, true);
                    handled = true;
                    return (IntPtr)NativeMethods.WVR_VALIDRECTS;
                }
                break;

            case NativeMethods.WM_ERASEBKGND:
                handled = true;
                return (IntPtr)1;

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
