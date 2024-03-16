namespace JL.Core.Audio;

public sealed class AudioSource(AudioSourceType type, bool active, int priority)
{
    public AudioSourceType Type { get; } = type;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;
}
