using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.MediaInfo;

namespace MediainfoProjectNg.Next.Tests.MediaInfo;

/// <summary>
/// Native adapter lane. CI job <c>native-projection</c> runs this class fail-closed.
/// Local runs use <see cref="SkippableFactAttribute"/> + <see cref="Skip.If"/> so missing library/fixture
/// is recorded as an explicit skip with evidence, not a silent pass.
/// </summary>
public class MediaInfoProjectionIntegrationTests
{
    public static bool IsCiNativeLane =>
        string.Equals(Environment.GetEnvironmentVariable("MPNG_NATIVE_PROJECTION_REQUIRED"), "1",
            StringComparison.Ordinal);

    [SkippableFact]
    public void NativeLibrary_ProjectsRequiredFields_WhenAvailable()
    {
        var reader = new MediaInfoMetadataReader();
        var version = reader.GetLibraryVersion();
        if (string.IsNullOrWhiteSpace(version))
        {
            if (IsCiNativeLane)
            {
                Assert.Fail("Native MediaInfo library missing in required CI native-projection lane.");
            }

            Skip.If(true,
                "SKIP evidence: native MediaInfo library unavailable locally (GetLibraryVersion empty).");
        }

        var fixture = FindOptionalFixture();
        if (fixture is null)
        {
            if (IsCiNativeLane)
            {
                Assert.Fail("Media fixture missing in required CI native-projection lane (fixtures/media).");
            }

            Skip.If(true,
                "SKIP evidence: no media fixture under fixtures/media for local native projection.");
        }

        MediaFileInfo info;
        try
        {
            info = reader.Read(fixture!);
        }
        catch (Exception ex) when (!IsCiNativeLane)
        {
            Skip.If(true,
                $"SKIP evidence: native Read failed locally: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        Assert.NotNull(info.RawSnapshot);
        Assert.False(info.RawSnapshot!.AdapterUnavailable);
        Assert.False(string.IsNullOrWhiteSpace(info.GeneralInfo.Format));
        Assert.NotEmpty(info.VideoInfos);
        Assert.NotEmpty(info.RawSnapshot.VideoTracks);

        var raw = info.RawSnapshot.VideoTracks[0];
        // The generated CI fixture declares every field below. Access-only checks would
        // still pass if the adapter queried a wrong MediaInfo key and returned Absent.
        Assert.True(raw.ScanType.IsPresent, "ScanType was not projected from the native fixture.");
        Assert.True(raw.ColourRange.IsPresent, "ColourRange was not projected from the native fixture.");
        Assert.True(raw.MatrixCoefficients.IsPresent, "MatrixCoefficients was not projected from the native fixture.");
        Assert.True(raw.ColourPrimaries.IsPresent, "ColourPrimaries was not projected from the native fixture.");
        Assert.True(raw.TransferCharacteristics.IsPresent,
            "TransferCharacteristics was not projected from the native fixture.");
        Assert.True(raw.Language.IsPresent, "Video language was not projected from the native fixture.");
        Assert.True(raw.Default.IsPresent, "Video default flag was not projected from the native fixture.");

        Assert.NotEmpty(info.RawSnapshot.AudioTracks);
        var audio = info.RawSnapshot.AudioTracks[0];
        Assert.True(audio.Format.IsPresent, "Audio Format was not projected from the native fixture.");
        Assert.True(audio.Language.IsPresent, "Audio language was not projected from the native fixture.");
        Assert.True(audio.Default.IsPresent, "Audio default flag was not projected from the native fixture.");
    }

    private static string? FindOptionalFixture()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "fixtures", "media");
        if (!Directory.Exists(dir))
        {
            return null;
        }

        return Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
            .FirstOrDefault(f =>
                f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MediainfoProjectNg.Next.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
