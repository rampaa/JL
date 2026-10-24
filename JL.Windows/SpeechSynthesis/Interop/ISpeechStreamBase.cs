using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport]
[Guid("6450336F-7D49-4CED-8097-49D6DEE37294")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechStreamBase
{
    [DispId(1)]
    public ISpeechAudioFormat Format { get; }
}
