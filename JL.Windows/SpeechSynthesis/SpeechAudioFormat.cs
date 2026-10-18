namespace JL.Windows.SpeechSynthesis;

// Each frame contains one audio sample from every channel.
internal readonly record struct SpeechAudioFormat(short EncodingId, short ChannelCount, int SamplesPerSecond,
    int BytesPerSecond, short BytesPerFrame, short BitsPerSample);
