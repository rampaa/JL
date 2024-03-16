namespace JL.Core.Audio;

internal sealed class AudioResponse(AudioSourceType audioSource, string audioFormat, byte[]? audioData)
{
    public AudioSourceType AudioSource { get; } = audioSource;
    public string AudioFormat { get; } = audioFormat;
    public byte[]? AudioData { get; } = audioData;
}
