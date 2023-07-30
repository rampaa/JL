namespace JL.Core.Audio;

internal sealed class AudioResponse
{
    public AudioSourceType AudioSource { get; }
    public string AudioFormat { get; }
    public byte[]? AudioData { get; }

    public AudioResponse(AudioSourceType audioSource, string audioFormat, byte[]? audioData)
    {
        AudioSource = audioSource;
        AudioFormat = audioFormat;
        AudioData = audioData;
    }
}
