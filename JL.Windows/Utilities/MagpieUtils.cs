namespace JL.Windows.Utilities;

internal static class MagpieUtils
{
    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;
    public static bool IsMagpieScaling { get; set; } // = false;
    public static double DpiAwareMagpieWindowLeftEdgePosition { get; set; }
    public static double DpiAwareMagpieWindowRightEdgePosition { get; set; }
    public static double DpiAwareMagpieWindowTopEdgePosition { get; set; }

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

    public static double GetDpiAwareMagpieWindowLeftEdgePosition(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestLeft") / WindowsUtils.Dpi.DpiScaleX;
    }

    public static double GetDpiAwareMagpieWindowRightEdgePosition(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestRight") / WindowsUtils.Dpi.DpiScaleX;
    }

    public static double GetDpiAwareMagpieWindowTopEdgePosition(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestTop") / WindowsUtils.Dpi.DpiScaleY;
    }

    //private static double GetDpiAwareMagpieWindowBottomEdgePosition(nint windowHandle)
    //{
    //    return WinApi.GetProp(windowHandle, "Magpie.DestBottom") / WindowsUtils.Dpi.DpiScaleY;
    //}

    public static bool IsMagpieReallyScaling()
    {
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22") is not 0;
    }
}
