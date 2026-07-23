namespace MediainfoProjectNg.Next.Domain.Models;

public sealed class ProfileInfo
{
    public string? Profile { get; }
    public string? Level { get; }

    public ProfileInfo(string profileString)
    {
        var parts = profileString.Split('@');
        Profile = parts.Length > 0 ? parts[0].Trim() : null;
        Level = parts.Length > 1 ? parts[1].Trim() : null;
    }

    public ProfileInfo(string? profile, string? level)
    {
        Profile = profile;
        Level = level;
    }
}
