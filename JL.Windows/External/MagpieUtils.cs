using System.Windows;
using JL.Windows.Interop;
using JL.Windows.Utilities;

namespace JL.Windows.External;

internal static class MagpieUtils
{
    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;
    public static bool IsMagpieScaling { get; set; }  // = false;
    public static double MagpieWindowLeftEdgePosition { get; private set; }
    public static double MagpieWindowRightEdgePosition { get; private set; }
    public static double MagpieWindowTopEdgePosition { get; private set; }
    public static double MagpieWindowBottomEdgePosition { get; private set; }
    public static double DpiAwareMagpieWindowWidth { get; private set; }
    // public static nint SourceWindowHandle { get; set; }
    public static Rect SourceWindowRect { get; private set; }
    private static double s_scaleFactorX;
    private static double s_scaleFactorY;

    public static void RegisterToMagpieScalingChangedMessage(nint windowHandle)
    {
        MagpieScalingChangedWindowMessage = WinApi.RegisterToWindowMessage("MagpieScalingChanged");
        _ = WinApi.AllowWindowMessage(windowHandle, MagpieScalingChangedWindowMessage);
    }

    public static void MarkWindowAsMagpieToolWindow(nint windowHandle)
    {
        WinApi.SetProp(windowHandle, "Magpie.ToolWindow", 1);
    }

    public static void UnmarkWindowAsMagpieToolWindow(nint windowHandle)
    {
        WinApi.RemoveProp(windowHandle, "Magpie.ToolWindow");
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
    public static bool IsMagpieReallyScaling()
    {
        return GetMagpieWindowHandle() is not 0;
    }

    public static nint GetMagpieWindowHandle()
    {
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22");
    }

    public static Point GetMousePosition(Point mousePosition)
    {
        return SourceWindowRect.Contains(mousePosition)
            ? new Point(MagpieWindowLeftEdgePosition + ((mousePosition.X - SourceWindowRect.X) * s_scaleFactorX),
                MagpieWindowTopEdgePosition + ((mousePosition.Y - SourceWindowRect.Y) * s_scaleFactorY))
            : mousePosition;
    }

    public static void SetMagpieInfo(nint magpieWindowHandle)
    {
        MagpieWindowTopEdgePosition = GetMagpieWindowTopEdgePositionFromMagpie(magpieWindowHandle);
        MagpieWindowBottomEdgePosition = GetMagpieWindowBottomEdgePositionFromMagpie(magpieWindowHandle);
        MagpieWindowLeftEdgePosition = GetMagpieWindowLeftEdgePositionFromMagpie(magpieWindowHandle);
        MagpieWindowRightEdgePosition = GetMagpieWindowRightEdgePositionFromMagpie(magpieWindowHandle);

        double sourceWindowLeftEdgePosition = GetSourceWindowLeftEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowTopEdgePosition = GetSourceWindowTopEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowRightEdgePosition = GetSourceWindowRightEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowBottomEdgePosition = GetSourceWindowBottomEdgePositionFromMagpie(magpieWindowHandle);
        double sourceWindowWidth = sourceWindowRightEdgePosition - sourceWindowLeftEdgePosition;
        double sourceWindowHeight = sourceWindowBottomEdgePosition - sourceWindowTopEdgePosition;

        SourceWindowRect = new Rect(sourceWindowLeftEdgePosition, sourceWindowTopEdgePosition, sourceWindowWidth, sourceWindowHeight);

        // SourceWindowHandle = GetSourceWindowHande(lParam);

        double magpieWindowWidth = MagpieWindowRightEdgePosition - MagpieWindowLeftEdgePosition;
        DpiAwareMagpieWindowWidth = magpieWindowWidth / WindowsUtils.Dpi.DpiScaleX;
        double magpieWindowHeight = MagpieWindowBottomEdgePosition - MagpieWindowTopEdgePosition;

        s_scaleFactorX = magpieWindowWidth / sourceWindowWidth;
        s_scaleFactorY = magpieWindowHeight / sourceWindowHeight;
    }
}
