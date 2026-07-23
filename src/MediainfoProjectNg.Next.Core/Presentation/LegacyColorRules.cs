using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Core.Presentation;

/// <summary>
/// Pure presentation rules ported from mpng converters and CheckFile brush assignments.
/// Row background uses the <b>first</b> finding (legacy InfoToBackgroundConverter), not worst severity.
/// </summary>
public static class LegacyColorRules
{
    // Exact description strings from MediaValidator / Utils.CheckFile
    private const string DescExtMismatchPrefix = "文件后缀和与容器不符";
    private const string DescDelay = "容器中含有延时非 0 的轨道。";
    private const string DescDuration = "轨道间长度相差过大。";
    private const string DescSingleChapter = "文件只有一个章节。";
    private const string DescMultiChapterSets = "文件存在多组章节。";
    private const string DescUselessChapter = "文件末尾有无用章节。";
    private const string DescFirstChapter = "首个章节时间戳非零。";
    private const string DescFilenameMismatch = "内容物和文件名描述不符。";
    private const string DescMultiAudio = "文件含有多条音轨。";

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
        var d = finding.Description;
        if (d.StartsWith(DescExtMismatchPrefix, StringComparison.Ordinal))
        {
            return ColorToken.ErrorRed;
        }

        if (d == DescDelay)
        {
            return ColorToken.WarningDelayTeal;
        }

        if (d == DescDuration)
        {
            return ColorToken.WarningPaleVioletRed;
        }

        if (d is DescSingleChapter or DescMultiChapterSets or DescUselessChapter or DescFirstChapter)
        {
            return ColorToken.WarningYellow;
        }

        if (d == DescFilenameMismatch)
        {
            return ColorToken.ErrorViolet;
        }

        if (d == DescMultiAudio)
        {
            return ColorToken.InfoGreenYellow;
        }

        // Fallback by severity if description unknown
        return finding.Level switch
        {
            ErrorLevel.Error => ColorToken.ErrorRed,
            ErrorLevel.Warning => ColorToken.WarningYellow,
            ErrorLevel.Info => ColorToken.InfoGreenYellow,
            _ => ColorToken.None
        };
    }

    /// <summary>Legacy InfoToForegroundConverter: TextCount &gt; 1 → blue.</summary>
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
