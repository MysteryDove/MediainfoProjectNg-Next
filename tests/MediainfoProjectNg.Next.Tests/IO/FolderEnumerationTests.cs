using MediainfoProjectNg.Next.Domain.IO;

namespace MediainfoProjectNg.Next.Tests.IO;

public class FolderEnumerationTests
{
    [Fact]
    public void ExcludeLists_MatchLegacy()
    {
        Assert.Equal(new[] { "CDs", "Scans" }, FolderEnumeration.ExcludeDirs);
        Assert.Equal(new[] { ".txt", ".log", ".torrent" }, FolderEnumeration.ExcludeExts);
    }

    [Fact]
    public void EnumerateFolder_TopLevelFirst_ThenBfs_SkipsCdsScans()
    {
        // tree:
        // root/a.mkv, root/b.mkv
        // root/sub/c.mkv
        // root/CDs/d.mkv (skipped dir)
        // root/sub/Scans/e.mkv (skipped)
        // root/sub2/f.mkv
        var files = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["/root"] = ["/root/a.mkv", "/root/b.mkv"],
            ["/root/sub"] = ["/root/sub/c.mkv"],
            ["/root/CDs"] = ["/root/CDs/d.mkv"],
            ["/root/sub/Scans"] = ["/root/sub/Scans/e.mkv"],
            ["/root/sub2"] = ["/root/sub2/f.mkv"],
        };
        var dirs = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["/root"] = ["/root/sub", "/root/CDs", "/root/sub2"],
            ["/root/sub"] = ["/root/sub/Scans"],
            ["/root/CDs"] = [],
            ["/root/sub/Scans"] = [],
            ["/root/sub2"] = [],
        };

        var result = FolderEnumeration.EnumerateFolder(
            "/root",
            p => files.GetValueOrDefault(p, []),
            p => dirs.GetValueOrDefault(p, [])).ToList();

        Assert.Equal(
            new[] { "/root/a.mkv", "/root/b.mkv", "/root/sub/c.mkv", "/root/sub2/f.mkv" },
            result);
        Assert.DoesNotContain(result, p => p.Contains("CDs") || p.Contains("Scans"));
    }

    [Theory]
    [InlineData(".txt", true)]
    [InlineData(".log", true)]
    [InlineData(".torrent", true)]
    [InlineData(".mkv", false)]
    public void IsExcludedExtension(string ext, bool expected)
    {
        Assert.Equal(expected, FolderEnumeration.IsExcludedExtension($"/x/file{ext}"));
    }
}
