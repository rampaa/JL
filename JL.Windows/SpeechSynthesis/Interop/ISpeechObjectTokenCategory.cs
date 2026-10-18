using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[ComImport, Guid("CA7EAC50-2D01-4145-86D4-5AE7D70F4469"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface ISpeechObjectTokenCategory
{
    [DispId(3)]
    public void SetId([MarshalAs(UnmanagedType.BStr)] string id, [MarshalAs(UnmanagedType.VariantBool)] bool create);
    [DispId(5)]
    public ISpeechObjectTokens EnumerateTokens([MarshalAs(UnmanagedType.BStr)] string required, [MarshalAs(UnmanagedType.BStr)] string optionalAttributes);
}
