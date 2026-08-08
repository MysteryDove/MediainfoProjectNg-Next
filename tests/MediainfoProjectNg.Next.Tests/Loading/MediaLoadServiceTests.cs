using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Core.Loading;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Tests.Loading;

public class MediaLoadServiceTests
{
    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

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
    public async Task FileAndParentDirectory_InSameBatch_LoadOnce()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mpng-next-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "v.mkv");
            await File.WriteAllBytesAsync(file, [0]);

            var svc = new MediaLoadService(new FakeReader());
            var (info, _) = await svc.LoadAsync([file, dir]);

            Assert.Single(info);
            Assert.Equal(file, info[0].GeneralInfo.FullPath);
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
            await svc.LoadAsync([mkv], progress: new InlineProgress<string>(p => seen = p));
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

    [Fact]
    public async Task MultipleFiles_LoadInParallel_AndReturnInEnumerationOrder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mpng-next-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var paths = Enumerable.Range(0, 8)
                .Select(index => Path.Combine(dir, $"{index:D2}.mkv"))
                .ToArray();
            foreach (var path in paths)
            {
                await File.WriteAllBytesAsync(path, [0]);
            }

            var reader = new ConcurrencyReader();
            var service = new MediaLoadService(reader);
            var (infos, _) = await service.LoadAsync(paths);

            Assert.True(reader.MaxConcurrency > 1, $"Expected parallel reads, saw {reader.MaxConcurrency}.");
            Assert.Equal(paths, infos.Select(info => info.GeneralInfo.FullPath));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private sealed class ConcurrencyReader : IMediaMetadataReader
    {
        private int _active;
        private int _maxConcurrency;

        public int MaxConcurrency => _maxConcurrency;

        public MediaFileInfo Read(string path)
        {
            var active = Interlocked.Increment(ref _active);
            int observed;
            do
            {
                observed = _maxConcurrency;
            }
            while (active > observed
                   && Interlocked.CompareExchange(ref _maxConcurrency, active, observed) != observed);

            Thread.Sleep(30);
            Interlocked.Decrement(ref _active);
            return new MediaFileInfo(new GeneralInfo(
                Path.GetFileNameWithoutExtension(path), path, "Matroska", 0, 0, 0, 0, 0));
        }

        public string? GetLibraryVersion() => "Fake 1.0";
    }
}
