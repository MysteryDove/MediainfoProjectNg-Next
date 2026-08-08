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
        var info = new MediaFileInfo(new GeneralInfo("multi", "/multi.mkv", "Matroska", 0, 1, 2, 3, 0));
        info.AudioInfos.Add(new AudioInfo("FLAC", 24, 1000, 1, "JPN", 0, "Yes"));
        info.AudioInfos.Add(new AudioInfo("AAC", 16, 192, 1, "ENG", 0, "No"));
        info.SubInfos.Add(new SubInfo("ASS", "Yes", "JPN"));
        info.SubInfos.Add(new SubInfo("PGS", "No", "ENG"));
        info.SubInfos.Add(new SubInfo("SRT", "No", "CHI"));

        var rows = MediaFileRowViewModel.CreateRows(info);

        Assert.Equal(3, rows.Count);
        Assert.Equal(("FLAC", "ASS"), (rows[0].AudioFormat, rows[0].SubtitleFormat));
        Assert.Equal(("AAC", "PGS"), (rows[1].AudioFormat, rows[1].SubtitleFormat));
        Assert.Equal((string.Empty, "SRT"), (rows[2].AudioFormat, rows[2].SubtitleFormat));
    }
}
