using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Core.Presentation;

/// <summary>
/// Pure presentation rules ported from mpng converters and CheckFile brush assignments.
/// Row background uses the <b>first</b> finding (legacy InfoToBackgroundConverter), not worst severity.
/// Structured Collation filename rule IDs map to <see cref="ColorToken.ErrorViolet"/>.
/// </summary>
public static class LegacyColorRules
{
    /// <summary>Legacy FirstOrDefault on CheckFile results → row background token.</summary>
    public static ColorToken FirstFindingBackgroundToken(IReadOnlyList<ValidationFinding> findings)
    {
        if (findings.Count == 0)
        {
            return ColorToken.None;
        }

        return TokenForFinding(findings[0]);
    }

    public static ColorToken TokenForFinding(ValidationFinding finding)
    {
        // Structured findings use the same category colors as the filter strip.
        if (finding.RuleId is not null)
        {
            var category = IssueCategoryRegistry.CategoryForRuleId(finding.RuleId);
            return category switch
            {
                IssueCategory.ContainerNaming => ColorToken.ErrorViolet,
                IssueCategory.Track => ColorToken.WarningDelayTeal,
                IssueCategory.FrameRate => ColorToken.FpsNtsc,
                IssueCategory.VideoColor => ColorToken.ColorSpaceNon420,
                IssueCategory.Chapter => ColorToken.WarningYellow,
                _ => TokenForSeverity(finding.Level),
            };
        }

        var d = finding.Description;
        if (LegacyFindingMatchers.IsExtensionMismatch(d))
        {
            return ColorToken.ErrorRed;
        }

        if (LegacyFindingMatchers.IsDelay(d))
        {
            return ColorToken.WarningDelayTeal;
        }

        if (LegacyFindingMatchers.IsDuration(d))
        {
            return ColorToken.WarningPaleVioletRed;
        }

        if (LegacyFindingMatchers.IsChapterLegacy(d))
        {
            return ColorToken.WarningYellow;
        }

        if (LegacyFindingMatchers.IsFilenameMismatch(d))
        {
            return ColorToken.ErrorViolet;
        }

        if (LegacyFindingMatchers.IsMultiAudio(d))
        {
            return ColorToken.InfoGreenYellow;
        }

        // Fallback by severity if description unknown
        return TokenForSeverity(finding.Level);
    }

    private static ColorToken TokenForSeverity(ErrorLevel level) =>
        level switch
        {
            ErrorLevel.Error => ColorToken.ErrorRed,
            ErrorLevel.Warning => ColorToken.WarningYellow,
            ErrorLevel.Info => ColorToken.InfoGreenYellow,
            _ => ColorToken.None
        };

    /// <summary>Legacy InfoToForegroundConverter signal: TextCount &gt; 1.</summary>
    public static ColorToken RowForegroundToken(long textCount) =>
        textCount > 1 ? ColorToken.ForegroundMultiSub : ColorToken.None;

    /// <summary>Legacy FpsModeToTextConverter.</summary>
    public static string FpsDisplayText(VideoInfo? video)
    {
        if (video is null)
        {
            return string.Empty;
        }

        return video.FpsMode == "VFR" ? "VFR" : video.Fps;
    }

    /// <summary>Legacy FpsToTextColorConverter.</summary>
    public static ColorToken FpsColorToken(VideoInfo? video)
    {
        if (video is null)
        {
            return ColorToken.None;
        }

        if (video.FpsMode == "VFR")
        {
            return ColorToken.FpsVfr;
        }

        return video.Fps switch
        {
            "23.976 (24000/1001)" => ColorToken.None,
            "29.970 (30000/1001)" or "59.940 (60000/1001)" => ColorToken.FpsNtsc,
            "23.976 (23976/1000)" or "29.970 (29970/1000)" => ColorToken.FpsRounded,
            _ => ColorToken.FpsOther
        };
    }

    /// <summary>Legacy ColorSpaceToColorConverter: only YUV420 is default.</summary>
    public static ColorToken ColorSpaceColorToken(VideoInfo? video)
    {
        if (video is null)
        {
            return ColorToken.None;
        }

        return video.ColorSpace == "YUV420" ? ColorToken.None : ColorToken.ColorSpaceNon420;
    }

    /// <summary>Legacy UnifiedLanguageConverter.</summary>
    public static string ChapterLanguageDisplay(IReadOnlyList<ChapterInfo> chapters)
    {
        if (chapters.Count == 0)
        {
            return string.Empty;
        }

        var firstLang = chapters[0].Language ?? string.Empty;
        var allSame = chapters.All(c =>
            string.Equals(c.Language, firstLang, StringComparison.OrdinalIgnoreCase));
        return allSame ? firstLang : string.Empty;
    }

    /// <summary>
    /// Legacy ChapterLanguageToColorConverter: yellow when multiple distinct langs or single empty lang.
    /// </summary>
    public static ColorToken ChapterLanguageBackgroundToken(IReadOnlyList<ChapterInfo> chapters)
    {
        if (chapters.Count == 0)
        {
            return ColorToken.None;
        }

        var langs = chapters
            .Select(c => c.Language ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hasIssue = langs.Count > 1 || (langs.Count == 1 && langs[0] == string.Empty);
        return hasIssue ? ColorToken.WarningYellow : ColorToken.None;
    }
}
