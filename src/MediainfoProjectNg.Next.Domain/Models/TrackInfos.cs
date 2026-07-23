namespace MediainfoProjectNg.Next.Domain.Models;

public sealed class GeneralInfo
{
    public string Filename { get; set; }
    public string FullPath { get; set; }
    public string Format { get; set; }
    public long Bitrate { get; set; }
    public long VideoCount { get; set; }
    public long AudioCount { get; set; }
    public long TextCount { get; set; }
    public long ChapterCount { get; set; }

    public GeneralInfo(
        string filename,
        string fullPath,
        string format,
        long bitrate,
        long videoCount,
        long audioCount,
        long textCount,
        long chapterCount)
    {
        Filename = filename;
        FullPath = fullPath;
        Format = format;
        Bitrate = bitrate;
        VideoCount = videoCount;
        AudioCount = audioCount;
        TextCount = textCount;
        ChapterCount = chapterCount;
    }
}

public sealed class VideoInfo
{
    public string Format { get; set; }
    public string FormatProfile { get; set; }
    public string FpsMode { get; set; }
    public string Fps { get; set; }
    public long Bitrate { get; set; }
    public long BitDepth { get; set; }
    public long Duration { get; set; }
    public long Height { get; set; }
    public long Width { get; set; }
    public string Language { get; set; }
    public long Delay { get; set; }
    public ProfileInfo Profile { get; set; }
    public string ColorSpace { get; set; }
    public string Default { get; set; }

    public VideoInfo(
        string format,
        string formatProfile,
        string fpsMode,
        string fps,
        long bitrate,
        long bitDepth,
        long duration,
        long height,
        long width,
        string language,
        long delay,
        ProfileInfo profile,
        string colorSpace,
        string isDefault)
    {
        Format = format;
        FormatProfile = formatProfile;
        FpsMode = fpsMode;
        Fps = fps;
        Bitrate = bitrate;
        BitDepth = bitDepth;
        Duration = duration;
        Height = height;
        Width = width;
        Language = language;
        Delay = delay;
        Profile = profile;
        ColorSpace = colorSpace;
        Default = isDefault;
    }
}

public sealed class AudioInfo
{
    public string Format { get; set; }
    public long BitDepth { get; set; }
    public long Bitrate { get; set; }
    public long Duration { get; set; }
    public string Language { get; set; }
    public long Delay { get; set; }
    public string Default { get; set; }

    public AudioInfo(
        string format,
        long bitDepth,
        long bitrate,
        long duration,
        string language,
        long delay,
        string isDefault)
    {
        Format = format;
        BitDepth = bitDepth;
        Bitrate = bitrate;
        Duration = duration;
        Language = language;
        Delay = delay;
        Default = isDefault;
    }
}

public sealed class ChapterInfo
{
    public int Timespan { get; set; }
    public string Name { get; set; }
    public string Language { get; set; }

    public ChapterInfo(int timespan, string name, string language)
    {
        Timespan = timespan;
        Name = name;
        Language = language;
    }
}

public sealed class SubInfo
{
    public string Format { get; set; }
    public string Default { get; set; }
    public string Language { get; set; }

    public SubInfo(string format, string isDefault, string language)
    {
        Format = format;
        Default = isDefault;
        Language = language;
    }
}
