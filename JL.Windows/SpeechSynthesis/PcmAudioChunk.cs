namespace JL.Windows.SpeechSynthesis;

internal readonly record struct PcmAudioChunk(nint Buffer, int ByteCount);
