using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Tests.Validation;

public class MediaValidatorTests
{
    private static MediaFileInfo CreateBase(
        string fullPath = @"/media/sample.mkv",
        string format = "Matroska",
        long chapterCount = 0)
    {
        var info = new MediaFileInfo(new GeneralInfo(
            filename: Path.GetFileNameWithoutExtension(fullPath),
            fullPath: fullPath,
            format: format,
            bitrate: 1000,
            videoCount: 1,
            audioCount: 1,
            textCount: 0,
            chapterCount: chapterCount));
        info.VideoInfos.Add(new VideoInfo(
            "HEVC", "Main 10@L4", "CFR", "23.976", 1000, 10, 10000, 1080, 1920, "JPN", 0,
            new ProfileInfo("Main 10@L4"), "YUV420", "Yes"));
        info.AudioInfos.Add(new AudioInfo("FLAC", 16, 1000, 10000, "JPN", 0, "Yes"));
        return info;
    }

    [Fact]
    public void ExtensionContainerMismatch_MatroskaWithMp4Ext_IsError()
    {
        var info = CreateBase("/media/a.mp4", "Matroska");
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Level == ErrorLevel.Error && f.Description.Contains("文件后缀"));
    }

    [Fact]
    public void NonZeroDelay_IsWarning()
    {
        var info = CreateBase();
        info.VideoInfos[0].Delay = 10;
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Level == ErrorLevel.Warning && f.Description.Contains("延时"));
    }

    [Fact]
    public void DurationDeltaOver600_IsWarning()
    {
        var info = CreateBase();
        info.VideoInfos[0].Duration = 10000;
        info.AudioInfos[0].Duration = 10000 + 601;
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Description.Contains("轨道间长度"));
    }

    [Fact]
    public void DurationDeltaExactly600_NotWarning()
    {
        var info = CreateBase();
        info.VideoInfos[0].Duration = 10000;
        info.AudioInfos[0].Duration = 10600;
        var findings = MediaValidator.CheckFile(info);
        Assert.DoesNotContain(findings, f => f.Description.Contains("轨道间长度"));
    }

    [Fact]
    public void SingleChapter_IsWarning()
    {
        var info = CreateBase(chapterCount: 1);
        info.ChapterInfos.Add(new ChapterInfo(0, "ch", "ENG"));
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Description.Contains("只有一个章节"));
    }

    [Fact]
    public void MultipleChapterSets_IsWarning()
    {
        var info = CreateBase(chapterCount: -1);
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Description.Contains("多组章节"));
    }

    [Fact]
    public void UselessFinalChapter_IsWarning()
    {
        var info = CreateBase(chapterCount: 2);
        info.VideoInfos[0].Duration = 10000;
        info.AudioInfos[0].Duration = 10000;
        info.ChapterInfos.Add(new ChapterInfo(0, "a", "ENG"));
        info.ChapterInfos.Add(new ChapterInfo(10000 - 500, "b", "ENG")); // > max - 1100
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Description.Contains("无用章节"));
    }

    [Fact]
    public void FirstChapterNonZero_IsWarning()
    {
        var info = CreateBase(chapterCount: 2);
        info.VideoInfos[0].Duration = 10000;
        info.AudioInfos[0].Duration = 10000;
        info.ChapterInfos.Add(new ChapterInfo(100, "a", "ENG"));
        info.ChapterInfos.Add(new ChapterInfo(5000, "b", "ENG"));
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Description.Contains("首个章节时间戳非零"));
    }

    [Fact]
    public void MultiAudio_IsInfo()
    {
        var info = CreateBase();
        info.AudioInfos.Add(new AudioInfo("AAC", 0, 128, 10000, "ENG", 0, "No"));
        info.AudioInfos.Add(new AudioInfo("AAC", 0, 128, 10000, "CHI", 0, "No"));
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Level == ErrorLevel.Info && f.Description.Contains("多条音轨"));
    }

    [Fact]
    public void NonVcbsFilename_IsMatched()
    {
        var info = CreateBase("/media/ordinary.mkv");
        Assert.True(MediaValidator.FileNameContentMatched(info));
    }

    [Fact]
    public void VcbsFilename_EmptyVideo_IsMismatch_NoThrow()
    {
        var path = "/media/[VCB-S] Show [Ma10p_1080p][x265_flac].mkv";
        var info = new MediaFileInfo(new GeneralInfo("Show", path, "Matroska", 0, 0, 0, 0, 0));
        Assert.False(MediaValidator.FileNameContentMatched(info));
        var findings = MediaValidator.CheckFile(info);
        // no duration tracks → CheckFile returns early before filename check when duration empty
        // Ensure FileNameContentMatched itself is safe
        Assert.False(MediaValidator.FileNameContentMatched(info));
    }

    [Fact]
    public void GenerateProfileString_HevcMain10()
    {
        Assert.Equal("Ma10p", MediaValidator.GenerateProfileString(
            new ProfileInfo("Main 10@L4"), "HEVC", 10, "YUV420"));
    }

    [Fact]
    public void GenerateVencoder_Hevc()
    {
        var v = new VideoInfo("HEVC", "", "", "", 0, 10, 0, 0, 0, "", 0, new ProfileInfo(""), "", "Yes");
        Assert.Equal("x265", MediaValidator.GenerateVencoderString(v));
    }

    [Fact]
    public void GenerateAencoders_TwoFlac()
    {
        var audios = new List<AudioInfo>
        {
            new("FLAC", 16, 0, 0, "", 0, "Yes"),
            new("FLAC", 16, 0, 0, "", 0, "No")
        };
        Assert.Equal("_2flac", MediaValidator.GenerateAencodersString(audios));
    }
}
