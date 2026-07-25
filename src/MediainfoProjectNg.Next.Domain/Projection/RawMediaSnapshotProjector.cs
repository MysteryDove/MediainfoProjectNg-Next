using System.Text.RegularExpressions;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Domain.Projection;

/// <summary>
/// Pure projector: maps a presence-preserving <see cref="RawMediaSnapshot"/> into
/// legacy-friendly display models on <see cref="MediaFileInfo"/>. No P/Invoke.
/// Collation rules must prefer the raw snapshot over these display values.
/// </summary>
public static class RawMediaSnapshotProjector
{
    public static void ProjectDisplayModels(MediaFileInfo info, RawMediaSnapshot raw)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(raw);

        info.RawSnapshot = raw;
        info.GeneralInfo.Format = raw.ContainerFormat.TextOrEmpty;
        info.GeneralInfo.FullPath = raw.FullPath;
        info.GeneralInfo.Filename = Path.GetFileNameWithoutExtension(raw.FullPath);
        info.GeneralInfo.VideoCount = raw.VideoTracks.Count;
        info.GeneralInfo.AudioCount = raw.AudioTracks.Count;
        info.GeneralInfo.TextCount = raw.TextTracks.Count;
        info.GeneralInfo.ChapterCount = raw.ChapterCount;

        info.VideoInfos.Clear();
        info.AudioInfos.Clear();
        info.SubInfos.Clear();
        info.ChapterInfos.Clear();

        foreach (var v in raw.VideoTracks)
        {
            var langDisplay = v.Language.IsAbsent || string.IsNullOrWhiteSpace(v.Language.TextOrEmpty)
                ? "UND"
                : v.Language.TextOrEmpty.ToUpperInvariant();
            var defaultDisplay = IsYes(v.Default) ? "Yes" : "No";
            info.VideoInfos.Add(new VideoInfo(
                format: v.Format.TextOrEmpty,
                formatProfile: v.FormatProfile.TextOrEmpty,
                fpsMode: v.FrameRateMode.TextOrEmpty,
                fps: v.FrameRate.TextOrEmpty.Replace(" FPS", "", StringComparison.Ordinal),
                bitrate: 0,
                bitDepth: v.ParsedBitDepth ?? 0,
                duration: 0,
                height: v.ParsedHeight ?? 0,
                width: v.ParsedWidth ?? 0,
                language: langDisplay,
                delay: 0,
                profile: new ProfileInfo(v.FormatProfile.TextOrEmpty),
                colorSpace: DisplayColorSpace(v),
                isDefault: defaultDisplay));
        }

        foreach (var a in raw.AudioTracks)
        {
            info.AudioInfos.Add(new AudioInfo(
                format: a.Format.TextOrEmpty,
                bitDepth: 0,
                bitrate: 0,
                duration: 0,
                language: a.Language.TextOrEmpty.ToUpperInvariant(),
                delay: 0,
                isDefault: IsYes(a.Default) ? "Yes" : "No"));
        }

        foreach (var t in raw.TextTracks)
        {
            info.SubInfos.Add(new SubInfo(
                format: t.Format.TextOrEmpty,
                isDefault: IsYes(t.Default) ? "Yes" : "No",
                language: t.Language.TextOrEmpty.ToUpperInvariant()));
        }

        foreach (var c in raw.Chapters)
        {
            var language = c.Language.TextOrEmpty;
            if (!string.IsNullOrWhiteSpace(language))
            {
                language = language.ToLowerInvariant() switch
                {
                    "en" => "ENG",
                    "ja" => "JPN",
                    "zh" => "CHI",
                    _ => language.ToUpperInvariant(),
                };
            }

            info.ChapterInfos.Add(new ChapterInfo(
                timespan: c.TimespanMs ?? 0,
                name: c.Name.TextOrEmpty,
                language: language));
        }
    }

    public static string DisplayColorSpace(RawVideoTrack video) =>
        video.ColorSpace.TextOrEmpty.ToUpperInvariant()
        + video.ChromaSubsampling.TextOrEmpty.Replace(":", "", StringComparison.Ordinal);

    public static string GenerateProfileStringFromRaw(RawVideoTrack video)
    {
        var bitDepth = video.ParsedBitDepth ?? 0;
        if (bitDepth != 10)
        {
            return "";
        }

        var format = video.Format.TextOrEmpty;
        var profile = new ProfileInfo(video.FormatProfile.TextOrEmpty).Profile;
        var colorSpace = DisplayColorSpace(video);
        return (format, profile, colorSpace) switch
        {
            ("HEVC", "Main 10", "YUV420") => "Ma10p",
            ("HEVC", "Format Range", "YUV444") => "Ma444-10p",
            ("AVC", "High 4:4:4 Predictive", "YUV420") => "Hi444pp",
            ("AVC", "High 10", "YUV420") => "Hi10p",
            ("AV1", "Main", "YUV420") => "Ma10p",
            ("AV1", "High", "YUV420") => "Hi10p",
            ("AV1", "Professional", "YUV420") => "Pro10p",
            _ => "",
        };
    }

    public static string GenerateVencoderStringFromRaw(RawVideoTrack video) =>
        video.Format.TextOrEmpty switch
        {
            "HEVC" => "x265",
            "AVC" => "x264",
            "AV1" => "svtav1",
            _ => "",
        };

    public static string GenerateAencodersStringFromRaw(IReadOnlyList<RawAudioTrack> tracks)
    {
        var audios = new Dictionary<string, int>(StringComparer.Ordinal);
        var ret = "";
        foreach (var audio in tracks)
        {
            var format = audio.Format.TextOrEmpty;
            if (string.IsNullOrEmpty(format))
            {
                continue;
            }

            if (!audios.TryAdd(format, 1))
            {
                audios[format]++;
            }
        }

        foreach (var key in audios.Keys)
        {
            ret +=
                $"_{(audios[key] > 1 ? audios[key].ToString() : string.Empty)}{Regex.Replace(key, "[^a-zA-Z0-9]+", "", RegexOptions.Compiled).ToLowerInvariant()}";
        }

        return ret;
    }

    public static bool IsPgsFormat(RawField format)
    {
        var f = format.TextOrEmpty;
        return f.Equals("PGS", StringComparison.OrdinalIgnoreCase)
               || f.Equals("HDMV PGS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsYes(RawField field)
    {
        if (!field.IsPresent || field.IsPresentEmpty)
        {
            return false;
        }

        var t = field.TextOrEmpty.Trim().ToLowerInvariant();
        return t is "yes" or "1" or "true";
    }
}
