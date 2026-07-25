using System.Reflection;

namespace MediainfoProjectNg.Next.Core.Presentation;

/// <summary>
/// Product version and main-window title formatting for the desktop host.
/// Prefers <see cref="AssemblyInformationalVersionAttribute"/> (set via MSBuild Version /
/// InformationalVersion), falling back to the assembly version triad.
/// </summary>
public static class AppVersionInfo
{
    public const string AppDisplayName = "mediainfo project ng next";

    public static string GetProductVersion(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Strip SourceLink / build metadata: "1.0.0+abc123" → "1.0.0"
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? informational[..plus] : informational.Trim();
        }

        var version = assembly.GetName().Version;
        if (version is null)
        {
            return "0.0.0";
        }

        // Prefer major.minor.patch over four-part 1.0.0.0 in the title bar.
        return version.Build >= 0
            ? $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}"
            : $"{version.Major}.{version.Minor}.0";
    }

    /// <summary>
    /// Formats the main window title:
    /// <c>mediainfo project ng next 1.0.0  ·  MediaInfo 24.06</c>
    /// </summary>
    public static string FormatWindowTitle(string appVersion, string? mediaInfoLibraryVersion)
    {
        var baseTitle = $"{AppDisplayName} {appVersion}";
        if (string.IsNullOrWhiteSpace(mediaInfoLibraryVersion))
        {
            return $"{baseTitle}  ·  MediaInfo Unavailable";
        }

        var display = mediaInfoLibraryVersion.StartsWith("MediaInfoLib - v", StringComparison.Ordinal)
            ? mediaInfoLibraryVersion["MediaInfoLib - v".Length..]
            : mediaInfoLibraryVersion.Trim();

        return $"{baseTitle}  ·  MediaInfo {display}";
    }
}
