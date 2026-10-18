using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport, Guid("269316D8-57BD-11D2-9EEE-00C04F797396"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechVoice
{
    [DispId(1)]
    public ISpeechVoiceStatus Status { get; }
    [DispId(20)]
    public int SpeakCompleteEvent();
    [DispId(2)]
    public ISpeechObjectToken Voice { get; [param: MarshalAs(UnmanagedType.Interface)] set; }
    [DispId(4)]
    public ISpeechStreamBase AudioOutputStream { get; [param: MarshalAs(UnmanagedType.Interface)] set; }
    [DispId(12)]
    public int Speak([MarshalAs(UnmanagedType.BStr)] string text, int flags);
    [DispId(17)]
    public ISpeechObjectTokens GetVoices([MarshalAs(UnmanagedType.BStr)] string required, [MarshalAs(UnmanagedType.BStr)] string optionalAttributes);
}
