using System.Windows;

namespace JL.Windows.Utilities;

internal static class MagpieUtils
{
    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;
    public static bool IsMagpieScaling { get; set; }
    public static double MagpieWindowLeftEdgePosition { get; set; }
    public static double MagpieWindowRightEdgePosition { get; set; }
    public static double MagpieWindowTopEdgePosition { get; set; }
    public static double MagpieWindowBottomEdgePosition { get; set; }
    public static double DpiAwareMagpieWindowWidth { get; set; }
    // public static nint SourceWindowHandle { get; set; }
    public static double SourceWindowLeftEdgePosition { get; set; }
    public static double SourceWindowTopEdgePosition { get; set; }
    public static double ScaleFactorX { get; set; }
    public static double ScaleFactorY { get; set; }

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

    public static double GetMagpieWindowLeftEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestLeft");
    }

    public static double GetMagpieWindowRightEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestRight");
    }

    public static double GetMagpieWindowTopEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestTop");
    }

    public static double GetMagpieWindowBottomEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestBottom");
    }

    public static double GetSourceWindowLeftEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.SrcLeft");
    }

    public static double GetSourceWindowTopEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.SrcTop");
    }

    public static double GetSourceWindowRightEdgePositionFromMagpie(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.SrcRight");
    }

    public static double GetSourceWindowBottomEdgePositionFromMagpie(nint windowHandle)
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
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22") is not 0;
    }

    public static Point GetMousePosition(Point mousePosition)
    {
        return new Point(
            MagpieWindowLeftEdgePosition + ((mousePosition.X - SourceWindowLeftEdgePosition) * ScaleFactorX),
            MagpieWindowTopEdgePosition + ((mousePosition.Y - SourceWindowTopEdgePosition) * ScaleFactorY));
    }
}
