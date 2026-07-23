using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Parsing;
using MediainfoProjectNg.Next.MediaInfo.Native;

namespace MediainfoProjectNg.Next.MediaInfo.Projection;

/// <summary>
/// Projects MediaInfo fields into domain models (ports FileInfo constructor field map).
/// </summary>
internal static class MediaInfoProjector
{
    public static MediaFileInfo Project(IntPtr handle, string url)
    {
        MediaInfoNative.Option(handle, "Complete");
        var summary = MediaInfoNative.Inform(handle);

        var general = new GeneralInfo(
            filename: Path.GetFileNameWithoutExtension(url),
            fullPath: url,
            format: MediaInfoNative.Get(handle, StreamKind.General, 0, "Format"),
            bitrate: MediaInfoNative.Get(handle, StreamKind.General, 0, "OverallBitRate").TryParseAsLong() / 1000,
            videoCount: MediaInfoNative.Get(handle, StreamKind.General, 0, "VideoCount").TryParseAsLong(),
            audioCount: MediaInfoNative.Get(handle, StreamKind.General, 0, "AudioCount").TryParseAsLong(),
            textCount: MediaInfoNative.Get(handle, StreamKind.General, 0, "TextCount").TryParseAsLong(),
            chapterCount: -1);

        switch (MediaInfoNative.Get(handle, StreamKind.General, 0, "MenuCount").TryParseAsLong())
        {
            case 0:
                general.ChapterCount = 0;
                break;
            case 1:
                general.ChapterCount =
                    MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_End").TryParseAsLong()
                    - MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_Begin").TryParseAsLong();
                break;
        }

        var info = new MediaFileInfo(general) { Summary = summary };

        for (var i = 0; i < general.VideoCount; i++)
        {
            var colorSpaceRaw = MediaInfoNative.Get(handle, StreamKind.Video, i, "ColorSpace");
            var chromaSubsampling = MediaInfoNative.Get(handle, StreamKind.Video, i, "ChromaSubsampling");
            var colorSpace = colorSpaceRaw.ToUpperInvariant() + chromaSubsampling.Replace(":", "", StringComparison.Ordinal);

            var defaultRaw = MediaInfoNative.Get(handle, StreamKind.Video, i, "Default").ToLowerInvariant();
            var isDefault = defaultRaw is "yes" or "1" ? "Yes" : "No";
            var lang = MediaInfoNative.Get(handle, StreamKind.Video, i, "Language/String3");
            info.VideoInfos.Add(new VideoInfo(
                format: MediaInfoNative.Get(handle, StreamKind.Video, i, "Format"),
                formatProfile: MediaInfoNative.Get(handle, StreamKind.Video, i, "Format_Profile"),
                fpsMode: MediaInfoNative.Get(handle, StreamKind.Video, i, "FrameRate_Mode"),
                fps: MediaInfoNative.Get(handle, StreamKind.Video, i, "FrameRate/String").Replace(" FPS", "", StringComparison.Ordinal),
                bitrate: MediaInfoNative.Get(handle, StreamKind.Video, i, "BitRate").TryParseAsLong() / 1000,
                bitDepth: MediaInfoNative.Get(handle, StreamKind.Video, i, "BitDepth").TryParseAsLong(),
                duration: MediaInfoNative.Get(handle, StreamKind.Video, i, "Duration").TryParseAsLong(),
                height: MediaInfoNative.Get(handle, StreamKind.Video, i, "Height").TryParseAsLong(),
                width: MediaInfoNative.Get(handle, StreamKind.Video, i, "Width").TryParseAsLong(),
                language: string.IsNullOrWhiteSpace(lang) ? "UND" : lang.ToUpperInvariant(),
                delay: MediaInfoNative.Get(handle, StreamKind.Video, i, "Delay").TryParseAsLong(),
                profile: new ProfileInfo(MediaInfoNative.Get(handle, StreamKind.Video, i, "Format_Profile")),
                colorSpace: colorSpace,
                isDefault: isDefault));
        }

        for (var i = 0; i < general.AudioCount; i++)
        {
            var defaultRaw = MediaInfoNative.Get(handle, StreamKind.Audio, i, "Default").ToLowerInvariant();
            var isDefault = defaultRaw is "yes" or "1" ? "Yes" : "No";
            info.AudioInfos.Add(new AudioInfo(
                format: MediaInfoNative.Get(handle, StreamKind.Audio, i, "Format"),
                bitDepth: MediaInfoNative.Get(handle, StreamKind.Audio, i, "BitDepth").TryParseAsLong(),
                bitrate: MediaInfoNative.Get(handle, StreamKind.Audio, i, "BitRate").TryParseAsLong() / 1000,
                duration: MediaInfoNative.Get(handle, StreamKind.Audio, i, "Duration").TryParseAsLong(),
                language: MediaInfoNative.Get(handle, StreamKind.Audio, i, "Language/String3").ToUpperInvariant(),
                delay: MediaInfoNative.Get(handle, StreamKind.Audio, i, "Delay").TryParseAsLong(),
                isDefault: isDefault));
        }

        for (var i = 0; i < general.TextCount; i++)
        {
            var defaultRaw = MediaInfoNative.Get(handle, StreamKind.Text, i, "Default").ToLowerInvariant();
            var isDefault = defaultRaw is "yes" or "1" ? "Yes" : "No";
            info.SubInfos.Add(new SubInfo(
                format: MediaInfoNative.Get(handle, StreamKind.Text, i, "Format"),
                isDefault: isDefault,
                language: MediaInfoNative.Get(handle, StreamKind.Text, i, "Language/String3").ToUpperInvariant()));
        }

        if (general.ChapterCount > 0)
        {
            var chapPosBegin = (int)MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_Begin").TryParseAsLong();
            var chapPosEnd = (int)MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_End").TryParseAsLong();
            for (var i = chapPosBegin; i < chapPosEnd; i++)
            {
                var name = MediaInfoNative.GetByIndex(handle, StreamKind.Menu, 0, i, InfoKind.Text);
                var language = "";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var idx = name.IndexOf(':');
                    if (idx > 0)
                    {
                        language = name[..idx].Trim();
                        language = language.ToLowerInvariant() switch
                        {
                            "en" => "ENG",
                            "ja" => "JPN",
                            "zh" => "CHI",
                            _ => language.ToUpperInvariant()
                        };
                    }
                }

                info.ChapterInfos.Add(new ChapterInfo(
                    timespan: MediaInfoNative.GetByIndex(handle, StreamKind.Menu, 0, i, InfoKind.Name).TryParseAsMillisecond(),
                    name: name,
                    language: language));
            }
        }

        return info;
    }
}
