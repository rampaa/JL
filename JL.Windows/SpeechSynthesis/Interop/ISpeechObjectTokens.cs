using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport]
[Guid("9285B776-2E7B-4BC0-B53E-580EB6FA967F")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechObjectTokens
{
    [DispId(1)]
    public int Count { get; }
    [DispId(0)]
    public ISpeechObjectToken Item(int index);
}
