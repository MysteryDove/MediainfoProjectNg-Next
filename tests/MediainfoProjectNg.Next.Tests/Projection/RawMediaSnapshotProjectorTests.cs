using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Projection;

namespace MediainfoProjectNg.Next.Tests.Projection;

public class RawMediaSnapshotProjectorTests
{
    [Fact]
    public void ProjectDisplayModels_IsPure_AndMapsPresencePreservingFields()
    {
        var info = new MediaFileInfo(new GeneralInfo("x", "/x.mkv", "", 0, 0, 0, 0, 0));
        var raw = new RawMediaSnapshot
        {
            FullPath = "/media/[VCB-S] Show [Ma10p_1080p][x265_flac].mkv",
            Extension = ".mkv",
            ContainerFormat = RawField.Of("Matroska"),
            VideoTracks =
            [
                new RawVideoTrack
                {
                    Format = RawField.Of("HEVC"),
                    FormatProfile = RawField.Of("Main 10@L4"),
                    Width = RawField.Of("1920"),
                    Height = RawField.Of("1080"),
                    BitDepth = RawField.Of("10"),
                    ColorSpace = RawField.Of("YUV"),
                    ChromaSubsampling = RawField.Of("4:2:0"),
                    Language = RawField.Absent,
                    Default = RawField.Of("Yes"),
                    ScanType = RawField.Of("Progressive"),
                    ColourRange = RawField.Of("Limited"),
                    MatrixCoefficients = RawField.Of("BT.709"),
                    ParsedWidth = 1920,
                    ParsedHeight = 1080,
                    ParsedBitDepth = 10,
                },
            ],
            AudioTracks =
            [
                new RawAudioTrack
                {
                    Format = RawField.Of("FLAC"),
                    Language = RawField.Of("jpn"),
                    Default = RawField.Of("Yes"),
                },
            ],
            TextTracks =
            [
                new RawTextTrack
                {
                    Format = RawField.Of("PGS"),
                    Language = RawField.Of("jpn"),
                    Default = RawField.Of("No"),
                },
            ],
            Chapters = [],
            ChapterCount = 0,
        };

        RawMediaSnapshotProjector.ProjectDisplayModels(info, raw);

        Assert.Same(raw, info.RawSnapshot);
        Assert.Equal("Matroska", info.GeneralInfo.Format);
        Assert.Single(info.VideoInfos);
        Assert.Equal("UND", info.VideoInfos[0].Language); // legacy-friendly display
        Assert.Equal("Yes", info.VideoInfos[0].Default);
        Assert.Equal("YUV420", info.VideoInfos[0].ColorSpace);
        Assert.Equal("Ma10p", RawMediaSnapshotProjector.GenerateProfileStringFromRaw(raw.VideoTracks[0]));
        Assert.Equal("x265", RawMediaSnapshotProjector.GenerateVencoderStringFromRaw(raw.VideoTracks[0]));
        Assert.Equal("_flac", RawMediaSnapshotProjector.GenerateAencodersStringFromRaw(raw.AudioTracks));
        Assert.True(RawMediaSnapshotProjector.IsPgsFormat(raw.TextTracks[0].Format));
    }
}
