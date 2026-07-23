using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Core.Loading;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Tests.Loading;

public class MediaLoadServiceTests
{
    private sealed class FakeReader : IMediaMetadataReader
    {
        public MediaFileInfo Read(string path) =>
            new(new GeneralInfo(
                Path.GetFileNameWithoutExtension(path),
                path,
                "Matroska",
                0, 1, 1, 0, 0))
            {
                Summary = "summary"
            };

        public string? GetLibraryVersion() => "Fake 1.0";
    }

    [Fact]
    public async Task FilterPolarity_TrueMeansSkip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mpng-next-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var a = Path.Combine(dir, "a.mkv");
            var b = Path.Combine(dir, "b.mkv");
            await File.WriteAllBytesAsync(a, [0]);
            await File.WriteAllBytesAsync(b, [0]);

            var svc = new MediaLoadService(new FakeReader());
            var skip = new HashSet<string>(StringComparer.Ordinal) { a };
            var (info, _) = await svc.LoadAsync([a, b], filter: p => skip.Contains(p));

            Assert.Single(info);
            Assert.Equal(b, info[0].GeneralInfo.FullPath);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task ExcludedExtensions_Skipped()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mpng-next-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var txt = Path.Combine(dir, "nfo.txt");
            var mkv = Path.Combine(dir, "v.mkv");
            await File.WriteAllBytesAsync(txt, [0]);
            await File.WriteAllBytesAsync(mkv, [0]);

            var svc = new MediaLoadService(new FakeReader());
            var (info, _) = await svc.LoadAsync([txt, mkv]);

            Assert.Single(info);
            Assert.EndsWith("v.mkv", info[0].GeneralInfo.FullPath);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task ProgressCallback_InvokedWithPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mpng-next-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var mkv = Path.Combine(dir, "v.mkv");
            await File.WriteAllBytesAsync(mkv, [0]);
            string? seen = null;
            var svc = new MediaLoadService(new FakeReader());
            await svc.LoadAsync([mkv], progressCallback: p => seen = p);
            Assert.Equal(mkv, seen);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task Load_AttachesFindings()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mpng-next-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var mkv = Path.Combine(dir, "v.mkv");
            await File.WriteAllBytesAsync(mkv, [0]);
            var svc = new MediaLoadService(new FakeReader());
            var (info, _) = await svc.LoadAsync([mkv]);
            Assert.NotNull(info[0].Findings);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
