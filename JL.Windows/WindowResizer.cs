using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using JL.Windows.GUI;

namespace JL.Windows
{
    internal class WindowResizer
    {
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WVR_VALIDRECTS = 0x0400;
        private const int WM_NCHITTEST = 0x0084;
        public IntPtr windowHandle;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

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

        public WindowResizer(Window windowSource)
        {
            if (PresentationSource.FromVisual(windowSource) is not HwndSource source)
            {
                throw new ArgumentException(
                    "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler."
                    , nameof(windowSource));
            }

            source.AddHook(WndProc);

            // get window handle for interop
            windowHandle = new WindowInteropHelper(windowSource).Handle;
        }

        //RECT Structure
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        //WINDOWPOS Structure
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndinsertafter;
            public int x, y, cx, cy;
            public int flags;
        }

        //NCCALCSIZE_PARAMS Structure
        [StructLayout(LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            public RECT rgrc0, rgrc1, rgrc2;
            public WINDOWPOS lppos;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCCALCSIZE:
                    if (wParam != IntPtr.Zero)
                    {
                        handled = true;
                        var calcSizeParams = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(lParam, typeof(NCCALCSIZE_PARAMS))!;
                        calcSizeParams.rgrc1.left = 1;
                        calcSizeParams.rgrc2.left = 1;

                        Marshal.StructureToPtr(calcSizeParams, lParam, true);
                        return (IntPtr)WVR_VALIDRECTS;
                    }
                    break;

                case WM_ERASEBKGND:
                    {
                        handled = true;
                        return (IntPtr)1;
                    }

                case WM_NCHITTEST:
                    {
                        if (MainWindow.Instance.IsMouseOnTitleBar(lParam.ToInt32()))
                        {
                            handled = true;
                            return (IntPtr)2; // HTCAPTION
                        }

                        return IntPtr.Zero;
                    }
            }

            return IntPtr.Zero;
        }

        public void ResizeWindow(Border border)
        {
            IntPtr wParam = IntPtr.Zero;

            switch (border.Name)
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
                case "ButtomBorder":
                    wParam = (IntPtr)ResizeDirection.Bottom;
                    break;
                case "ButtomLeftBorder":
                    wParam = (IntPtr)ResizeDirection.BottomLeft;
                    break;
                case "ButtomRightBorder":
                    wParam = (IntPtr)ResizeDirection.BottomRight;
                    break;
                case "TopLeftBorder":
                    wParam = (IntPtr)ResizeDirection.TopLeft;
                    break;
            }

            SendMessage(windowHandle, WM_SYSCOMMAND, wParam, IntPtr.Zero);
        }

        //[Flags]
        //public enum SetWindowPosFlags : uint
        //{
        //    SWP_ASYNCWINDOWPOS = 0x4000,
        //    SWP_DEFERERASE = 0x2000,
        //    SWP_DRAWFRAME = 0x0020,
        //    SWP_FRAMECHANGED = 0x0020,
        //    SWP_HIDEWINDOW = 0x0080,
        //    SWP_NOACTIVATE = 0x0010,
        //    SWP_NOCOPYBITS = 0x0100,
        //    SWP_NOMOVE = 0x0002,
        //    SWP_NOOWNERZORDER = 0x0200,
        //    SWP_NOREDRAW = 0x0008,
        //    SWP_NOREPOSITION = 0x0200,
        //    SWP_NOSENDCHANGING = 0x0400,
        //    SWP_NOSIZE = 0x0001,
        //    SWP_NOZORDER = 0x0004,
        //    SWP_SHOWWINDOW = 0x0040,
        //}

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        ////GetWindowRect User32 Function
        //[System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
        //[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        //public static extern bool GetWindowRect(
        //    IntPtr hwnd,
        //    out RECT lpRect
        //    );

        //private void WmNCCalcSize(ref Message m)
        //{
        //    //Get Window Rect
        //    RECT formRect = new();
        //    GetWindowRect(m.HWnd, out formRect);

        //    //Check WPARAM
        //    if (m.WParam != IntPtr.Zero)    //TRUE
        //    {
        //        //When TRUE, LPARAM Points to a NCCALCSIZE_PARAMS structure
        //        var nccsp = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS))!;

        //        //We're adjusting the size of the client area here. Right now, the client area is the whole form.
        //        //Adding to the Top, Bottom, Left, and Right will size the client area.
        //        nccsp.rgrc0.top += 30;      //30-pixel top border
        //        nccsp.rgrc0.bottom -= 4;    //4-pixel bottom (resize) border
        //        nccsp.rgrc0.left += 4;      //4-pixel left (resize) border
        //        nccsp.rgrc0.right -= 4;     //4-pixel right (resize) border

        //        //Set the structure back into memory
        //        System.Runtime.InteropServices.Marshal.StructureToPtr(nccsp, m.LParam, true);
        //    }
        //    else    //FALSE
        //    {
        //        //When FALSE, LPARAM Points to a RECT structure
        //        var clnRect = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT))!;

        //        //Like before, we're adjusting the rectangle...
        //        //Adding to the Top, Bottom, Left, and Right will size the client area.
        //        clnRect.top += 30;      //30-pixel top border
        //        clnRect.bottom -= 4;    //4-pixel bottom (resize) border
        //        clnRect.left += 4;      //4-pixel left (resize) border
        //        clnRect.right -= 4;     //4-pixel right (resize) border

        //        //Set the structure back into memory
        //        System.Runtime.InteropServices.Marshal.StructureToPtr(clnRect, m.LParam, true);
        //    }

        //    //Return Zero
        //    m.Result = IntPtr.Zero;
        //}
    }
}
