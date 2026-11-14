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
    private const int BytesPerPixel = 3;
    private const int RowAlignment = 4;
    private const long JpegQuality = 92;
    private const int EstimatedCompressionRatio = 4;
    private const byte BlackThreshold = 16;
    private const int MinimumBarSize = 4;
    private const int MaximumBarPercent = 50;
    private const int MinimumBarPairTolerance = 4;
    private const int BarPairToleranceDivisor = 8;

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
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
        return encoderParameters;
    }

    public static byte[]? GetMonitorScreenshot()
    {
        if (s_encoder is null)
        {
            return null;
        }

        bool useMagpiePositioning = MagpieUtils.UseMagpiePositioning();
        Rectangle screenshotBounds = useMagpiePositioning
            ? MagpieUtils.MagpieWindowRect.ToRectangle()
            : WindowsUtils.ActiveScreen.Bounds;

        int width = screenshotBounds.Width;
        int height = screenshotBounds.Height;
        int bytesPerRow = width * BytesPerPixel;

        // DIB scanlines must be aligned to four bytes.
        int stride = (bytesPerRow + RowAlignment - 1) & ~(RowAlignment - 1);

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
            if (bitmapHandle is 0 || pixels is 0)
            {
                return null;
            }

            previousBitmap = WinApi.NativeMethods.SelectObject(memoryDeviceContext, bitmapHandle);
            if (previousBitmap is 0 or -1)
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

            Rectangle contentBounds = useMagpiePositioning
                ? new Rectangle(0, 0, width, height)
                : DetectContentBounds(pixels, width, height, stride);

            nint croppedPixels = pixels + (contentBounds.Y * stride) + (contentBounds.X * BytesPerPixel);
            using Bitmap bitmap = new(contentBounds.Width, contentBounds.Height, stride, PixelFormat.Format24bppRgb, croppedPixels);
            using MemoryStream ms = new(contentBounds.Width * contentBounds.Height * BytesPerPixel / EstimatedCompressionRatio);
            bitmap.Save(ms, s_encoder, s_encoderParams);
            return ms.ToArray();
        }
        finally
        {
            if (screenDeviceContext is not 0)
            {
                _ = WinApi.NativeMethods.ReleaseDC(0, screenDeviceContext);
            }

            if (previousBitmap is not 0 and not -1)
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

    private static unsafe Rectangle DetectContentBounds(nint pixelBuffer, int width, int height, int stride)
    {
        ReadOnlySpan<byte> pixels = new((void*)pixelBuffer, height * stride);
        return DetectContentBounds(pixels, width, height, stride);
    }

    private static Rectangle DetectContentBounds(ReadOnlySpan<byte> pixels, int width, int height, int stride)
    {
        int rowBytes = width * BytesPerPixel;

        int maxHorizontalBar = height * MaximumBarPercent / 100;

        int top = 0;
        while (top < maxHorizontalBar && IsBlackRow(pixels.Slice(top * stride, rowBytes)))
        {
            ++top;
        }

        int bottom = 0;
        while (bottom < maxHorizontalBar && IsBlackRow(pixels.Slice((height - 1 - bottom) * stride, rowBytes)))
        {
            ++bottom;
        }

        if (!IsValidBarPair(top, bottom, maxHorizontalBar))
        {
            top = 0;
            bottom = 0;
        }

        int maxVerticalBar = width * MaximumBarPercent / 100;
        int left = maxVerticalBar;
        int right = maxVerticalBar;

        int contentBottom = height - bottom;
        for (int y = top; y < contentBottom; y++)
        {
            if (left is 0 && right is 0)
            {
                break;
            }

            ReadOnlySpan<byte> row = pixels.Slice(y * stride, rowBytes);

            if (left > 0)
            {
                ReadOnlySpan<byte> head = row[..(left * BytesPerPixel)];
                int index = head.IndexOfAnyExceptInRange((byte)0, BlackThreshold);
                if (index >= 0)
                {
                    left = index / BytesPerPixel;
                }
            }

            if (right > 0)
            {
                ReadOnlySpan<byte> tail = row[((width - right) * BytesPerPixel)..];
                int index = tail.LastIndexOfAnyExceptInRange((byte)0, BlackThreshold);
                if (index >= 0)
                {
                    right = right - 1 - (index / BytesPerPixel);
                }
            }
        }

        if (!IsValidBarPair(left, right, maxVerticalBar))
        {
            left = 0;
            right = 0;
        }

        return new Rectangle(left, top, width - left - right, height - top - bottom);
    }

    private static bool IsBlackRow(ReadOnlySpan<byte> row)
    {
        return row.IndexOfAnyExceptInRange((byte)0, BlackThreshold) < 0;
    }

    private static bool IsValidBarPair(int first, int second, int maximum)
    {
        if (first < MinimumBarSize
            || second < MinimumBarSize
            || first >= maximum
            || second >= maximum)
        {
            return false;
        }

        int tolerance = Math.Max(MinimumBarPairTolerance, Math.Min(first, second) / BarPairToleranceDivisor);
        return Math.Abs(first - second) <= tolerance;
    }
}
