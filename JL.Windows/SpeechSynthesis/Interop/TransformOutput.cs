using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct TransformOutput
{
    private readonly uint _streamId;
    public nint Sample { get; }
    private readonly uint _status;
    public nint Events { get; }

    public TransformOutput(nint sample)
    {
        _streamId = 0;
        Sample = sample;
        _status = 0;
        Events = 0;
    }
}
