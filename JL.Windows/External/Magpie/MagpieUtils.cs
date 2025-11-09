using System.Windows;
using JL.Windows.Interop;

namespace JL.Windows.External.Magpie;

internal static class MagpieUtils
{
    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;

    private static bool s_isMagpieScaling;
    public static bool IsMagpieScaling
    {
        get
        {
            if (s_isMagpieScaling)
            {
                s_isMagpieScaling = IsMagpieReallyScaling();
            }

            return s_isMagpieScaling;
        }

        private set => s_isMagpieScaling = value;
    }

    // public static nint SourceWindowHandle { get; set; }
    public static Rect SourceWindowRect { get; private set; }
    public static Rect MagpieWindowRect { get; private set; }

    private static double s_scaleFactorX;
    private static double s_scaleFactorY;

    public static void Init()
    {
        nint magpieWindowHandle = GetMagpieWindowHandle();
        SetMagpieInfo(magpieWindowHandle is not 0, magpieWindowHandle);
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
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22");
    }

    public static Point GetMousePosition(Point mousePosition)
    {
        return SourceWindowRect.Contains(mousePosition)
            ? new Point(MagpieWindowRect.X + ((mousePosition.X - SourceWindowRect.X) * s_scaleFactorX),
                MagpieWindowRect.Y + ((mousePosition.Y - SourceWindowRect.Y) * s_scaleFactorY))
            : mousePosition;
    }

    public static void SetMagpieInfo(bool isMagpieScaling, nint magpieWindowHandle)
    {
        IsMagpieScaling = isMagpieScaling;
        if (isMagpieScaling)
        {
            SetMagpieInfo(magpieWindowHandle);
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
        SourceWindowRect = new Rect(sourceWindowLeftEdgePosition, sourceWindowTopEdgePosition, sourceWindowWidth, sourceWindowHeight);

        s_scaleFactorX = magpieWindowWidth / sourceWindowWidth;
        s_scaleFactorY = magpieWindowHeight / sourceWindowHeight;
        // SourceWindowHandle = GetSourceWindowHande(lParam);
    }
}
