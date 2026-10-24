using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport]
[Guid("1A9E9F4F-104F-4DB8-A115-EFD7FD0C97AE")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechStreamOutput : ISpeechStreamBase
{
    [DispId(100)]
    public object BaseStream { [return: MarshalAs(UnmanagedType.IUnknown)] get; [param: MarshalAs(UnmanagedType.IUnknown)] set; }
}
