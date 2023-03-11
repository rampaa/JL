namespace JL.Core.Audio;
public sealed class AudioSource
{
    public AudioSourceType Type { get; }
    public bool Active { get; set; }
    public int Priority { get; set; }
    public AudioSource(AudioSourceType type, bool active, int priority)
    {
        Type = type;
        Active = active;
        Priority = priority;
    }
}
