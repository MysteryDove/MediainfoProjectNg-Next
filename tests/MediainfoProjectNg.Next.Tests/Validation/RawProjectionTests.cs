using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Tests.Validation;

public class RawProjectionTests
{
    [Fact]
    public void Distinguishes_Absent_Empty_Und_AndExplicitNo()
    {
        Assert.True(RawField.Absent.IsAbsent);
        Assert.False(RawField.Absent.IsPresent);

        var empty = RawField.Of("");
        Assert.True(empty.IsPresent);
        Assert.True(empty.IsPresentEmpty);

        var und = RawField.Of("UND");
        Assert.True(und.IsPresent);
        Assert.Equal("UND", und.TextOrEmpty);

        var no = RawField.Of("No");
        Assert.Equal("No", no.TextOrEmpty);

        var malformed = RawField.Of("not-a-number").ParseLong(out var parsed);
        Assert.Null(parsed);
        Assert.True(malformed.ParseFailed);

        var zero = RawField.Of("0").ParseLong(out parsed);
        Assert.Equal(0, parsed);
        Assert.False(zero.ParseFailed);

        var frac = RawField.Of("1920.5").ParseLong(out var fracVal);
        Assert.Null(fracVal);
        Assert.True(frac.ParseFailed);

        var overflow = RawField.Of("9999999999999999999999999999").ParseLong(out var ov);
        Assert.Null(ov);
        Assert.True(overflow.ParseFailed);
    }

    [Fact]
    public void AbsentVideoLanguage_IsUnverifiable_NotSilentPass()
    {
        var path = "/media/[VCB-S] Show [Ma10p_1080p][x265_flac].mkv";
        var info = new MediaFileInfo(new GeneralInfo("Show", path, "Matroska", 0, 1, 1, 0, 0));
        info.VideoInfos.Add(new VideoInfo(
            "HEVC", "Main 10@L4", "CFR", "23.976", 1000, 10, 10000, 1080, 1920, "UND", 0,
            new ProfileInfo("Main 10@L4"), "YUV420", "Yes"));
        info.AudioInfos.Add(new AudioInfo("FLAC", 16, 1000, 10000, "JPN", 0, "Yes"));
        info.RawSnapshot = new RawMediaSnapshot
        {
            FullPath = path,
            Extension = ".mkv",
            ContainerFormat = RawField.Of("Matroska"),
            VideoTracks =
            [
                new RawVideoTrack
                {
                    Language = RawField.Absent,
                    Default = RawField.Of("Yes"),
                    Width = RawField.Of("1920"),
                    Height = RawField.Of("1080"),
                    ScanType = RawField.Of("Progressive"),
                    ColourRange = RawField.Of("Limited"),
                    MatrixCoefficients = RawField.Of("BT.709"),
                    ParsedWidth = 1920,
                    ParsedHeight = 1080,
                    ParsedBitDepth = 10,
                    Format = RawField.Of("HEVC"),
                },
            ],
            AudioTracks =
            [
                new RawAudioTrack
                {
                    Format = RawField.Of("FLAC"),
                    Language = RawField.Of("JPN"),
                    Default = RawField.Of("Yes"),
                },
            ],
        };

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.TrackVideoLanguage);
        Assert.Equal(RuleOutcome.Unverifiable, eval.Outcome);
    }

    [Fact]
    public void ExplicitNoDefault_IsNotYes()
    {
        var path = "/media/[VCB-S] Show [Ma10p_1080p][x265_flac].mkv";
        var info = new MediaFileInfo(new GeneralInfo("Show", path, "Matroska", 0, 1, 1, 0, 0));
        info.VideoInfos.Add(new VideoInfo(
            "HEVC", "Main 10@L4", "CFR", "23.976", 1000, 10, 10000, 1080, 1920, "UND", 0,
            new ProfileInfo("Main 10@L4"), "YUV420", "No"));
        info.AudioInfos.Add(new AudioInfo("FLAC", 16, 1000, 10000, "JPN", 0, "Yes"));
        info.RawSnapshot = new RawMediaSnapshot
        {
            FullPath = path,
            Extension = ".mkv",
            ContainerFormat = RawField.Of("Matroska"),
            VideoTracks =
            [
                new RawVideoTrack
                {
                    Language = RawField.Of("UND"),
                    Default = RawField.Of("No"),
                    Width = RawField.Of("1920"),
                    Height = RawField.Of("1080"),
                    ScanType = RawField.Of("Progressive"),
                    ColourRange = RawField.Of("Limited"),
                    MatrixCoefficients = RawField.Of("BT.709"),
                    ParsedWidth = 1920,
                    ParsedHeight = 1080,
                    ParsedBitDepth = 10,
                    Format = RawField.Of("HEVC"),
                },
            ],
            AudioTracks =
            [
                new RawAudioTrack
                {
                    Format = RawField.Of("FLAC"),
                    Language = RawField.Of("JPN"),
                    Default = RawField.Of("Yes"),
                },
            ],
        };

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.TrackVideoDefault);
        Assert.Equal(RuleOutcome.Violation, eval.Outcome);
    }
}
