using System.Text.RegularExpressions;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Domain.Validation;

public sealed class CollationFilenameClaim
{
    public required string Profile { get; init; }
    public required string Resolution { get; init; }
    public required string VideoEncoder { get; init; }
    public required string AudioEncoders { get; init; }
    public required string FileName { get; init; }
}

/// <summary>
/// Separate Collation filename parser. Does not rewrite the legacy Boolean matcher.
/// Unrecognized names are NotApplicable rather than failures.
/// </summary>
public static class CollationFilenameParser
{
    private static readonly Regex VcbsMkv = new(
        @"^\[[^\[\]]*VCB\-S(?:tudio)?[^\[\]]*\] [^\[\]]+ (?:\[[^\[\]]*\d*\])?\[(?:(?<profile>.*?)_)?(?<resolution>.*?)\]\[(?<vencoder>.*?)(?<aencoders>(?:_\d*.*?)*)\]\.mkv$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AudioGroups = new(
        @"^(?:_\d*[a-z0-9]+)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Projected from matrix filenameGrammarAllowlists (dual-write tested).</summary>
    public static readonly IReadOnlySet<string> SupportedProfiles =
        new HashSet<string>(StringComparer.Ordinal) { "", "Ma10p", "Ma444-10p", "Hi444pp", "Hi10p" };

    /// <summary>Projected from matrix filenameGrammarAllowlists (dual-write tested).</summary>
    public static readonly IReadOnlySet<string> SupportedVideoEncoders =
        new HashSet<string>(StringComparer.Ordinal) { "x264", "x265" };

    public static bool TryParse(string fullPathOrFileName, out CollationFilenameClaim? claim)
    {
        var fileName = Path.GetFileName(fullPathOrFileName);
        var match = VcbsMkv.Match(fileName);
        if (!match.Success)
        {
            claim = null;
            return false;
        }

        var profile = match.Groups["profile"].Value;
        var resolution = match.Groups["resolution"].Value;
        var videoEncoder = match.Groups["vencoder"].Value;
        var audioEncoders = match.Groups["aencoders"].Value;
        if (!SupportedProfiles.Contains(profile)
            || !CollationPolicyMatrix.ResolutionBuckets.ContainsKey(resolution)
            || !SupportedVideoEncoders.Contains(videoEncoder)
            || !AudioGroups.IsMatch(audioEncoders))
        {
            claim = null;
            return false;
        }

        claim = new CollationFilenameClaim
        {
            Profile = profile,
            Resolution = resolution,
            VideoEncoder = videoEncoder,
            AudioEncoders = audioEncoders,
            FileName = fileName,
        };
        return true;
    }

    public static bool IsRecognizedVcbsMkv(MediaFileInfo info) =>
        TryParse(info.GeneralInfo.FullPath, out _);
}
