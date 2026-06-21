using System.Runtime.CompilerServices;
using System.Windows;
using JL.Core.Utilities.Bool;
using JL.Windows.GUI;
using JL.Windows.Interop;
using JL.Windows.Utilities;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;

namespace JL.Windows.External.Magpie;

internal static class MagpieUtils
{
    private static readonly AtomicBool s_isMagpieScaling = new(false);

    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;

    public static bool IsMagpieScaling()
    {
        bool isMagpieScaling = s_isMagpieScaling.Read();
        if (isMagpieScaling)
        {
            isMagpieScaling = IsMagpieReallyScaling();
            s_isMagpieScaling.SetValue(isMagpieScaling);
        }

        return isMagpieScaling;
    }

    private static Rect s_sourceWindowRect;
    public static Rect MagpieWindowRect { get; private set; }
    private static nint s_magpieWindowHandle;

    private static double s_scaleFactorX;
    private static double s_scaleFactorY;

    private static readonly Timer s_deadZoneCheckingTimer = new()
    {
        Interval = 16,
        Enabled = false,
        AutoReset = true
    };

    private static readonly AtomicBool s_mainWindowTransparentToAvoidDeadZone = new(false);

    public static void Init()
    {
        nint magpieWindowHandle = GetMagpieWindowHandle();

        bool isMagpieScaling = magpieWindowHandle is not 0;
        s_isMagpieScaling.SetValue(isMagpieScaling);
        s_magpieWindowHandle = magpieWindowHandle;

        if (isMagpieScaling)
        {
            SetMagpieInfo(magpieWindowHandle);
        }

        s_deadZoneCheckingTimer.Elapsed += DeadZoneCheckEvent;
        s_deadZoneCheckingTimer.Enabled = isMagpieScaling && MainWindowIntersectsWithSourceWindow();
    }

    private static bool ContainsPoint(double rectX, double rectY, double rectWidth, double rectHeight, Point point)
    {
        return point.X >= rectX
            && point.X - rectWidth <= rectX
            && point.Y >= rectY
            && point.Y - rectHeight <= rectY;
    }

    private static bool IntersectsWith(double rectX, double rectY, double rectWidth, double rectHeight, Rect rect)
    {
        return !rect.IsEmpty
            && rect.Left <= rectX + rectWidth
            && rect.Right >= rectX
            && rect.Top <= rectY + rectHeight
            && rect.Bottom >= rectY;
    }

    private static bool MainWindowIntersectsWithSourceWindow()
    {
        MainWindow mainWindow = MainWindow.Instance;
        DpiScale dpi = WindowsUtils.Dpi;
        return IntersectsWith(mainWindow.LeftPositionBeforeResolutionChange * dpi.DpiScaleX, mainWindow.TopPositionBeforeResolutionChange * dpi.DpiScaleY, mainWindow.WidthBeforeResolutionChange * dpi.DpiScaleX, mainWindow.HeightBeforeResolutionChange * dpi.DpiScaleY, s_sourceWindowRect);
    }

    public static void UpdateDeadZoneCheckingState()
    {
        s_deadZoneCheckingTimer.Enabled = IsMagpieScaling() && MainWindowIntersectsWithSourceWindow();
    }

    private static void DeadZoneCheckEvent(object? sender, EventArgs e)
    {
        if (!IsMagpieScaling())
        {
            s_deadZoneCheckingTimer.Enabled = false;
            if (s_mainWindowTransparentToAvoidDeadZone.TrySetFalse())
            {
                WinApi.UnsetTransparentStyle(MainWindow.Instance.WindowHandle);
            }

            return;
        }

        MainWindow mainWindow = MainWindow.Instance;
        DpiScale dpi = WindowsUtils.Dpi;

        Point rawMousePosition = WinApi.GetMousePosition().ToPoint();
        bool isMousePositionScaled = TryGetEffectiveMousePosition(rawMousePosition, out Point effectiveMousePosition);
        bool mouseOverMainWindow = ContainsPoint(mainWindow.LeftPositionBeforeResolutionChange * dpi.DpiScaleX, mainWindow.TopPositionBeforeResolutionChange * dpi.DpiScaleY, mainWindow.WidthBeforeResolutionChange * dpi.DpiScaleX, mainWindow.HeightBeforeResolutionChange * dpi.DpiScaleY, effectiveMousePosition);
        if (mouseOverMainWindow)
        {
            if (s_mainWindowTransparentToAvoidDeadZone.TrySetFalse())
            {
                WinApi.UnsetTransparentStyle(mainWindow.WindowHandle);
            }
        }
        else if (!isMousePositionScaled)
        {
            if (s_deadZoneCheckingTimer.Enabled && s_mainWindowTransparentToAvoidDeadZone.TrySetTrue())
            {
                WinApi.SetTransparentStyle(mainWindow.WindowHandle);
            }
        }
    }

    public static void RegisterToMagpieScalingChangedMessage(nint windowHandle)
    {
        MagpieScalingChangedWindowMessage = WinApi.RegisterToWindowMessage("MagpieScalingChanged");
        _ = WinApi.AllowWindowMessage(windowHandle, MagpieScalingChangedWindowMessage);
    }

    private static double GetMagpieWindowLeftEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestLeft");
    }

    private static double GetMagpieWindowRightEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestRight");
    }

    private static double GetMagpieWindowTopEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestTop");
    }

    private static double GetMagpieWindowBottomEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestBottom");
    }

    private static double GetSourceWindowLeftEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.SrcLeft");
    }

    private static double GetSourceWindowTopEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.SrcTop");
    }

    private static double GetSourceWindowRightEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.SrcRight");
    }

    private static double GetSourceWindowBottomEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.SrcBottom");
    }

    //public static nint GetSourceWindowHande(nint windowHandle)
    //{
    //    return WinApi.GetProp(windowHandle, "Magpie.SrcHWND");
    //}

    /// <summary>
    /// If Magpie crashes or is killed during the process of scaling a window, the MagpieScalingChangedWindowMessage will not be received.
    /// Consequently, IsMagpieScaling may not be set to false.
    /// To ensure Magpie is still running, this method must be used to re-check whether any window is currently being scaled by Magpie.
    /// </summary>
    private static bool IsMagpieReallyScaling()
    {
        return GetMagpieWindowHandle() is not 0;
    }

    private static nint GetMagpieWindowHandle()
    {
        s_magpieWindowHandle = WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22");
        return s_magpieWindowHandle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetEffectiveMousePosition(Point rawMousePosition, out Point effectiveMousePosition)
    {
        if (s_sourceWindowRect.Contains(rawMousePosition))
        {
            Point transformedMousePosition = new(MagpieWindowRect.X + ((rawMousePosition.X - s_sourceWindowRect.X) * s_scaleFactorX), MagpieWindowRect.Y + ((rawMousePosition.Y - s_sourceWindowRect.Y) * s_scaleFactorY));
            if (MagpieWindowRect.Contains(transformedMousePosition) && WinApi.GetWindowFromPoint(rawMousePosition.ToPoint()) != s_magpieWindowHandle)
            {
                effectiveMousePosition = transformedMousePosition;
                return true;
            }
        }

        effectiveMousePosition = rawMousePosition;
        return false;
    }

    public static Point GetEffectiveMousePosition(Point mousePosition)
    {
        return !IsMagpieScaling()
            ? mousePosition
            : TryGetEffectiveMousePosition(mousePosition, out Point transformedMousePosition)
                ? transformedMousePosition
                : mousePosition;
    }

    public static void SetMagpieInfo(nint wParam, nint lParam)
    {
        if (wParam is 0)
        {
            bool isMagpieScaling = lParam is 1;
            s_isMagpieScaling.SetValue(isMagpieScaling);
            if (!isMagpieScaling)
            {
                s_deadZoneCheckingTimer.Enabled = false;
                if (s_mainWindowTransparentToAvoidDeadZone.TrySetFalse())
                {
                    WinApi.UnsetTransparentStyle(MainWindow.Instance.WindowHandle);
                }
            }
            else
            {
                s_deadZoneCheckingTimer.Enabled = MainWindowIntersectsWithSourceWindow();
            }
        }
        else if (wParam is 1 or 2)
        {
            s_isMagpieScaling.SetTrue();
            SetMagpieInfo(wParam is 1 ? lParam : s_magpieWindowHandle);
            s_deadZoneCheckingTimer.Enabled = MainWindowIntersectsWithSourceWindow();
        }
    }

    private static void SetMagpieInfo(nint magpieWindowHandle)
    {
        double magpieWindowTopEdgePosition = GetMagpieWindowTopEdgePositionFromMagpie(magpieWindowHandle);
        double magpieWindowBottomEdgePosition = GetMagpieWindowBottomEdgePositionFromMagpie(magpieWindowHandle);
        double magpieWindowLeftEdgePosition = GetMagpieWindowLeftEdgePositionFromMagpie(magpieWindowHandle);
        double magpieWindowRightEdgePosition = GetMagpieWindowRightEdgePositionFromMagpie(magpieWindowHandle);
        double magpieWindowWidth = magpieWindowRightEdgePosition - magpieWindowLeftEdgePosition;
        double magpieWindowHeight = magpieWindowBottomEdgePosition - magpieWindowTopEdgePosition;
        MagpieWindowRect = new Rect(magpieWindowLeftEdgePosition, magpieWindowTopEdgePosition, magpieWindowWidth, magpieWindowHeight);

        double sourceWindowLeftEdgePosition = GetSourceWindowLeftEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowTopEdgePosition = GetSourceWindowTopEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowRightEdgePosition = GetSourceWindowRightEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowBottomEdgePosition = GetSourceWindowBottomEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowWidth = sourceWindowRightEdgePosition - sourceWindowLeftEdgePosition;
        double sourceWindowHeight = sourceWindowBottomEdgePosition - sourceWindowTopEdgePosition;
        s_sourceWindowRect = new Rect(sourceWindowLeftEdgePosition, sourceWindowTopEdgePosition, sourceWindowWidth, sourceWindowHeight);

        s_scaleFactorX = magpieWindowWidth / sourceWindowWidth;
        s_scaleFactorY = magpieWindowHeight / sourceWindowHeight;
        // SourceWindowHandle = GetSourceWindowHande(lParam);
    }
}
