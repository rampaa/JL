using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct TransformOutputInfo(uint Flags, uint BufferSizeBytes, uint BufferAlignmentBytes);
