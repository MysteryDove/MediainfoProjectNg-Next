using System.Text.RegularExpressions;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Domain.Validation;

/// <summary>
/// Ports legacy Utils.CheckFile / FileNameContentMatched / generators.
/// Messages and thresholds preserved from ../mpng.
/// </summary>
public static class MediaValidator
{
    private static readonly string[] Matroska = [".mkv", ".mka", ".mks"];
    private static readonly string[] Mpeg4 = [".mp4", ".m4a", ".m4v"];

    public static IReadOnlyList<ValidationFinding> CheckFile(MediaFileInfo info)
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
                "内容物和文件名描述不符。"));
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
