namespace JL.Core.Audio;
internal sealed class AudioResponse
{
    public string AudioFormat { get; }
    public byte[] AudioData { get; }

    public AudioResponse(string audioFormat, byte[] audioData)
    {
        AudioFormat = audioFormat;
        AudioData = audioData;
    }
}
