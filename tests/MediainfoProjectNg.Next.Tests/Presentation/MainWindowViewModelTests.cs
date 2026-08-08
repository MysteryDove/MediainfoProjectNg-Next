using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Core.Loading;
using MediainfoProjectNg.Next.Core.Presentation;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.ViewModels;

namespace MediainfoProjectNg.Next.Tests.Presentation;

public class MainWindowViewModelTests
{
    private sealed class FakeReader : IMediaMetadataReader
    {
        public MediaFileInfo Read(string path)
        {
            var chapterFile = Path.GetFileName(path).StartsWith("chapter", StringComparison.Ordinal);
            var info = new MediaFileInfo(new GeneralInfo(
                Path.GetFileNameWithoutExtension(path),
                path,
                "Matroska",
                1000,
                1,
                chapterFile ? 1 : 3,
                0,
                chapterFile ? 1 : 0));
            info.VideoInfos.Add(new VideoInfo(
                "HEVC", "Main 10@L4", "CFR", "23.976", 1000, 10, 10000, 1080, 1920,
                "UND", 0, new ProfileInfo("Main 10@L4"), "YUV420", "Yes"));
            info.Summary = $"summary:{path}";
            info.AudioInfos.Add(new AudioInfo("FLAC", 16, 1000, 10000, "JPN", 0, "Yes"));
            if (!chapterFile)
            {
                info.AudioInfos.Add(new AudioInfo("AAC", 0, 192, 10000, "JPN", 0, "No"));
                info.AudioInfos.Add(new AudioInfo("AAC", 0, 192, 10000, "JPN", 0, "No"));
            }

            return info;
        }

        public string? GetLibraryVersion() => "Fake 1.0";
    }

    [Fact]
    public async Task Load_Filter_Counts_Selection_AndClearFilters_AreCoordinated()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mpng-next-vm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var chapter = Path.Combine(directory, "chapter.mkv");
            var track = Path.Combine(directory, "track.mkv");
            await File.WriteAllBytesAsync(chapter, [0]);
            await File.WriteAllBytesAsync(track, [0]);

            var reader = new FakeReader();
            var vm = new MainWindowViewModel(new MediaLoadService(reader), reader);
            await vm.LoadPathsAsync([track, directory]);

            Assert.Equal(2, vm.CanonicalCount);
            Assert.Equal(4, vm.Files.Count);
            Assert.Equal("列表中共有 2 个文件", vm.FileCountText);
            var trackToggle = vm.CategoryToggles.Single(t => t.Category == IssueCategory.Track);
            var chapterToggle = vm.CategoryToggles.Single(t => t.Category == IssueCategory.Chapter);
            Assert.Equal("轨道 (1)", trackToggle.ButtonText);
            Assert.Equal("章节 (1)", chapterToggle.ButtonText);

            vm.SelectedFile = vm.Files.First(row => row.FullPath == track);
            vm.ToggleCategoryCommand.Execute(chapterToggle);

            Assert.Null(vm.SelectedFile);
            Assert.Single(vm.Files);
            Assert.Equal(chapter, vm.Files[0].FullPath);

            vm.ClearCategoryFiltersCommand.Execute(null);
            Assert.Equal(4, vm.Files.Count);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void TrackRows_ExposeOneAudioAndSubtitlePerRow()
    {
        var info = new MediaFileInfo(new GeneralInfo("multi", "/multi.mkv", "Matroska", 0, 1, 2, 3, 1));
        info.VideoInfos.Add(new VideoInfo(
            "HEVC", "Main 10", "CFR", "23.976 (24000/1001)", 1000, 10, 1,
            1080, 1920, "JPN", 0, new ProfileInfo("Main 10"), "YUV420", "Yes"));
        info.AudioInfos.Add(new AudioInfo("FLAC", 24, 1000, 1, "JPN", 0, "Yes"));
        info.AudioInfos.Add(new AudioInfo("AAC", 16, 192, 1, "ENG", 0, "No"));
        info.SubInfos.Add(new SubInfo("ASS", "Yes", "JPN"));
        info.SubInfos.Add(new SubInfo("PGS", "No", "ENG"));
        info.SubInfos.Add(new SubInfo("SRT", "No", "CHI"));
        info.ChapterInfos.Add(new ChapterInfo(0, "Chapter 1", "JPN"));

        var rows = MediaFileRowViewModel.CreateRows(info);

        Assert.Equal(3, rows.Count);
        Assert.Equal(("#1", "FLAC", "#1", "ASS"),
            (rows[0].AudioTrackLabel, rows[0].AudioFormat, rows[0].SubtitleTrackLabel, rows[0].SubtitleFormat));
        Assert.Equal(("#2", "AAC", "#2", "PGS"),
            (rows[1].AudioTrackLabel, rows[1].AudioFormat, rows[1].SubtitleTrackLabel, rows[1].SubtitleFormat));
        Assert.Equal((string.Empty, string.Empty, "#3", "SRT"),
            (rows[2].AudioTrackLabel, rows[2].AudioFormat, rows[2].SubtitleTrackLabel, rows[2].SubtitleFormat));

        Assert.False(rows[0].IsContinuation);
        Assert.Equal(("multi", "Matroska", "/multi.mkv", "HEVC", "有"),
            (rows[0].DisplayFilename, rows[0].DisplayContainer, rows[0].DisplayFullPath,
                rows[0].VideoFormat, rows[0].ChapterState));
        Assert.All(rows.Skip(1), row =>
        {
            Assert.True(row.IsContinuation);
            Assert.Equal((string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
                (row.DisplayFilename, row.DisplayContainer, row.DisplayFullPath,
                    row.VideoFormat, row.ChapterState));
            Assert.Equal("/multi.mkv", row.FullPath);
            Assert.Same(info, row.Model);
        });
    }

    [Theory]
    [InlineData("Show [Menu01]")]
    [InlineData("Show [menu01_2]")]
    public void Menu_WithMultiplePgs_CollapsesToOneAggregateRow(string filename)
    {
        var info = CreateMenuInfo(filename);
        info.AudioInfos.Add(new AudioInfo("FLAC", 24, 1000, 1, "JPN", 0, "Yes"));
        info.AudioInfos.Add(new AudioInfo("FLAC", 24, 1000, 1, "JPN", 0, "Yes"));
        info.SubInfos.Add(new SubInfo("PGS", "No", "JPN"));
        info.SubInfos.Add(new SubInfo("HDMV PGS", "No", "JPN"));

        var row = Assert.Single(MediaFileRowViewModel.CreateRows(info));

        Assert.True(row.IsMenuPgsAggregate);
        Assert.False(row.IsContinuation);
        Assert.Equal(("#1-#2", "FLAC", "24", "1000", "JPN", "Yes"),
            (row.AudioTrackLabel, row.AudioFormat, row.AudioBitDepth, row.AudioBitrate,
                row.AudioLanguage, row.AudioDefault));
        Assert.Equal(("#1-#2", "多种", "JPN", "No"),
            (row.SubtitleTrackLabel, row.SubtitleFormat, row.SubtitleLanguage, row.SubtitleDefault));
        Assert.Equal(ColorToken.WarningDelayTeal, row.RowBackgroundToken);
        Assert.Contains("多字幕轨道提示", row.TooltipText, StringComparison.Ordinal);
    }

    [Fact]
    public void Menu_Aggregate_UsesMixedValues_AndExistingFindingHasColorPriority()
    {
        var info = CreateMenuInfo("Show [Menu02]");
        info.AudioInfos.Add(new AudioInfo("FLAC", 16, 1000, 1, "JPN", 0, "Yes"));
        info.AudioInfos.Add(new AudioInfo("AAC", 24, 192, 1, "ENG", 0, "No"));
        info.SubInfos.Add(new SubInfo("PGS", "No", "JPN"));
        info.SubInfos.Add(new SubInfo("PGS", "Yes", "ENG"));
        info.SetFindings([new ValidationFinding(ErrorLevel.Error, "extension mismatch")]);

        var row = Assert.Single(MediaFileRowViewModel.CreateRows(info));

        Assert.Equal(("多种", "多种", "多种", "多种", "多种"),
            (row.AudioFormat, row.AudioBitDepth, row.AudioBitrate, row.AudioLanguage, row.AudioDefault));
        Assert.Equal(("PGS", "多种", "多种"),
            (row.SubtitleFormat, row.SubtitleLanguage, row.SubtitleDefault));
        Assert.Equal(ColorToken.ErrorRed, row.RowBackgroundToken);
    }

    [Theory]
    [InlineData("Show", 2)]
    [InlineData("Show [Menu01]", 1)]
    public void MenuAggregation_RequiresBothTagAndMultiplePgs(string filename, int pgsCount)
    {
        var info = CreateMenuInfo(filename);
        for (var index = 0; index < pgsCount; index++)
        {
            info.SubInfos.Add(new SubInfo("PGS", "No", "JPN"));
        }

        var rows = MediaFileRowViewModel.CreateRows(info);

        Assert.Equal(Math.Max(1, pgsCount), rows.Count);
        Assert.All(rows, row => Assert.False(row.IsMenuPgsAggregate));
    }

    [Fact]
    public async Task ContinuationSelection_RetainsDetailsAndDeletingItRemovesWholeFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mpng-next-continuation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "track.mkv");
            await File.WriteAllBytesAsync(path, [0]);
            var reader = new FakeReader();
            var vm = new MainWindowViewModel(new MediaLoadService(reader), reader);

            await vm.LoadPathsAsync([path]);
            var continuation = vm.Files.Single(row => row.TrackIndex == 1);
            vm.SelectedFile = continuation;

            Assert.Equal(path, continuation.FullPath);
            Assert.Equal(string.Empty, continuation.DisplayFullPath);
            Assert.Same(continuation.Model, vm.SelectedFile.Model);
            Assert.Equal($"summary:{path}", vm.SelectedSummary);

            vm.RemoveRows([continuation]);

            Assert.Empty(vm.Files);
            Assert.Equal(0, vm.CanonicalCount);
            Assert.Null(vm.SelectedFile);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static MediaFileInfo CreateMenuInfo(string filename) =>
        new(new GeneralInfo(filename, $"/{filename}.mkv", "Matroska", 0, 0, 0, 2, 0));
}
