using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Tests.Validation;

public class CollationPolicyMatrixTests
{
    [Fact]
    public void MatrixFile_MatchesPinnedHash_AndRequiredFields()
    {
        var path = FindMatrixPath();
        Assert.True(File.Exists(path), $"Missing matrix at {path}");
        var json = File.ReadAllText(path);
        Assert.True(
            CollationPolicyMatrix.TryValidateMatrixJson(json, out var hash, out var error),
            error);
        Assert.Equal(CollationPolicyMatrix.ExpectedMatrixSha256, hash);
    }

    [Fact]
    public void EnabledRules_HaveUniqueIds_AndKnownOrder()
    {
        var ids = CollationRuleIds.EnabledOrder;
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, id => Assert.True(CollationPolicyMatrix.IsRuleEnabled(id)));
    }

    [Fact]
    public void MkaAndMp4_AreDisabled_InPhase0()
    {
        Assert.False(CollationPolicyMatrix.IsMkaEnabled);
        Assert.False(CollationPolicyMatrix.IsMp4Enabled);
        Assert.Contains(CollationRuleIds.MkaAudioOnlyDefaults, CollationPolicyMatrix.DisabledRuleIds);
        Assert.Contains(CollationRuleIds.Mp4MobileTrackLayout, CollationPolicyMatrix.DisabledRuleIds);
    }

    [Fact]
    public void ResolutionBuckets_IncludePinnedCropException()
    {
        Assert.Equal("1080p", CollationPolicyMatrix.MapResolutionBucket(1920, 1080));
        Assert.Equal("1080p", CollationPolicyMatrix.MapResolutionBucket(1920, 1072));
        Assert.Equal("720p", CollationPolicyMatrix.MapResolutionBucket(1280, 720));
        Assert.Null(CollationPolicyMatrix.MapResolutionBucket(1918, 1078));
    }

    [Fact]
    public void RuntimeSdrDefaults_MatchPinnedMatrix()
    {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(FindMatrixPath()));
        var defaults = document.RootElement.GetProperty("sdrDefaults");

        AssertSet(defaults, "colourRange", CollationPolicyMatrix.SdrColourRanges);
        AssertSet(defaults, "matrixCoefficients", CollationPolicyMatrix.SdrMatrixCoefficients);
        AssertSet(defaults, "transferCharacteristics", CollationPolicyMatrix.SdrTransferCharacteristics, ignoreEmpty: true);
        AssertSet(defaults, "colourPrimaries", CollationPolicyMatrix.SdrColourPrimaries, ignoreEmpty: true);
    }

    [Fact]
    public void ApprovalRecord_Exists_WithHashAndApprover()
    {
        var path = Path.Combine(FindRepoRoot(), ".omx", "specs", "collation-v1-policy-approval.md");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains(CollationPolicyMatrix.ExpectedMatrixSha256, text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Owen", text, StringComparison.Ordinal);
        Assert.Contains(CollationPolicyMatrix.UpstreamPin, text, StringComparison.Ordinal);
        Assert.Contains("APPROVED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DualWrite_GrammarAllowlists_MatchParser()
    {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(FindMatrixPath()));
        var allow = document.RootElement
            .GetProperty("filenameGrammarAllowlists")
            .GetProperty("vcbs-mkv-release-v1");
        var profiles = allow.GetProperty("profiles").EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty).ToHashSet(StringComparer.Ordinal);
        var encoders = allow.GetProperty("videoEncoders").EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty).ToHashSet(StringComparer.Ordinal);
        var resolutions = allow.GetProperty("resolutions").EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(CollationFilenameParser.SupportedProfiles.SetEquals(profiles));
        Assert.True(CollationFilenameParser.SupportedVideoEncoders.SetEquals(encoders));
        Assert.True(CollationPolicyMatrix.ResolutionBuckets.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)
            .SetEquals(resolutions));
    }

    [Fact]
    public void DualWrite_Waivers_And_FpsSupersession_And_Applicability()
    {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(FindMatrixPath()));
        var root = document.RootElement;
        var waivers = root.GetProperty("approvedColorReviewProfiles").EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.True(waivers.SetEquals(CollationPolicyMatrix.ApprovedColorReviewProfiles));

        var fps = root.GetProperty("signalSupersession").GetProperty("SIG.FpsReview");
        Assert.Equal(0, fps.GetArrayLength());
        Assert.Empty(MediainfoProjectNg.Next.Core.Presentation.IssueCategoryRegistry
            .SignalSupersession[MediainfoProjectNg.Next.Core.Presentation.LegacySignalIds.FpsReview]);

        Assert.True(root.TryGetProperty("applicabilityPredicates", out _));
        foreach (var rule in root.GetProperty("rules").EnumerateArray())
        {
            if (!rule.GetProperty("enabled").GetBoolean())
            {
                continue;
            }

            var id = rule.GetProperty("ruleId").GetString()!;
            var app = rule.GetProperty("applicability").GetString()!;
            Assert.Equal(app, CollationApplicability.EnabledRuleApplicability[id]);
        }
    }

    private static string FindMatrixPath() =>
        Path.Combine(FindRepoRoot(), ".omx", "specs", "collation-v1-policy-matrix.json");

    private static void AssertSet(
        System.Text.Json.JsonElement defaults,
        string property,
        IReadOnlySet<string> actual,
        bool ignoreEmpty = false)
    {
        var expected = defaults.GetProperty(property)
            .EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .Where(value => !ignoreEmpty || value.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.True(expected.SetEquals(actual));
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
