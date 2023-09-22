namespace JL.Core.Profile;
internal sealed class Profile
{
    public string CurrentProfile { get; }
    public List<string> Profiles { get; }

    public Profile(string currentProfile, List<string> profiles)
    {
        CurrentProfile = currentProfile;
        Profiles = profiles;
    }
}
