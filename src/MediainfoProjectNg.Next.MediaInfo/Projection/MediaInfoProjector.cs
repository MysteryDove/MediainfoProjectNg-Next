using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Parsing;
using MediainfoProjectNg.Next.Domain.Projection;
using MediainfoProjectNg.Next.MediaInfo.Native;

namespace MediainfoProjectNg.Next.MediaInfo.Projection;

/// <summary>
/// Native adapter: collects a presence-preserving <see cref="RawMediaSnapshot"/> via P/Invoke,
/// then delegates display-model projection to the pure <see cref="RawMediaSnapshotProjector"/>.
/// </summary>
internal static class MediaInfoProjector
{
    public static MediaFileInfo Project(IntPtr handle, string url)
    {
        MediaInfoNative.Option(handle, "Complete");
        var summary = MediaInfoNative.Inform(handle);

        var videoCount = MediaInfoNative.Get(handle, StreamKind.General, 0, "VideoCount").TryParseAsLong();
        var audioCount = MediaInfoNative.Get(handle, StreamKind.General, 0, "AudioCount").TryParseAsLong();
        var textCount = MediaInfoNative.Get(handle, StreamKind.General, 0, "TextCount").TryParseAsLong();
        var format = MediaInfoNative.Get(handle, StreamKind.General, 0, "Format");
        var bitrate = MediaInfoNative.Get(handle, StreamKind.General, 0, "OverallBitRate").TryParseAsLong() / 1000;

        long chapterCount = -1;
        switch (MediaInfoNative.Get(handle, StreamKind.General, 0, "MenuCount").TryParseAsLong())
        {
            case 0:
                chapterCount = 0;
                break;
            case 1:
                chapterCount =
                    MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_End").TryParseAsLong()
                    - MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_Begin").TryParseAsLong();
                break;
        }

        var rawVideos = new List<RawVideoTrack>();
        for (var i = 0; i < videoCount; i++)
        {
            var widthRaw = GetRaw(handle, StreamKind.Video, i, "Width").ParseLong(out var parsedWidth);
            var heightRaw = GetRaw(handle, StreamKind.Video, i, "Height").ParseLong(out var parsedHeight);
            var bitDepthRaw = GetRaw(handle, StreamKind.Video, i, "BitDepth").ParseLong(out var parsedBitDepth);

            var colourRange = GetRaw(handle, StreamKind.Video, i, "colour_range");
            if (colourRange.IsAbsent)
            {
                colourRange = GetRaw(handle, StreamKind.Video, i, "ColorRange");
            }

            var matrix = GetRaw(handle, StreamKind.Video, i, "matrix_coefficients");
            if (matrix.IsAbsent)
            {
                matrix = GetRaw(handle, StreamKind.Video, i, "MatrixCoefficients");
            }

            var primaries = GetRaw(handle, StreamKind.Video, i, "colour_primaries");
            if (primaries.IsAbsent)
            {
                primaries = GetRaw(handle, StreamKind.Video, i, "ColorPrimaries");
            }

            var transfer = GetRaw(handle, StreamKind.Video, i, "transfer_characteristics");
            if (transfer.IsAbsent)
            {
                transfer = GetRaw(handle, StreamKind.Video, i, "TransferCharacteristics");
            }

            rawVideos.Add(new RawVideoTrack
            {
                Format = GetRaw(handle, StreamKind.Video, i, "Format"),
                FormatProfile = GetRaw(handle, StreamKind.Video, i, "Format_Profile"),
                Width = widthRaw,
                Height = heightRaw,
                BitDepth = bitDepthRaw,
                ColorSpace = GetRaw(handle, StreamKind.Video, i, "ColorSpace"),
                ChromaSubsampling = GetRaw(handle, StreamKind.Video, i, "ChromaSubsampling"),
                Language = GetRaw(handle, StreamKind.Video, i, "Language/String3"),
                Default = GetRaw(handle, StreamKind.Video, i, "Default"),
                ScanType = GetRaw(handle, StreamKind.Video, i, "ScanType"),
                FrameRateMode = GetRaw(handle, StreamKind.Video, i, "FrameRate_Mode"),
                FrameRate = GetRaw(handle, StreamKind.Video, i, "FrameRate/String"),
                ColourRange = colourRange,
                MatrixCoefficients = matrix,
                ColourPrimaries = primaries,
                TransferCharacteristics = transfer,
                ParsedWidth = parsedWidth,
                ParsedHeight = parsedHeight,
                ParsedBitDepth = parsedBitDepth,
            });
        }

        var rawAudios = new List<RawAudioTrack>();
        for (var i = 0; i < audioCount; i++)
        {
            rawAudios.Add(new RawAudioTrack
            {
                Format = GetRaw(handle, StreamKind.Audio, i, "Format"),
                Language = GetRaw(handle, StreamKind.Audio, i, "Language/String3"),
                Default = GetRaw(handle, StreamKind.Audio, i, "Default"),
            });
        }

        var rawTexts = new List<RawTextTrack>();
        for (var i = 0; i < textCount; i++)
        {
            rawTexts.Add(new RawTextTrack
            {
                Format = GetRaw(handle, StreamKind.Text, i, "Format"),
                Language = GetRaw(handle, StreamKind.Text, i, "Language/String3"),
                Default = GetRaw(handle, StreamKind.Text, i, "Default"),
            });
        }

        var rawChapters = new List<RawChapter>();
        if (chapterCount > 0)
        {
            var chapPosBegin = (int)MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_Begin").TryParseAsLong();
            var chapPosEnd = (int)MediaInfoNative.Get(handle, StreamKind.Menu, 0, "Chapters_Pos_End").TryParseAsLong();
            for (var i = chapPosBegin; i < chapPosEnd; i++)
            {
                var name = MediaInfoNative.GetByIndex(handle, StreamKind.Menu, 0, i, InfoKind.Text);
                RawField languageRaw = RawField.Absent;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var idx = name.IndexOf(':');
                    if (idx > 0)
                    {
                        languageRaw = RawField.Of(name[..idx].Trim());
                    }
                    else
                    {
                        languageRaw = RawField.Of(string.Empty);
                    }
                }

                var timeName = MediaInfoNative.GetByIndex(handle, StreamKind.Menu, 0, i, InfoKind.Name);
                rawChapters.Add(new RawChapter
                {
                    Language = languageRaw,
                    Name = string.IsNullOrWhiteSpace(name) ? RawField.Absent : RawField.Of(name),
                    TimespanMs = timeName.TryParseAsMillisecond(),
                });
            }
        }

        var snapshot = new RawMediaSnapshot
        {
            FullPath = url,
            Extension = Path.GetExtension(url),
            ContainerFormat = string.IsNullOrWhiteSpace(format) ? RawField.Absent : RawField.Of(format),
            VideoTracks = rawVideos,
            AudioTracks = rawAudios,
            TextTracks = rawTexts,
            Chapters = rawChapters,
            ChapterCount = chapterCount,
            AdapterUnavailable = false,
        };

        // Shell GeneralInfo before pure display projection fills tracks.
        var info = new MediaFileInfo(new GeneralInfo(
            filename: Path.GetFileNameWithoutExtension(url),
            fullPath: url,
            format: format ?? string.Empty,
            bitrate: bitrate,
            videoCount: videoCount,
            audioCount: audioCount,
            textCount: textCount,
            chapterCount: chapterCount))
        {
            Summary = summary,
        };

        // Pure projection — no further P/Invoke.
        RawMediaSnapshotProjector.ProjectDisplayModels(info, snapshot);

        // Preserve bitrate (not carried on raw snapshot).
        info.GeneralInfo.Bitrate = bitrate;

        // Duration/delay remain native-only legacy display fields (not Collation evidence).
        for (var i = 0; i < videoCount && i < info.VideoInfos.Count; i++)
        {
            info.VideoInfos[i].Duration = MediaInfoNative.Get(handle, StreamKind.Video, i, "Duration").TryParseAsLong();
            info.VideoInfos[i].Delay = MediaInfoNative.Get(handle, StreamKind.Video, i, "Delay").TryParseAsLong();
            info.VideoInfos[i].Bitrate = MediaInfoNative.Get(handle, StreamKind.Video, i, "BitRate").TryParseAsLong() / 1000;
        }

        for (var i = 0; i < audioCount && i < info.AudioInfos.Count; i++)
        {
            info.AudioInfos[i].Duration = MediaInfoNative.Get(handle, StreamKind.Audio, i, "Duration").TryParseAsLong();
            info.AudioInfos[i].Delay = MediaInfoNative.Get(handle, StreamKind.Audio, i, "Delay").TryParseAsLong();
            info.AudioInfos[i].Bitrate = MediaInfoNative.Get(handle, StreamKind.Audio, i, "BitRate").TryParseAsLong() / 1000;
            info.AudioInfos[i].BitDepth = MediaInfoNative.Get(handle, StreamKind.Audio, i, "BitDepth").TryParseAsLong();
        }

        return info;
    }

    private static RawField GetRaw(IntPtr handle, StreamKind kind, int stream, string parameter)
    {
        var value = MediaInfoNative.Get(handle, kind, stream, parameter);
        // Null pointer from native → Absent. Empty string → PresentEmpty.
        // Whitespace-only → trim to PresentEmpty (Stage 0 presence policy).
        if (value is null)
        {
            return RawField.Absent;
        }

        if (value.Length == 0)
        {
            return RawField.Of(string.Empty);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return RawField.Of(string.Empty);
        }

        return RawField.Of(value);
    }
}
