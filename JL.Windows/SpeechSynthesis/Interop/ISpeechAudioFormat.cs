using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport, Guid("E6E9C590-3E18-40E3-8299-061F98BDE7C7"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechAudioFormat
{
    [DispId(1)]
    public int Type { get; set; }
    [DispId(3)]
    public ISpeechWaveFormat ReadWaveFormat();
    [DispId(4)]
    public void SetWaveFormat(ISpeechWaveFormat format);
}
