namespace JL.Core.Profile;

internal sealed class Profile(string currentProfile, List<string> profiles)
{
    public string CurrentProfile { get; } = currentProfile;
    public List<string> Profiles { get; } = profiles;
}
