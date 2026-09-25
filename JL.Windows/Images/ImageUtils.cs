using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Utilities;

namespace JL.Windows.Images;

internal static class ImageUtils
{
    private const double DefaultDpi = 96.0;
    private const double WicWebpDpi = 72.0;
    private const double MetersPerInch = 0.0254;
    private const double CentimetersPerInch = 2.54;
    private const float InverseFloatMaxPrecision = 1.0F / 0xFFFFFF;

    private const int MetadataReadBufferSize = 4096;
    private const int XmpApp1IdentifierLength = 29;
    private const int ExtendedXmpApp1IdentifierLength = 35;

    private const uint RiffSignature = 0x46464952;
    private const uint WebpSignature = 0x50424557;
    private const uint Vp8XSignature = 0x58385056;
    private const uint Vp8LSignature = 0x4C385056;
    private const uint Vp8Signature = 0x20385056;
    private const ulong PngSignature = 0x0A1A0A0D474E5089;
    private const uint IhdrChunkType = 0x49484452;
    private const uint PhysicalDimensionsChunkType = 0x70485973;
    private const uint ImageDataChunkType = 0x49444154;
    private const uint ImageEndChunkType = 0x49454E44;
    private const uint GifSignature = 0x38464947;
    private const uint JfifSignature = 0x4649464A;
    private const uint ExifSignature = 0x66697845;
    private const uint PhotoshopSignature = 0x4D494238;

    public static ImageInfo? GetImageInfo(string imagePath)
    {
        string fullImagePath = Path.GetFullPath(imagePath, AppInfo.ApplicationPath);

        try
        {
            ImageInfo? imageInfo;
            ImageFormat imageFormat = GetImageFormat(Path.GetExtension(imagePath.AsSpan()));
            switch (imageFormat)
            {
                case ImageFormat.Webp:
                    if (TryGetWebpImageInfo(fullImagePath, imagePath, out imageInfo))
                    {
                        return imageInfo;
                    }

                    break;

                case ImageFormat.Png:
                    if (TryGetPngImageInfo(fullImagePath, imagePath, out imageInfo))
                    {
                        return imageInfo;
                    }

                    break;

                case ImageFormat.Gif:
                    if (TryGetGifImageInfo(fullImagePath, imagePath, out imageInfo))
                    {
                        return imageInfo;
                    }

                    break;

                case ImageFormat.Ico:
                    if (TryGetIcoImageInfo(fullImagePath, imagePath, out imageInfo))
                    {
                        return imageInfo;
                    }

                    break;

                case ImageFormat.Bmp:
                    if (TryGetBmpImageInfo(fullImagePath, imagePath, out imageInfo))
                    {
                        return imageInfo;
                    }

                    break;

                case ImageFormat.Jpeg:
                    if (TryGetJpegImageInfo(fullImagePath, imagePath, out imageInfo))
                    {
                        return imageInfo;
                    }

                    break;

                case ImageFormat.Unknown:
                    break;

                default:
                    LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(ImageFormat), nameof(ImageUtils), nameof(GetImageInfo), imageFormat);
                    break;
            }

            Uri imageUri = new(fullImagePath);
            BitmapFrame frame = BitmapFrame.Create(imageUri, BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);
            return new ImageInfo(imagePath, frame.PixelWidth, frame.PixelHeight, frame.Width, frame.Height);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Warning(ex, "Failed to read image dimensions for '{ImagePath}'", fullImagePath);
            return null;
        }
    }

    private static bool TryGetWebpImageInfo(string fullImagePath, string imagePath, out ImageInfo? imageInfo)
    {
        imageInfo = null;

        using FileStream fileStream = OpenFixedHeaderImageFile(fullImagePath);

        Span<byte> header = stackalloc byte[30];
        int bytesRead = ReadUpTo(fileStream, header);

        if (bytesRead < 20
            || BinaryPrimitives.ReadUInt32LittleEndian(header) is not RiffSignature
            || BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(8, 4)) is not WebpSignature)
        {
            return false;
        }

        uint chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(16, 4));

        int width;
        int height;

        uint chunkType = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(12, 4));

        if (chunkType is Vp8XSignature)
        {
            if (chunkSize is not 10 || bytesRead < 30)
            {
                return false;
            }

            width = ReadUInt24LittleEndian(header.Slice(24, 3)) + 1;
            height = ReadUInt24LittleEndian(header.Slice(27, 3)) + 1;

            if ((ulong)width * (ulong)height > uint.MaxValue)
            {
                return false;
            }
        }
        else if (chunkType is Vp8LSignature)
        {
            if (chunkSize < 5 || bytesRead < 25 || header[20] is not 0x2F)
            {
                return false;
            }

            uint imageSize = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(21, 4));

            if ((imageSize >> 29) is not 0)
            {
                return false;
            }

            width = (int)(imageSize & 0x3FFF) + 1;
            height = (int)((imageSize >> 14) & 0x3FFF) + 1;
        }
        else if (chunkType is Vp8Signature)
        {
            if (chunkSize < 10
                || bytesRead < 30
                || (header[20] & 1) is not 0
                || header[23] is not 0x9D
                || header[24] is not 0x01
                || header[25] is not 0x2A)
            {
                return false;
            }

            width = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(26, 2)) & 0x3FFF;
            height = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(28, 2)) & 0x3FFF;

            if (width is 0 || height is 0)
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        imageInfo = CreateImageInfo(imagePath, width, height, WicWebpDpi, WicWebpDpi);
        return true;
    }

    private static bool TryGetPngImageInfo(string fullImagePath, string imagePath, out ImageInfo? imageInfo)
    {
        imageInfo = null;

        using FileStream fileStream = OpenMetadataImageFile(fullImagePath);

        Span<byte> readBuffer = stackalloc byte[MetadataReadBufferSize];
        BufferedImageReader reader = new(fileStream, readBuffer);

        Span<byte> header = stackalloc byte[24];
        if (!reader.TryReadExactly(header)
            || BinaryPrimitives.ReadUInt64LittleEndian(header) is not PngSignature
            || BinaryPrimitives.ReadUInt32BigEndian(header.Slice(8, 4)) is not 13
            || BinaryPrimitives.ReadUInt32BigEndian(header.Slice(12, 4)) is not IhdrChunkType)
        {
            return false;
        }

        uint nativeWidth = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(16, 4));
        uint nativeHeight = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(20, 4));

        if (nativeWidth is 0 or > int.MaxValue || nativeHeight is 0 or > int.MaxValue)
        {
            return false;
        }

        int width = (int)nativeWidth;
        int height = (int)nativeHeight;

        if (!reader.TrySkip(9))
        {
            return false;
        }

        Span<byte> chunkHeader = stackalloc byte[8];
        Span<byte> physicalDimensions = stackalloc byte[9];

        while (reader.TryReadExactly(chunkHeader))
        {
            uint chunkLength = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader[..4]);
            uint chunkType = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader.Slice(4, 4));

            if (chunkType is PhysicalDimensionsChunkType)
            {
                if (chunkLength is not 9
                    || !reader.TryReadExactly(physicalDimensions))
                {
                    return false;
                }

                uint pixelsPerUnitX = BinaryPrimitives.ReadUInt32BigEndian(physicalDimensions[..4]);
                uint pixelsPerUnitY = BinaryPrimitives.ReadUInt32BigEndian(physicalDimensions.Slice(4, 4));
                byte unit = physicalDimensions[8];

                if (unit is 0)
                {
                    imageInfo = CreateImageInfo(imagePath, width, height, DefaultDpi, DefaultDpi);
                    return true;
                }

                if (unit is not 1)
                {
                    return false;
                }

                double dpiX = pixelsPerUnitX * MetersPerInch;
                double dpiY = pixelsPerUnitY * MetersPerInch;

                imageInfo = CreateImageInfo(imagePath, width, height, dpiX, dpiY);
                return true;
            }

            if (chunkType is ImageDataChunkType or ImageEndChunkType)
            {
                imageInfo = CreateImageInfo(imagePath, width, height, DefaultDpi, DefaultDpi);
                return true;
            }

            if (!reader.TrySkip(chunkLength + 4L))
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryGetGifImageInfo(string fullImagePath, string imagePath, out ImageInfo? imageInfo)
    {
        imageInfo = null;

        using FileStream fileStream = OpenMetadataImageFile(fullImagePath);

        Span<byte> readBuffer = stackalloc byte[MetadataReadBufferSize];
        BufferedImageReader reader = new(fileStream, readBuffer);

        Span<byte> header = stackalloc byte[13];
        if (!reader.TryReadExactly(header)
            || BinaryPrimitives.ReadUInt32LittleEndian(header) is not GifSignature
            || header[5] is not (byte)'a'
            || (header[4] is not (byte)'7' && header[4] is not (byte)'9'))
        {
            return false;
        }

        byte packedFields = header[10];
        byte pixelAspectRatio = header[12];

        if ((packedFields & 0x80) is not 0)
        {
            int globalColorTableSize = 3 * (1 << ((packedFields & 0x07) + 1));
            if (!reader.TrySkip(globalColorTableSize))
            {
                return false;
            }
        }

        double dpiX = DefaultDpi;
        if (pixelAspectRatio is not 0)
        {
            double aspectRatio = (pixelAspectRatio + 15.0) / 64.0;
            dpiX /= aspectRatio;
        }

        Span<byte> imageDescriptor = stackalloc byte[9];
        while (reader.TryReadByte(out byte blockType))
        {
            if (blockType is 0x2C)
            {
                if (!reader.TryReadExactly(imageDescriptor))
                {
                    return false;
                }

                int width = BinaryPrimitives.ReadUInt16LittleEndian(imageDescriptor.Slice(4, 2));
                int height = BinaryPrimitives.ReadUInt16LittleEndian(imageDescriptor.Slice(6, 2));

                if (width is 0 || height is 0)
                {
                    return false;
                }

                imageInfo = CreateImageInfo(imagePath, width, height, dpiX, DefaultDpi);
                return true;
            }

            if (blockType is 0x21)
            {
                if (!reader.TryReadByte(out _) || !TrySkipGifSubBlocks(ref reader))
                {
                    return false;
                }

                continue;
            }

            return false;
        }

        return false;
    }

    private static bool TryGetIcoImageInfo(string fullImagePath, string imagePath, out ImageInfo? imageInfo)
    {
        imageInfo = null;

        using FileStream fileStream = OpenFixedHeaderImageFile(fullImagePath);

        Span<byte> header = stackalloc byte[6];
        if (!TryReadExactly(fileStream, header)
            || BinaryPrimitives.ReadUInt16LittleEndian(header[..2]) is not 0
            || BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(2, 2)) is not 1)
        {
            return false;
        }

        int imageCount = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(4, 2));
        if (imageCount is 0)
        {
            return false;
        }

        int width = 0;
        int height = 0;
        int smallestArea = int.MaxValue;

        Span<byte> entries = stackalloc byte[MetadataReadBufferSize];
        int entriesPerBatch = entries.Length / 16;
        int remainingImageCount = imageCount;

        while (remainingImageCount > 0)
        {
            int batchImageCount = Math.Min(remainingImageCount, entriesPerBatch);
            Span<byte> currentEntries = entries[..(batchImageCount * 16)];

            if (!TryReadExactly(fileStream, currentEntries))
            {
                return false;
            }

            for (int i = 0; i < batchImageCount; i++)
            {
                ReadOnlySpan<byte> entry = currentEntries.Slice(i * 16, 16);

                int entryWidth = entry[0] is 0 ? 256 : entry[0];
                int entryHeight = entry[1] is 0 ? 256 : entry[1];
                int area = entryWidth * entryHeight;

                if (area < smallestArea)
                {
                    smallestArea = area;
                    width = entryWidth;
                    height = entryHeight;
                }
            }

            remainingImageCount -= batchImageCount;
        }

        imageInfo = CreateImageInfo(imagePath, width, height, DefaultDpi, DefaultDpi);
        return true;
    }

    private static bool TryGetBmpImageInfo(string fullImagePath, string imagePath, out ImageInfo? imageInfo)
    {
        imageInfo = null;

        using FileStream fileStream = OpenFixedHeaderImageFile(fullImagePath);

        Span<byte> header = stackalloc byte[54];
        int bytesRead = ReadUpTo(fileStream, header);

        if (bytesRead < 26
            || header[0] is not (byte)'B'
            || header[1] is not (byte)'M')
        {
            return false;
        }

        uint dibHeaderSize = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(14, 4));

        int width;
        int height;
        if (dibHeaderSize is 12)
        {
            width = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(18, 2));
            height = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(20, 2));

            if (width is 0 || height is 0)
            {
                return false;
            }

            imageInfo = CreateImageInfo(imagePath, width, height, DefaultDpi, DefaultDpi);
            return true;
        }

        if (dibHeaderSize < 40 || bytesRead < 46)
        {
            return false;
        }

        int nativeWidth = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(18, 4));
        int nativeHeight = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(22, 4));

        if (nativeWidth <= 0 || nativeHeight is 0 or int.MinValue)
        {
            return false;
        }

        width = nativeWidth;
        height = Math.Abs(nativeHeight);

        int pixelsPerMeterX = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(38, 4));
        int pixelsPerMeterY = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(42, 4));

        double dpiX = pixelsPerMeterX > 0
            ? pixelsPerMeterX * MetersPerInch
            : DefaultDpi;

        double dpiY = pixelsPerMeterY > 0
            ? pixelsPerMeterY * MetersPerInch
            : DefaultDpi;

        imageInfo = CreateImageInfo(imagePath, width, height, dpiX, dpiY);
        return true;
    }

    private static bool TryGetJpegImageInfo(string fullImagePath, string imagePath, out ImageInfo? imageInfo)
    {
        imageInfo = null;

        using FileStream fileStream = OpenMetadataImageFile(fullImagePath);

        Span<byte> readBuffer = stackalloc byte[MetadataReadBufferSize];
        BufferedImageReader reader = new(fileStream, readBuffer);

        Span<byte> soi = stackalloc byte[2];
        if (!reader.TryReadExactly(soi)
            || soi[0] is not 0xFF
            || soi[1] is not 0xD8)
        {
            return false;
        }

        double dpiX = DefaultDpi;
        double dpiY = DefaultDpi;
        double nativeDensityX = 1.0;
        double nativeDensityY = 1.0;
        JpegResolutionUnit resolutionUnit = JpegResolutionUnit.None;

        bool hasExifMetadata = false;
        bool hasPhotoshopApp13Metadata = false;
        bool secondaryResolutionMetadataBlocked = false;

        int width = 0;
        int height = 0;

        Span<byte> segmentLengthBuffer = stackalloc byte[2];
        Span<byte> app0 = stackalloc byte[14];
        Span<byte> app1Header = stackalloc byte[ExtendedXmpApp1IdentifierLength];
        Span<byte> app13Header = stackalloc byte[14];
        Span<byte> frameHeader = stackalloc byte[5];

        while (reader.TryReadByte(out byte prefix))
        {
            if (prefix is not 0xFF)
            {
                return false;
            }

            byte marker;
            do
            {
                if (!reader.TryReadByte(out marker))
                {
                    return false;
                }
            }
            while (marker is 0xFF);

            if (marker is 0x00 or 0xD8)
            {
                return false;
            }

            if (marker is 0xDA)
            {
                if (width is 0 || height is 0)
                {
                    return false;
                }

                imageInfo = CreateImageInfo(imagePath, width, height, dpiX, dpiY);
                return true;
            }

            if (marker is 0xD9)
            {
                return false;
            }

            if (marker is 0x01 or (>= 0xD0 and <= 0xD7))
            {
                continue;
            }

            if (!reader.TryReadExactly(segmentLengthBuffer))
            {
                return false;
            }

            int segmentLength = BinaryPrimitives.ReadUInt16BigEndian(segmentLengthBuffer);
            if (segmentLength < 2)
            {
                return false;
            }

            int payloadLength = segmentLength - 2;
            long payloadStart = reader.Position;
            long segmentEnd = payloadStart + payloadLength;

            if (marker is 0xE0 && payloadLength >= app0.Length)
            {
                if (!reader.TryReadExactly(app0))
                {
                    return false;
                }

                if (BinaryPrimitives.ReadUInt32LittleEndian(app0) is JfifSignature
                    && app0[4] is 0)
                {
                    byte densityUnit = app0[7];
                    ushort densityX = BinaryPrimitives.ReadUInt16BigEndian(app0.Slice(8, 2));
                    ushort densityY = BinaryPrimitives.ReadUInt16BigEndian(app0.Slice(10, 2));

                    if (densityUnit is 0)
                    {
                        nativeDensityX = densityX;
                        nativeDensityY = densityY;

                        dpiX = DefaultDpi;
                        dpiY = DefaultDpi;
                        resolutionUnit = JpegResolutionUnit.Unitless;
                    }
                    else if (densityUnit is 1)
                    {
                        if (densityX is 0 && densityY is 0)
                        {
                            continue;
                        }

                        if (densityX is 0)
                        {
                            densityX = densityY;
                        }
                        else if (densityY is 0)
                        {
                            densityY = densityX;
                        }

                        nativeDensityX = densityX;
                        nativeDensityY = densityY;

                        dpiX = densityX;
                        dpiY = densityY;
                        resolutionUnit = JpegResolutionUnit.Inches;
                    }
                    else if (densityUnit is 2)
                    {
                        if (densityX is 0 && densityY is 0)
                        {
                            continue;
                        }

                        if (densityX is 0)
                        {
                            densityX = densityY;
                        }
                        else if (densityY is 0)
                        {
                            densityY = densityX;
                        }

                        nativeDensityX = densityX;
                        nativeDensityY = densityY;

                        dpiX = densityX * CentimetersPerInch;
                        dpiY = densityY * CentimetersPerInch;
                        resolutionUnit = JpegResolutionUnit.Centimeters;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (!reader.TrySeek(segmentEnd))
                {
                    return false;
                }

                continue;
            }

            if (marker is 0xE1 && payloadLength > 0)
            {
                int initialHeaderLength = Math.Min(payloadLength, 6);
                Span<byte> initialApp1Header = app1Header[..initialHeaderLength];

                if (!reader.TryReadExactly(initialApp1Header))
                {
                    return false;
                }

                if (initialHeaderLength is 6
                    && BinaryPrimitives.ReadUInt32LittleEndian(initialApp1Header) is ExifSignature
                    && BinaryPrimitives.ReadUInt16LittleEndian(initialApp1Header.Slice(4, 2)) is 0)
                {
                    if (!secondaryResolutionMetadataBlocked
                        && !hasExifMetadata)
                    {
                        hasExifMetadata = true;

                        if (!TryGetExifResolutionMetadata(ref reader, payloadStart + 6, segmentEnd, out ushort? exifResolutionUnit, out double? xResolution, out double? yResolution, out bool hasResolutionMetadata))
                        {
                            return false;
                        }

                        if (hasResolutionMetadata)
                        {
                            ApplyExifResolution(exifResolutionUnit, xResolution, yResolution, ref nativeDensityX, ref nativeDensityY, ref dpiX, ref dpiY, ref resolutionUnit);
                        }
                    }

                    if (!reader.TrySeek(segmentEnd))
                    {
                        return false;
                    }

                    continue;
                }

                int headerLength = Math.Min(payloadLength, app1Header.Length);
                if (headerLength > initialHeaderLength
                    && !reader.TryReadExactly(app1Header[initialHeaderLength..headerLength]))
                {
                    return false;
                }

                ReadOnlySpan<byte> currentApp1Header = app1Header[..headerLength];

                if (IsXmpApp1Header(currentApp1Header)
                    || IsExtendedXmpApp1Header(currentApp1Header))
                {
                    secondaryResolutionMetadataBlocked = true;
                }

                if (!reader.TrySeek(segmentEnd))
                {
                    return false;
                }

                continue;
            }

            if (marker is 0xED && payloadLength >= app13Header.Length)
            {
                if (!reader.TryReadExactly(app13Header))
                {
                    return false;
                }

                if (IsPhotoshopApp13Header(app13Header)
                    && !secondaryResolutionMetadataBlocked
                    && !hasExifMetadata
                    && !hasPhotoshopApp13Metadata)
                {
                    hasPhotoshopApp13Metadata = true;

                    if (!TryGetPhotoshopResolutionInfo(ref reader, segmentEnd, out double app13DpiX, out double app13DpiY, out bool hasResolutionInfo))
                    {
                        return false;
                    }

                    if (hasResolutionInfo)
                    {
                        nativeDensityX = app13DpiX;
                        nativeDensityY = app13DpiY;

                        dpiX = app13DpiX;
                        dpiY = app13DpiY;
                        resolutionUnit = JpegResolutionUnit.Inches;
                    }
                }

                if (!reader.TrySeek(segmentEnd))
                {
                    return false;
                }

                continue;
            }

            if (IsJpegStartOfFrame(marker))
            {
                if (payloadLength < 5 || !reader.TryReadExactly(frameHeader))
                {
                    return false;
                }

                height = BinaryPrimitives.ReadUInt16BigEndian(frameHeader.Slice(1, 2));
                width = BinaryPrimitives.ReadUInt16BigEndian(frameHeader.Slice(3, 2));

                if (width is 0 || height is 0)
                {
                    return false;
                }
            }

            if (!reader.TrySeek(segmentEnd))
            {
                return false;
            }
        }

        return false;
    }

    private static void ApplyExifResolution(ushort? nativeResolutionUnit, double? xResolution, double? yResolution, ref double nativeDensityX, ref double nativeDensityY, ref double dpiX, ref double dpiY, ref JpegResolutionUnit resolutionUnit)
    {
        if (nativeResolutionUnit is not null and not 2 and not 3)
        {
            dpiX = DefaultDpi;
            dpiY = DefaultDpi;
            resolutionUnit = JpegResolutionUnit.Unitless;
            return;
        }

        if (nativeResolutionUnit is null)
        {
            double multiplier;
            if (resolutionUnit is JpegResolutionUnit.Inches)
            {
                multiplier = 1.0;
            }
            else if (resolutionUnit is JpegResolutionUnit.Centimeters)
            {
                multiplier = CentimetersPerInch;
            }
            else
            {
                return;
            }

            if (xResolution is not null)
            {
                nativeDensityX = xResolution.Value;
                dpiX = nativeDensityX * multiplier;
            }

            if (yResolution is not null)
            {
                nativeDensityY = yResolution.Value;
                dpiY = nativeDensityY * multiplier;
            }

            return;
        }

        if (xResolution is not null)
        {
            nativeDensityX = xResolution.Value;
        }

        if (yResolution is not null)
        {
            nativeDensityY = yResolution.Value;
        }

        if (nativeResolutionUnit is 2)
        {
            dpiX = nativeDensityX;
            dpiY = nativeDensityY;
            resolutionUnit = JpegResolutionUnit.Inches;
        }
        else
        {
            dpiX = nativeDensityX * CentimetersPerInch;
            dpiY = nativeDensityY * CentimetersPerInch;
            resolutionUnit = JpegResolutionUnit.Centimeters;
        }
    }

    private static bool TryGetExifResolutionMetadata(ref BufferedImageReader reader, long tiffStart, long segmentEnd, out ushort? resolutionUnit, out double? xResolution, out double? yResolution, out bool hasResolutionMetadata)
    {
        resolutionUnit = null;
        xResolution = null;
        yResolution = null;
        hasResolutionMetadata = false;

        if (segmentEnd - tiffStart < 8
            || !reader.TrySeek(tiffStart))
        {
            return false;
        }

        Span<byte> tiffHeader = stackalloc byte[8];
        if (!reader.TryReadExactly(tiffHeader))
        {
            return false;
        }

        bool littleEndian;
        if (tiffHeader[0] is (byte)'I' && tiffHeader[1] is (byte)'I')
        {
            littleEndian = true;
        }
        else if (tiffHeader[0] is (byte)'M' && tiffHeader[1] is (byte)'M')
        {
            littleEndian = false;
        }
        else
        {
            return false;
        }

        if (ReadExifUInt16(tiffHeader.Slice(2, 2), littleEndian) is not 42)
        {
            return false;
        }

        uint nativeIfdOffset = ReadExifUInt32(tiffHeader.Slice(4, 4), littleEndian);
        long tiffLength = segmentEnd - tiffStart;

        if (nativeIfdOffset > tiffLength - 2
            || !reader.TrySeek(tiffStart + nativeIfdOffset))
        {
            return false;
        }

        Span<byte> entryCountBuffer = stackalloc byte[2];
        if (!reader.TryReadExactly(entryCountBuffer))
        {
            return false;
        }

        int entryCount = ReadExifUInt16(entryCountBuffer, littleEndian);
        if (((long)entryCount * 12) + 4 > segmentEnd - reader.Position)
        {
            return false;
        }

        uint? xResolutionOffset = null;
        uint? yResolutionOffset = null;

        Span<byte> entry = stackalloc byte[12];

        for (int i = 0; i < entryCount; i++)
        {
            if (!reader.TryReadExactly(entry))
            {
                return false;
            }

            ushort tag = ReadExifUInt16(entry[..2], littleEndian);
            if (tag > 0x0128)
            {
                break;
            }

            ushort type = ReadExifUInt16(entry.Slice(2, 2), littleEndian);
            uint count = ReadExifUInt32(entry.Slice(4, 4), littleEndian);

            if (tag is 0x011A)
            {
                hasResolutionMetadata = true;

                if (type is 5 && count is 1)
                {
                    xResolutionOffset = ReadExifUInt32(entry.Slice(8, 4), littleEndian);
                }
            }
            else if (tag is 0x011B)
            {
                hasResolutionMetadata = true;

                if (type is 5 && count is 1)
                {
                    yResolutionOffset = ReadExifUInt32(entry.Slice(8, 4), littleEndian);
                }
            }
            else if (tag is 0x0128)
            {
                hasResolutionMetadata = true;

                if (type is 3 && count is 1)
                {
                    resolutionUnit = ReadExifUInt16(entry.Slice(8, 2), littleEndian);
                }

                break;
            }
        }

        if (xResolutionOffset is not null && TryReadExifRational(ref reader, tiffStart, segmentEnd, xResolutionOffset.Value, littleEndian, out double nativeXResolution))
        {
            xResolution = nativeXResolution;
        }

        if (yResolutionOffset is not null && TryReadExifRational(ref reader, tiffStart, segmentEnd, yResolutionOffset.Value, littleEndian, out double nativeYResolution))
        {
            yResolution = nativeYResolution;
        }

        return true;
    }

    private static bool TryReadExifRational(ref BufferedImageReader reader, long tiffStart, long segmentEnd, uint nativeOffset, bool littleEndian, out double value)
    {
        value = 0.0;

        long tiffLength = segmentEnd - tiffStart;
        if (tiffLength < 8
            || nativeOffset > tiffLength - 8
            || !reader.TrySeek(tiffStart + nativeOffset))
        {
            return false;
        }

        Span<byte> rational = stackalloc byte[8];
        if (!reader.TryReadExactly(rational))
        {
            return false;
        }

        uint numerator = ReadExifUInt32(rational[..4], littleEndian);
        uint denominator = ReadExifUInt32(rational.Slice(4, 4), littleEndian);

        if (denominator is 0)
        {
            return false;
        }

        value = (double)numerator / denominator;
        return true;
    }

    private static bool TryGetPhotoshopResolutionInfo(ref BufferedImageReader reader, long segmentEnd, out double dpiX, out double dpiY, out bool hasResolutionInfo)
    {
        dpiX = 0.0;
        dpiY = 0.0;
        hasResolutionInfo = false;

        Span<byte> resourceHeader = stackalloc byte[6];
        Span<byte> dataLengthBuffer = stackalloc byte[4];
        Span<byte> resolutionInfo = stackalloc byte[16];

        while (reader.Position < segmentEnd)
        {
            if (segmentEnd - reader.Position < resourceHeader.Length
                || !reader.TryReadExactly(resourceHeader)
                || BinaryPrimitives.ReadUInt32LittleEndian(resourceHeader) is not PhotoshopSignature)
            {
                return false;
            }

            ushort resourceId = BinaryPrimitives.ReadUInt16BigEndian(resourceHeader.Slice(4, 2));

            if (!reader.TryReadByte(out byte nameLength))
            {
                return false;
            }

            int paddedNameLength = (nameLength + 2) & ~1;
            int remainingNameLength = paddedNameLength - 1;

            if (remainingNameLength > segmentEnd - reader.Position
                || !reader.TrySkip(remainingNameLength))
            {
                return false;
            }

            if (segmentEnd - reader.Position < dataLengthBuffer.Length
                || !reader.TryReadExactly(dataLengthBuffer))
            {
                return false;
            }

            uint nativeDataLength = BinaryPrimitives.ReadUInt32BigEndian(dataLengthBuffer);
            long paddedDataLength = nativeDataLength + (nativeDataLength & 1);

            if (paddedDataLength > segmentEnd - reader.Position)
            {
                return false;
            }

            if (resourceId is 0x03ED)
            {
                if (nativeDataLength < resolutionInfo.Length
                    || !reader.TryReadExactly(resolutionInfo))
                {
                    return false;
                }

                uint nativeDpiX = BinaryPrimitives.ReadUInt32BigEndian(resolutionInfo[..4]);
                uint nativeDpiY = BinaryPrimitives.ReadUInt32BigEndian(resolutionInfo.Slice(8, 4));

                dpiX = nativeDpiX / 65536.0;
                dpiY = nativeDpiY / 65536.0;

                if (dpiX <= 0.0 || dpiY <= 0.0)
                {
                    return false;
                }

                hasResolutionInfo = true;
                return true;
            }

            if (!reader.TrySkip(paddedDataLength))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadExifUInt16(ReadOnlySpan<byte> value, bool littleEndian)
    {
        return littleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(value)
            : BinaryPrimitives.ReadUInt16BigEndian(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadExifUInt32(ReadOnlySpan<byte> value, bool littleEndian)
    {
        return littleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(value)
            : BinaryPrimitives.ReadUInt32BigEndian(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsJpegStartOfFrame(byte marker)
    {
        return marker is >= 0xC0 and <= 0xCF
            and not 0xC4
            and not 0xC8
            and not 0xCC;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ImageFormat GetImageFormat(ReadOnlySpan<char> extension)
    {
        return extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            ? ImageFormat.Webp
            : extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? ImageFormat.Png
                : extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".jpe", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".jfif", StringComparison.OrdinalIgnoreCase)
                    ? ImageFormat.Jpeg
                    : extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                        ? ImageFormat.Gif
                        : extension.Equals(".ico", StringComparison.OrdinalIgnoreCase)
                            ? ImageFormat.Ico
                            : extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                                || extension.Equals(".dib", StringComparison.OrdinalIgnoreCase)
                                ? ImageFormat.Bmp
                                : ImageFormat.Unknown;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsXmpApp1Header(ReadOnlySpan<byte> header)
    {
        return header.Length >= XmpApp1IdentifierLength
            && BinaryPrimitives.ReadUInt64LittleEndian(header) is 0x6E2F2F3A70747468
            && BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(8, 8)) is 0x2E65626F64612E73
            && BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(16, 8)) is 0x2F7061782F6D6F63
            && BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(24, 4)) is 0x2F302E31
            && header[28] is 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsExtendedXmpApp1Header(ReadOnlySpan<byte> header)
    {
        return header.Length >= ExtendedXmpApp1IdentifierLength
            && BinaryPrimitives.ReadUInt64LittleEndian(header) is 0x6E2F2F3A70747468
            && BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(8, 8)) is 0x2E65626F64612E73
            && BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(16, 8)) is 0x2F706D782F6D6F63
            && BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(24, 8)) is 0x6F69736E65747865
            && BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(32, 2)) is 0x2F6E
            && header[34] is 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPhotoshopApp13Header(ReadOnlySpan<byte> header)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(header) is 0x6F68736F746F6850
            && BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(8, 4)) is 0x2E332070
            && BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(12, 2)) is 0x0030;
    }

    private static bool TrySkipGifSubBlocks(ref BufferedImageReader reader)
    {
        while (reader.TryReadByte(out byte blockSize))
        {
            if (blockSize is 0)
            {
                return true;
            }

            if (!reader.TrySkip(blockSize))
            {
                return false;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ImageInfo CreateImageInfo(string imagePath, int pixelWidth, int pixelHeight, double dpiX, double dpiY)
    {
        return new ImageInfo(imagePath, pixelWidth, pixelHeight, PixelsToDips(pixelWidth, dpiX), PixelsToDips(pixelHeight, dpiY));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double PixelsToDips(int pixels, double dpi)
    {
        float nativeDpi = (float)dpi;

        if (nativeDpi < 0.0F
            || MathF.Abs(nativeDpi) <= 96.0F * InverseFloatMaxPrecision)
        {
            nativeDpi = 96.0F;
        }

        return pixels * (96.0F / nativeDpi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> value)
    {
        return value[0] | (value[1] << 8) | (value[2] << 16);
    }

    private static int ReadUpTo(FileStream fileStream, Span<byte> buffer)
    {
        int bytesRead = 0;

        while (bytesRead < buffer.Length)
        {
            int read = fileStream.Read(buffer[bytesRead..]);
            if (read is 0)
            {
                break;
            }

            bytesRead += read;
        }

        return bytesRead;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadExactly(FileStream fileStream, Span<byte> buffer)
    {
        return fileStream.ReadAtLeast(buffer, buffer.Length, false) == buffer.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FileStream OpenFixedHeaderImageFile(string fullImagePath)
    {
        return new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FileStream OpenMetadataImageFile(string fullImagePath)
    {
        return new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.SequentialScan);
    }
}
