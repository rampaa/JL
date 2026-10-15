using System.Runtime.InteropServices;

namespace JL.Windows.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct BitmapInfoHeader
{
    public readonly uint Size;
    public readonly int Width;
    public readonly int Height;
    public readonly ushort Planes;
    public readonly ushort BitCount;
    public readonly uint Compression;
    public readonly uint SizeImage;
    public readonly int XPelsPerMeter;
    public readonly int YPelsPerMeter;
    public readonly uint ColorsUsed;
    public readonly uint ColorsImportant;

    public BitmapInfoHeader(int width, int height)
    {
        Size = 40;
        Width = width;
        // A negative height stores rows from top to bottom, matching Bitmap's positive stride.
        Height = -height;
        Planes = 1;
        BitCount = 24;
    }
}
