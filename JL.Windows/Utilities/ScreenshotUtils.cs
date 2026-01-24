using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using JL.Windows.External.Magpie;
using JL.Windows.GUI;
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

        bool useMagpiePositioning = WindowsUtils.UseMagpiePositioning(MainWindow.Instance);
        Rectangle referenceWindowRect = !useMagpiePositioning
            ? WindowsUtils.ActiveScreen.Bounds
            : MagpieUtils.MagpieWindowRect.ToRectangle();

        using Bitmap bitmap = new(referenceWindowRect.Width, referenceWindowRect.Height, PixelFormat.Format24bppRgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(referenceWindowRect.Left, referenceWindowRect.Top, 0, 0, new Size(referenceWindowRect.Width, referenceWindowRect.Height), CopyPixelOperation.SourceCopy);
        }

        using MemoryStream ms = new(capacity: referenceWindowRect.Width * referenceWindowRect.Height * 3 / 4);
        bitmap.Save(ms, s_encoder, s_encoderParams);
        return ms.ToArray();
    }
}
