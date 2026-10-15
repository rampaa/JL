using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using JL.Windows.External.Magpie;
using JL.Windows.Interop;
using Encoder = System.Drawing.Imaging.Encoder;
using Rectangle = System.Drawing.Rectangle;

namespace JL.Windows.Utilities;

internal static class ScreenshotUtils
{
    private static readonly ImageCodecInfo? s_encoder = GetEncoder(ImageFormat.Jpeg);
    private static readonly EncoderParameters s_encoderParams = CreateEncoderParams();

    private static ImageCodecInfo? GetEncoder(ImageFormat imageFormat)
    {
        Guid imageFormatGuid = imageFormat.Guid;
        foreach (ImageCodecInfo imageCodecInfo in ImageCodecInfo.GetImageEncoders())
        {
            if (imageCodecInfo.FormatID == imageFormatGuid)
            {
                return imageCodecInfo;
            }
        }

        return null;
    }

    private static EncoderParameters CreateEncoderParams()
    {
        EncoderParameters encoderParameters = new(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 92L);
        return encoderParameters;
    }

    public static byte[]? GetMonitorScreenshot()
    {
        if (s_encoder is null)
        {
            return null;
        }

        Rectangle screenshotBounds = MagpieUtils.UseMagpiePositioning()
            ? MagpieUtils.MagpieWindowRect.ToRectangle()
            : WindowsUtils.ActiveScreen.Bounds;

        int width = screenshotBounds.Width;
        int height = screenshotBounds.Height;
        int bytesPerRow = width * 3;

        // Add up to three padding bytes, then clear the lowest two bits to align each row to four bytes.
        int stride = (bytesPerRow + 3) & ~3;

        BitmapInfoHeader bitmapInfo = new(width, height);
        nint memoryDeviceContext = WinApi.NativeMethods.CreateCompatibleDC(0);
        if (memoryDeviceContext is 0)
        {
            return null;
        }

        nint bitmapHandle = 0;
        nint previousBitmap = 0;
        nint screenDeviceContext = 0;
        try
        {
            bitmapHandle = WinApi.NativeMethods.CreateDIBSection(memoryDeviceContext, in bitmapInfo, WinApi.NativeMethods.DIB_RGB_COLORS, out nint pixels, 0, 0);
            if (bitmapHandle is 0)
            {
                return null;
            }

            previousBitmap = WinApi.NativeMethods.SelectObject(memoryDeviceContext, bitmapHandle);
            if (previousBitmap is 0)
            {
                return null;
            }

            screenDeviceContext = WinApi.NativeMethods.GetDC(0);
            if (screenDeviceContext is 0
                || !WinApi.NativeMethods.BitBlt(memoryDeviceContext, 0, 0, width, height, screenDeviceContext, screenshotBounds.Left, screenshotBounds.Top, WinApi.NativeMethods.SRCCOPY))
            {
                return null;
            }

            if (!WinApi.NativeMethods.GdiFlush())
            {
                return null;
            }

            using Bitmap bitmap = new(width, height, stride, PixelFormat.Format24bppRgb, pixels);
            using MemoryStream ms = new(width * height * 3 / 4);
            bitmap.Save(ms, s_encoder, s_encoderParams);
            return ms.ToArray();
        }
        finally
        {
            if (screenDeviceContext is not 0)
            {
                _ = WinApi.NativeMethods.ReleaseDC(0, screenDeviceContext);
            }

            if (previousBitmap is not 0)
            {
                _ = WinApi.NativeMethods.SelectObject(memoryDeviceContext, previousBitmap);
            }

            if (bitmapHandle is not 0)
            {
                _ = WinApi.NativeMethods.DeleteObject(bitmapHandle);
            }

            _ = WinApi.NativeMethods.DeleteDC(memoryDeviceContext);
        }
    }
}
