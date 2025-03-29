using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using JL.Windows.GUI;
using JL.Windows.Utilities;
using static JL.Windows.WinApi.NativeMethods;

namespace JL.Windows;

internal sealed partial class WinApi
{
#pragma warning disable IDE1006 // Naming rule violation
    internal static partial class NativeMethods
    {
        // ReSharper disable InconsistentNaming

        internal const int GWL_EXSTYLE = -20;
        internal const nint HWND_TOPMOST = -1;
        // internal const nint HWND_TOP = 0;
        // internal const nint HWND_NOTOPMOST = -2;
        internal const int SWP_NOACTIVATE = 0x0010;
        internal const int SWP_NOMOVE = 0x0002;
        internal const int SWP_NOSIZE = 0x0001;
        internal const int SWP_NOZORDER = 0x0004;
        internal const int SWP_SHOWWINDOW = 0x0040;
        internal const int SW_SHOWNOACTIVATE = 4;
        internal const int SW_SHOWMINNOACTIVE = 7;
        internal const int WM_CLIPBOARDUPDATE = 0x031D;
        // internal const int WM_ERASEBKGND = 0x0014;
        internal const int WM_HOTKEY = 0x0312;
        internal const int WM_SYSCOMMAND = 0x0112;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        internal const int WS_EX_APPWINDOW = 0x00040000;
        internal const int MOD_NOREPEAT = 0x4000;

        [StructLayout(LayoutKind.Sequential)]
        internal struct LPPOINT : IEquatable<LPPOINT>
        {
            public int X;
            public int Y;

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(X, Y);
            }

            public override readonly bool Equals(object? obj)
            {
                return obj is LPPOINT lpPoint && Equals(lpPoint);
            }

            public readonly bool Equals(LPPOINT other)
            {
                return X == other.X && Y == other.Y;
            }

            public static bool operator ==(LPPOINT left, LPPOINT right) => left.Equals(right);
            public static bool operator !=(LPPOINT left, LPPOINT right) => !left.Equals(right);
        }

        // ReSharper disable UnusedMember.Global
#pragma warning disable CA1028 // Enum storage should be Int32
        internal enum ChangeWindowMessageFilterExAction : uint
        {
            Reset = 0,
            Allow = 1,
            Disallow = 2
        }
#pragma warning restore CA1028 // Enum storage should be Int32
        // ReSharper restore UnusedMember.Global

        [LibraryImport("user32.dll", EntryPoint = "AddClipboardFormatListener", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AddClipboardFormatListener(nint hwnd);

        [LibraryImport("user32.dll", EntryPoint = "RemoveClipboardFormatListener", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool RemoveClipboardFormatListener(nint hwnd);

        [LibraryImport("user32.dll", EntryPoint = "GetClipboardSequenceNumber")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial ulong GetClipboardSequenceNumber();

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

        internal static nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong)
        {
            return Environment.Is64BitProcess
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
        }

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int GetWindowLongPtr32(nint hWnd, int nIndex);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint GetWindowLongPtr64(nint hWnd, int nIndex);

        internal static nint GetWindowLongPtr(nint hWnd, int nIndex)
        {
            return Environment.Is64BitProcess
                ? GetWindowLongPtr64(hWnd, nIndex)
                : GetWindowLongPtr32(hWnd, nIndex);
        }

        [LibraryImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool UnregisterHotKey(nint hWnd, int id);

        [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

        [LibraryImport("user32.dll", EntryPoint = "SetActiveWindow")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint SetActiveWindow(nint hWnd);

        [LibraryImport("user32.dll", EntryPoint = "GetCursorPos", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetCursorPos(ref LPPOINT pt);

        [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int RegisterWindowMessage(string lpString);

        [LibraryImport("user32.dll", EntryPoint = "ChangeWindowMessageFilterEx", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ChangeWindowMessageFilterEx(nint hWnd, int msg, ChangeWindowMessageFilterExAction action, nint changeInfo);

        [LibraryImport("user32.dll", EntryPoint = "SetPropW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetPropW(nint hWnd, string lpString, nint hData);

        [LibraryImport("user32.dll", EntryPoint = "RemovePropW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint RemovePropW(nint hWnd, string lpString);

        [LibraryImport("user32.dll", EntryPoint = "GetPropW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint GetPropW(nint hWnd, string lpString);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint FindWindowW(string? lpClassName, string? lpWindowName);

        [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint GetForegroundWindow();

        [LibraryImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetForegroundWindow(nint hwnd);

        [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial uint GetCurrentThreadId();

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [LibraryImport("user32.dll", EntryPoint = "AttachThreadInput")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);
    }
#pragma warning restore IDE1006 // Naming rule violation

    private static ulong s_clipboardSequenceNo;

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
        s_clipboardSequenceNo = GetClipboardSequenceNumber();
        _ = AddClipboardFormatListener(windowHandle);
    }

    public static void UnsubscribeFromClipboardChanged(nint windowHandle)
    {
        _ = RemoveClipboardFormatListener(windowHandle);
    }

    public static void AddHotKeyToGlobalKeyGestureDict(string hotkeyName, KeyGesture keyGesture)
    {
        ModifierKeys modifierKeys = keyGesture.Modifiers is ModifierKeys.Windows
            ? ModifierKeys.None
            : keyGesture.Modifiers;

        if (modifierKeys is not ModifierKeys.None || KeyGestureUtils.ValidGlobalKeys.Contains(keyGesture.Key))
        {
            KeyGesture newKeyGesture = keyGesture;

            if (modifierKeys is ModifierKeys.None)
            {
                newKeyGesture = new KeyGesture(keyGesture.Key, ModifierKeys.None);
            }

            KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict.Add(hotkeyName, newKeyGesture);
        }
    }

    public static void RegisterAllGlobalHotKeys(nint windowHandle)
    {
        OrderedDictionary<string, KeyGesture> globalKeyGestureNameToKeyGestureDict = KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict;
        int count = globalKeyGestureNameToKeyGestureDict.Count;
        for (int id = 0; id < count; id++)
        {
            KeyGesture keyGesture = globalKeyGestureNameToKeyGestureDict.GetAt(id).Value;
            _ = RegisterHotKey(windowHandle, id, (uint)keyGesture.Modifiers | MOD_NOREPEAT, (uint)KeyInterop.VirtualKeyFromKey(keyGesture.Key));
        }
    }

    public static void UnregisterAllGlobalHotKeys(nint windowHandle)
    {
        int count = KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict.Count;
        for (int id = 0; id < count; id++)
        {
            _ = UnregisterHotKey(windowHandle, id);
        }
    }

    public static void UnregisterAllGlobalHotKeys(nint windowHandle, params ReadOnlySpan<int> keyGestureIdsToIgnore)
    {
        int count = KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict.Count;
        for (int id = 0; id < count; id++)
        {
            if (keyGestureIdsToIgnore.Contains(id))
            {
                continue;
            }

            _ = UnregisterHotKey(windowHandle, id);
        }
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
        _ = SetWindowLongPtr(windowHandle, GWL_EXSTYLE, GetWindowLongPtr(windowHandle, GWL_EXSTYLE) | WS_EX_NOACTIVATE | WS_EX_APPWINDOW);
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

    public static int RegisterToWindowMessage(string messageName)
    {
        return RegisterWindowMessage(messageName);
    }

    private static bool ChangeWindowMessageFilter(nint windowHandle, int message, ChangeWindowMessageFilterExAction filterAction)
    {
        return ChangeWindowMessageFilterEx(windowHandle, message, filterAction, 0);
    }

    public static bool AllowWindowMessage(nint windowHandle, int message)
    {
        return ChangeWindowMessageFilter(windowHandle, message, ChangeWindowMessageFilterExAction.Allow);
    }

    public static void SetProp(nint windowHandle, string lpString, nint hData)
    {
        _ = SetPropW(windowHandle, lpString, hData);
    }

    public static void RemoveProp(nint windowHandle, string lpString)
    {
        _ = RemovePropW(windowHandle, lpString);
    }

    public static nint GetProp(nint windowHandle, string lpString)
    {
        return GetPropW(windowHandle, lpString);
    }

    public static nint FindWindow(string lpClassName)
    {
        return FindWindowW(lpClassName, null);
    }

    public static void MoveWindowToPosition(nint windowHandle, double x, double y)
    {
        _ = SetWindowPos(windowHandle, 0, double.ConvertToIntegerNative<int>(x), double.ConvertToIntegerNative<int>(y), 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    public static nint GetActiveWindowHandle()
    {
        return GetForegroundWindow();
    }

    public static void GiveFocusToWindow(nint windowHandle)
    {
        if (!SetForegroundWindow(windowHandle))
        {
            uint currentThreadId = GetCurrentThreadId();
            uint foregroundThread = GetWindowThreadProcessId(windowHandle, out _);
            if (AttachThreadInput(currentThreadId, foregroundThread, true))
            {
                _ = SetForegroundWindow(windowHandle);
                _ = AttachThreadInput(currentThreadId, foregroundThread, false);
            }
        }
    }

    public static void StealFocus(nint windowHandle)
    {
        uint currentThreadId = GetCurrentThreadId();
        uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        if (AttachThreadInput(currentThreadId, foregroundThread, true))
        {
            _ = SetForegroundWindow(windowHandle);
            _ = AttachThreadInput(currentThreadId, foregroundThread, false);
        }
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg is WM_CLIPBOARDUPDATE)
        {
            ulong clipboardSequenceNo = GetClipboardSequenceNumber();
            if (s_clipboardSequenceNo != clipboardSequenceNo)
            {
                s_clipboardSequenceNo = clipboardSequenceNo;
                ClipboardChanged!.Invoke(null, EventArgs.Empty);
                handled = true;
            }
        }

        else if (msg is WM_HOTKEY)
        {
            int keyGestureId = (int)wParam;
            if (KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict.Count > keyGestureId)
            {
                KeyGesture keyGesture = KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict.GetAt(keyGestureId).Value;
                _ = KeyGestureUtils.HandleHotKey(keyGesture).ConfigureAwait(false);
                handled = true;
            }
        }

        else if (msg == MagpieUtils.MagpieScalingChangedWindowMessage)
        {
            MagpieUtils.IsMagpieScaling = wParam is 1;
            if (MagpieUtils.IsMagpieScaling)
            {
                MagpieUtils.MagpieWindowTopEdgePosition = MagpieUtils.GetMagpieWindowTopEdgePositionFromMagpie(lParam);
                MagpieUtils.MagpieWindowBottomEdgePosition = MagpieUtils.GetMagpieWindowBottomEdgePositionFromMagpie(lParam);
                MagpieUtils.MagpieWindowLeftEdgePosition = MagpieUtils.GetMagpieWindowLeftEdgePositionFromMagpie(lParam);
                MagpieUtils.MagpieWindowRightEdgePosition = MagpieUtils.GetMagpieWindowRightEdgePositionFromMagpie(lParam);
                // MagpieUtils.SourceWindowHandle = MagpieUtils.GetSourceWindowHande(lParam);
                MagpieUtils.DpiAwareMagpieWindowWidth = (MagpieUtils.MagpieWindowRightEdgePosition - MagpieUtils.MagpieWindowLeftEdgePosition) / WindowsUtils.Dpi.DpiScaleX;
                MainWindow.Instance.BringToFront();
            }

            handled = true;
        }

        return 0;
    }
}
