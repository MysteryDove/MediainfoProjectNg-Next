using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MediainfoProjectNg.Next.Domain.Validation;

/// <summary>
/// Phase 0 pinned policy matrix. The embedded content is the implementation authority;
/// the on-disk JSON under .omx/specs is the review artifact and must match the hash.
/// </summary>
public static class CollationPolicyMatrix
{
    public const string PolicyRevision = "CollationV1@2cb203644dd4a05335fe4551b1086304f9f623a9";
    public const string UpstreamPin = "2cb203644dd4a05335fe4551b1086304f9f623a9";
    public const string ExpectedMatrixSha256 = "60adfb54d3295e25dbad62ae8dc840335cee8677ff6536df8f0b3d762fd000c6";
    public const string LegacyFilenameMismatchDescription = "内容物和文件名描述不符。";

    public static readonly IReadOnlySet<string> EnabledRuleIds =
        CollationRuleIds.EnabledOrder.ToHashSet(StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> DisabledRuleIds =
        new HashSet<string>(StringComparer.Ordinal)
        {
            CollationRuleIds.MkaAudioOnlyDefaults,
            CollationRuleIds.Mp4MobileTrackLayout,
        };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(long Width, long Height)>> ResolutionBuckets =
        new Dictionary<string, IReadOnlyList<(long, long)>>(StringComparer.OrdinalIgnoreCase)
        {
            ["1080p"] = [(1920, 1080), (1920, 1072)],
            ["720p"] = [(1280, 720)],
            ["576p"] = [(720, 576)],
            ["480p"] = [(720, 480)],
        };

    public static readonly IReadOnlySet<string> SdrColourRanges =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Limited", "Full" };

    public static readonly IReadOnlySet<string> SdrMatrixCoefficients =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BT.709", "BT.601" };

    public static readonly IReadOnlySet<string> SdrTransferCharacteristics =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BT.709", "BT.601" };

    public static readonly IReadOnlySet<string> SdrColourPrimaries =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BT.709", "BT.601" };

    /// <summary>
    /// Only these declared review profiles suppress VIDEO.ColorReview SDR-deviation hard failures.
    /// Arbitrary non-empty strings must not disable the advisory rule.
    /// </summary>
    public static readonly IReadOnlySet<string> ApprovedColorReviewProfiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HDR",
            "HDR10",
            "HDR10+",
            "HLG",
            "Dolby Vision",
            "DV",
            "DVD",
            "DVD-Video",
        };

    public static bool IsApprovedColorReviewProfile(string? declared) =>
        !string.IsNullOrWhiteSpace(declared) && ApprovedColorReviewProfiles.Contains(declared.Trim());

    public static bool IsRuleEnabled(string ruleId) => EnabledRuleIds.Contains(ruleId);

    public static bool IsMkaEnabled => false;
    public static bool IsMp4Enabled => false;
    public static bool IsMenuPgsExemptionEnabled => false;

    public static string? MapResolutionBucket(long width, long height)
    {
        foreach (var (bucket, dims) in ResolutionBuckets)
        {
            if (dims.Any(d => d.Width == width && d.Height == height))
            {
                return bucket;
            }
        }

        return null;
    }

    public static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool TryValidateMatrixJson(string json, out string actualHash, out string? error)
    {
        actualHash = ComputeSha256(NormalizeNewlines(json));
        // Also accept raw file hash without newline normalization for exact file bytes.
        var rawHash = ComputeSha256(json);
        if (!string.Equals(rawHash, ExpectedMatrixSha256, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(actualHash, ExpectedMatrixSha256, StringComparison.OrdinalIgnoreCase))
        {
            // Prefer reporting the raw file hash used by approval record.
            actualHash = rawHash;
            error = $"Matrix hash mismatch. Expected {ExpectedMatrixSha256}, got {rawHash}.";
            return false;
        }

        actualHash = rawHash;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
            {
                error = "Matrix missing rules array.";
                return false;
            }

            foreach (var rule in rules.EnumerateArray())
            {
                foreach (var required in new[]
                         {
                             "ruleId", "class", "applicability", "outcomeOnFail", "severity",
                             "upstreamClauseUrl", "expectedEvidence", "exceptions", "supersedesLegacy", "enabled",
                         })
                {
                    if (!rule.TryGetProperty(required, out _))
                    {
                        error = $"Rule missing required field '{required}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string NormalizeNewlines(string s) => s.Replace("\r\n", "\n", StringComparison.Ordinal);
}
