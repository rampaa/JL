using System.Windows;

namespace JL.Windows.Utilities;

internal static class MagpieUtils
{
    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;
    public static bool IsMagpieScaling { get; set; }  // = false;
    public static double MagpieWindowLeftEdgePosition { get; set; }
    public static double MagpieWindowRightEdgePosition { get; set; }
    public static double MagpieWindowTopEdgePosition { get; set; }
    public static double MagpieWindowBottomEdgePosition { get; set; }
    public static double DpiAwareMagpieWindowWidth { get; set; }
    // public static nint SourceWindowHandle { get; set; }
    public static Rect SourceWindowRect { get; set; }
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
        return GetMagpieWindowHandle() is not 0;
    }

    public static nint GetMagpieWindowHandle()
    {
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22");
    }

    public static Point GetMousePosition(Point mousePosition)
    {
        return SourceWindowRect.Contains(mousePosition)
            ? new Point(MagpieWindowLeftEdgePosition + ((mousePosition.X - SourceWindowRect.X) * ScaleFactorX),
                MagpieWindowTopEdgePosition + ((mousePosition.Y - SourceWindowRect.Y) * ScaleFactorY))
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

        // MagpieUtils.SourceWindowHandle = MagpieUtils.GetSourceWindowHande(lParam);

        double magpieWindowWidth = MagpieWindowRightEdgePosition - MagpieWindowLeftEdgePosition;
        DpiAwareMagpieWindowWidth = magpieWindowWidth / WindowsUtils.Dpi.DpiScaleX;
        double magpieWindowHeight = MagpieWindowBottomEdgePosition - MagpieWindowTopEdgePosition;

        ScaleFactorX = magpieWindowWidth / sourceWindowWidth;
        ScaleFactorY = magpieWindowHeight / sourceWindowHeight;
    }
}
