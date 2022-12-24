﻿using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static JL.Windows.WinApi.NativeMethods;

namespace JL.Windows;

public class WinApi
{
    internal static class NativeMethods
    {
        internal const int WM_WINDOWPOSCHANGING = 0x0046;
        internal const int SWP_NOCOPYBITS = 0x0100;
        internal const int WM_CLIPBOARDUPDATE = 0x031D;
        internal const int WM_ERASEBKGND = 0x0014;
        internal const int WM_SYSCOMMAND = 0x0112;
        internal const int WM_NCCALCSIZE = 0x0083;
        // internal const int WM_NCHITTEST = 0x0084;
        internal const int SWP_NOSIZE = 0x0001;
        internal const int SWP_NOMOVE = 0x0002;
        internal const int SWP_SHOWWINDOW = 0x0040;
        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        public static readonly IntPtr WVR_VALIDRECTS = new(0x0400);
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
            BottomRight = 61448,
        }

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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }

    public event EventHandler? ClipboardChanged;

    private void SubscribeToWndProc(Window windowSource)
    {
        if (PresentationSource.FromVisual(windowSource) is not HwndSource source)
        {
            throw new ArgumentException(
                "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler.",
                nameof(windowSource));
        }

        source.AddHook(WndProc);
    }

    public void SubscribeToClipboardChanged(Window windowSource)
    {
        SubscribeToWndProc(windowSource);
        NativeMethods.AddClipboardFormatListener(new WindowInteropHelper(windowSource).Handle);
    }

    private void OnClipboardChanged()
    {
        ClipboardChanged?.Invoke(this, EventArgs.Empty);
    }

    public static void ResizeWindow(IntPtr windowHandle, string borderName)
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

        SendMessage(windowHandle, WM_SYSCOMMAND, wParam, IntPtr.Zero);
    }

    public static void BringToFront(IntPtr windowHandle)
    {
        SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
    }

    public static void PreventFocus(IntPtr windowHandle)
    {
        SetWindowLong(windowHandle, GWL_EXSTYLE, GetWindowLong(windowHandle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
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
                DefWindowProc(hwnd, msg, wParam, lParam);
                WINDOWPOS windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam)!;
                windowPos.flags |= SWP_NOCOPYBITS;
                Marshal.StructureToPtr(windowPos, lParam, true);
                handled = true;
                break;

                //case WM_NCCALCSIZE:
                //    if (wParam  != IntPtr.Zero)
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
