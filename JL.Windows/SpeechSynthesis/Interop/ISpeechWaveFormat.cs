using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport]
[Guid("7A1EF0D5-1581-4741-88E4-209A49F11A10")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechWaveFormat
{
    [DispId(1)]
    public short FormatTag { get; set; }
    [DispId(2)]
    public short Channels { get; set; }
    [DispId(3)]
    public int SamplesPerSec { get; set; }
    [DispId(4)]
    public int AvgBytesPerSec { get; set; }
    [DispId(5)]
    public short BlockAlign { get; set; }
    [DispId(6)]
    public short BitsPerSample { get; set; }
}
