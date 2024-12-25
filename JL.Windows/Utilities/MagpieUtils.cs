namespace JL.Windows.Utilities;

internal static class MagpieUtils
{
    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;
    public static bool IsMagpieScaling { get; set; } // = false;
    public static double MagpieWindowLeftEdgePosition { get; set; }
    public static double MagpieWindowRightEdgePosition { get; set; }
    public static double MagpieWindowTopEdgePosition { get; set; }
    public static double MagpieWindowBottomEdgePosition { get; set; }
    public static double DpiAwareMagpieWindowWidth { get; set; }

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

    public static double GetMagpieWindowLeftEdgePosition(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestLeft");
    }

    public static double GetMagpieWindowRightEdgePosition(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestRight");
    }

    public static double GetMagpieWindowTopEdgePosition(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestTop");
    }

    public static double GetMagpieWindowBottomEdgePosition(nint windowHandle)
    {
        return WinApi.GetProp(windowHandle, "Magpie.DestBottom");
    }

    // If Magpie crashes or is killed during the process of scaling a window,
    // JL will not receive the MagpieScalingChangedWindowMessage.
    // Consequently, IsMagpieScaling may not be set to false.
    // To ensure Magpie is still running, we must re-check whether it is scaling a window through this method.
    public static bool IsMagpieReallyScaling()
    {
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22") is not 0;
    }
}
