using System.Text.RegularExpressions;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Domain.Validation;

/// <summary>
/// Ports legacy Utils.CheckFile / FileNameContentMatched / generators.
/// Messages and thresholds preserved from ../mpng.
/// CollationV1 merges structured evaluations via Algorithm A without changing
/// parameterless <see cref="CheckFile(MediaFileInfo)"/> behaviour.
/// </summary>
public static class MediaValidator
{
    private static readonly string[] Matroska = [".mkv", ".mka", ".mks"];
    private static readonly string[] Mpeg4 = [".mp4", ".m4a", ".m4v"];

    /// <summary>
    /// LegacyV1 entry point. Exact findings, text, order, and early-return behaviour.
    /// </summary>
    public static IReadOnlyList<ValidationFinding> CheckFile(MediaFileInfo info) =>
        CheckFileLegacyCore(info);

    /// <summary>
    /// Profile-aware entry point. Omitted profile / LegacyV1 preserves parameterless behaviour.
    /// </summary>
    public static IReadOnlyList<ValidationFinding> CheckFile(MediaFileInfo info, ValidationProfile profile)
    {
        if (profile == ValidationProfile.LegacyV1)
        {
            return CheckFileLegacyCore(info);
        }

        return MergeCollation(info);
    }

    private static List<ValidationFinding> CheckFileLegacyCore(MediaFileInfo info)
    {
        var ret = new List<ValidationFinding>();
        var extension = Path.GetExtension(info.GeneralInfo.FullPath);

        if (info.GeneralInfo.Format == "Matroska" && !Matroska.Contains(extension)
            || info.GeneralInfo.Format == "MPEG-4" && !Mpeg4.Contains(extension))
        {
            ret.Add(new ValidationFinding(
                ErrorLevel.Error,
                $"文件后缀和与容器不符。后缀：{extension}，容器{info.GeneralInfo.Format}"));
        }

        if (info.VideoInfos.Any(o => o.Delay != 0) || info.AudioInfos.Any(o => o.Delay != 0))
        {
            ret.Add(new ValidationFinding(
                ErrorLevel.Warning,
                "容器中含有延时非 0 的轨道。"));
        }

        var duration = new List<long>();
        duration.AddRange(info.VideoInfos.Select(videoInfo => videoInfo.Duration));
        duration.AddRange(info.AudioInfos.Select(audioInfo => audioInfo.Duration));
        if (duration.Count == 0)
        {
            return ret;
        }

        if (duration.Max() - duration.Min() > 600)
        {
            ret.Add(new ValidationFinding(
                ErrorLevel.Warning,
                "轨道间长度相差过大。"));
        }

        if (info.GeneralInfo.ChapterCount != 0)
        {
            if (info.GeneralInfo.ChapterCount == 1)
            {
                ret.Add(new ValidationFinding(
                    ErrorLevel.Warning,
                    "文件只有一个章节。"));
            }
            else if (info.GeneralInfo.ChapterCount == -1)
            {
                ret.Add(new ValidationFinding(
                    ErrorLevel.Warning,
                    "文件存在多组章节。"));
            }
            else if (info.ChapterInfos.Count > 0 && info.ChapterInfos.Last().Timespan > duration.Max() - 1100)
            {
                ret.Add(new ValidationFinding(
                    ErrorLevel.Warning,
                    "文件末尾有无用章节。"));
            }
            else if (info.ChapterInfos.Count > 0 && info.ChapterInfos.First().Timespan != 0)
            {
                ret.Add(new ValidationFinding(
                    ErrorLevel.Warning,
                    "首个章节时间戳非零。"));
            }
        }

        if (!FileNameContentMatched(info))
        {
            ret.Add(new ValidationFinding(
                ErrorLevel.Error,
                CollationPolicyMatrix.LegacyFilenameMismatchDescription));
        }

        if (info.AudioInfos.Count > 2)
        {
            ret.Add(new ValidationFinding(
                ErrorLevel.Info,
                "文件含有多条音轨。"));
        }

        return ret;
    }

    /// <summary>
    /// Algorithm A: compute Legacy stream first (including empty-duration early return),
    /// then merge Collation evaluations with supersession and append rules.
    /// </summary>
    private static IReadOnlyList<ValidationFinding> MergeCollation(MediaFileInfo info)
    {
        var legacy = CheckFileLegacyCore(info);
        var evaluations = CollationEvaluator.Evaluate(info);

        // PRD: filename Violation/Error before Unverifiable/Info siblings so mixed
        // results retain ErrorViolet first-finding emphasis; then matrix rule order.
        var filenameEvals = evaluations
            .Where(e => CollationRuleIds.FilenameRuleOrder.Contains(e.RuleId) && e.IsVisibleFinding)
            .OrderBy(e => e.Outcome == RuleOutcome.Violation ? 0 : 1)
            .ThenBy(e => e.Severity == ErrorLevel.Error ? 0 : e.Severity == ErrorLevel.Warning ? 1 : 2)
            .ThenBy(e => FilenameOrderIndex(e.RuleId))
            .ToList();

        var otherEvals = evaluations
            .Where(e => !CollationRuleIds.FilenameRuleOrder.Contains(e.RuleId) && e.IsVisibleFinding)
            .OrderBy(e => EnabledOrderIndex(e.RuleId))
            .ToList();

        var merged = new List<ValidationFinding>();
        var genericIndex = legacy.ToList().FindIndex(f =>
            f.Description == CollationPolicyMatrix.LegacyFilenameMismatchDescription
            && f.RuleId is null);

        var recognized = CollationFilenameParser.IsRecognizedVcbsMkv(info);
        var insertedFilename = false;

        for (var i = 0; i < legacy.Count; i++)
        {
            if (recognized && i == genericIndex && genericIndex >= 0)
            {
                // Supersede generic once; insert field-specific findings at the slot.
                foreach (var eval in filenameEvals)
                {
                    merged.Add(ValidationFinding.FromEvaluation(eval));
                }

                insertedFilename = true;
                continue;
            }

            merged.Add(legacy[i]);
        }

        if (recognized && !insertedFilename && filenameEvals.Count > 0)
        {
            // No generic filename slot (match or early return): append filename findings.
            foreach (var eval in filenameEvals)
            {
                merged.Add(ValidationFinding.FromEvaluation(eval));
            }
        }

        foreach (var eval in otherEvals)
        {
            merged.Add(ValidationFinding.FromEvaluation(eval));
        }

        return merged;
    }

    private static int FilenameOrderIndex(string ruleId)
    {
        for (var i = 0; i < CollationRuleIds.FilenameRuleOrder.Count; i++)
        {
            if (CollationRuleIds.FilenameRuleOrder[i] == ruleId)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static int EnabledOrderIndex(string ruleId)
    {
        for (var i = 0; i < CollationRuleIds.EnabledOrder.Count; i++)
        {
            if (CollationRuleIds.EnabledOrder[i] == ruleId)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    /// <summary>
    /// Returns true when filename does not claim VCB-S structure, or when claims match media.
    /// Returns false on mismatch. Safe when VideoInfos empty (V1 §14: no throw → mismatch).
    /// </summary>
    public static bool FileNameContentMatched(MediaFileInfo info)
    {
        var filenameReg = new Regex(
            @"^\[[^\[\]]*VCB\-S(?:tudio)?[^\[\]]*\] [^\[\]]+ (?:\[[^\[\]]*\d*\])?\[(?:(?<profile>.*?)_)?(?<resolution>.*?)\]\[(?<vencoder>.*?)(?<aencoders>(?:_\d*.*?)*)\]\.mkv$",
            RegexOptions.Compiled);

        var match = filenameReg.Match(Path.GetFileName(info.GeneralInfo.FullPath));
        if (!match.Success)
        {
            return true;
        }

        // V1 policy: empty video after VCB-S name match → mismatch (no IndexOutOfRangeException)
        if (info.VideoInfos.Count == 0)
        {
            return false;
        }

        var profile = GenerateProfileString(
            info.VideoInfos[0].Profile,
            info.VideoInfos[0].Format,
            info.VideoInfos[0].BitDepth,
            info.VideoInfos[0].ColorSpace);
        if (match.Groups["profile"].Value != "" && profile == "")
        {
            return true;
        }

        var vencoder = GenerateVencoderString(info.VideoInfos[0]);
        if (vencoder == "")
        {
            return true;
        }

        return match.Groups["profile"].Value == profile
               && match.Groups["vencoder"].Value == vencoder
               && match.Groups["aencoders"].Value == GenerateAencodersString(info.AudioInfos);
    }

    public static string GenerateProfileString(ProfileInfo info, string format, long bitDepth, string colorSpace)
    {
        if (bitDepth != 10)
        {
            return "";
        }

        return (format, info.Profile, colorSpace) switch
        {
            ("HEVC", "Main 10", "YUV420") => "Ma10p",
            ("HEVC", "Format Range", "YUV444") => "Ma444-10p",
            ("AVC", "High 4:4:4 Predictive", "YUV420") => "Hi444pp",
            ("AVC", "High 10", "YUV420") => "Hi10p",
            ("AV1", "Main", "YUV420") => "Ma10p",
            ("AV1", "High", "YUV420") => "Hi10p",
            ("AV1", "Professional", "YUV420") => "Pro10p",
            _ => ""
        };
    }

    public static string GenerateVencoderString(VideoInfo info) =>
        info.Format switch
        {
            "HEVC" => "x265",
            "AVC" => "x264",
            "AV1" => "svtav1",
            _ => ""
        };

    public static string GenerateAencodersString(IReadOnlyList<AudioInfo> infos)
    {
        var audios = new Dictionary<string, int>();
        var ret = "";
        foreach (var audioInfo in infos)
        {
            if (!audios.TryAdd(audioInfo.Format, 1))
            {
                audios[audioInfo.Format]++;
            }
        }

        foreach (var key in audios.Keys)
        {
            ret +=
                $"_{(audios[key] > 1 ? audios[key].ToString() : string.Empty)}{Regex.Replace(key, "[^a-zA-Z0-9]+", "", RegexOptions.Compiled).ToLower()}";
        }

        return ret;
    }
}
