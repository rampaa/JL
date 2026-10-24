using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport]
[Guid("8BE47B07-57F6-11D2-9EEE-00C04F797396")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechVoiceStatus
{
    [DispId(3)]
    public int LastResult { get; }
}
