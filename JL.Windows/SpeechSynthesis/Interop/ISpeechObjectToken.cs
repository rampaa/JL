using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport]
[Guid("C74A3ADC-B727-4500-A84A-B526721C8B8C")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechObjectToken
{
    [DispId(7)]
    [return: MarshalAs(UnmanagedType.IUnknown)]
    public object CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object? outer, int context);
    [DispId(6)]
    [return: MarshalAs(UnmanagedType.BStr)]
    public string GetAttribute([MarshalAs(UnmanagedType.BStr)] string name);
}
