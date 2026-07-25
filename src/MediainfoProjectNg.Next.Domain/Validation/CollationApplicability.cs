using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Domain.Validation;

/// <summary>
/// Pure applicability predicates projected from Stage 0 matrix
/// <c>applicabilityPredicates</c>. No path/title semantics.
/// </summary>
public static class CollationApplicability
{
    public const string RecognizedFilename = "RecognizedVcbsMkvFilename";
    public const string RecognizedFilenameAndContainer = "RecognizedVcbsMkvFilenameAndContainer";
    public const string RecognizedFilenameAndContainerWithChapters =
        "RecognizedVcbsMkvFilenameAndContainerWithChapters";
    public const string DisabledUntilDedicatedGrammar = "DisabledUntilDedicatedGrammar";

    public static readonly IReadOnlyDictionary<string, string> EnabledRuleApplicability =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CollationRuleIds.FnResolution] = RecognizedFilename,
            [CollationRuleIds.FnProfile] = RecognizedFilename,
            [CollationRuleIds.FnVideoEncoder] = RecognizedFilename,
            [CollationRuleIds.FnAudioEncoders] = RecognizedFilename,
            [CollationRuleIds.TrackVideoPresent] = RecognizedFilenameAndContainer,
            [CollationRuleIds.TrackAudioPresent] = RecognizedFilenameAndContainer,
            [CollationRuleIds.TrackVideoLanguage] = RecognizedFilenameAndContainer,
            [CollationRuleIds.TrackVideoDefault] = RecognizedFilenameAndContainer,
            [CollationRuleIds.TrackAudioLanguage] = RecognizedFilenameAndContainer,
            [CollationRuleIds.TrackAudioDefaultCardinality] = RecognizedFilenameAndContainer,
            [CollationRuleIds.TrackPgsLanguage] = RecognizedFilenameAndContainer,
            [CollationRuleIds.TrackPgsDefault] = RecognizedFilenameAndContainer,
            [CollationRuleIds.VideoScanType] = RecognizedFilenameAndContainer,
            [CollationRuleIds.VideoColorRange] = RecognizedFilenameAndContainer,
            [CollationRuleIds.VideoColorMatrix] = RecognizedFilenameAndContainer,
            [CollationRuleIds.VideoColorReview] = RecognizedFilenameAndContainer,
            [CollationRuleIds.ChapterLanguageMissing] = RecognizedFilenameAndContainerWithChapters,
            [CollationRuleIds.ChapterLanguageMixed] = RecognizedFilenameAndContainerWithChapters,
        };

    public static bool IsApplicable(string applicabilityId, MediaFileInfo info, bool grammarRecognized)
    {
        if (string.Equals(applicabilityId, DisabledUntilDedicatedGrammar, StringComparison.Ordinal))
        {
            return false;
        }

        if (!grammarRecognized)
        {
            return false;
        }

        var extension = Path.GetExtension(info.GeneralInfo.FullPath);
        if (!string.Equals(extension, ".mkv", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(applicabilityId, RecognizedFilename, StringComparison.Ordinal))
        {
            return true;
        }

        if (!IsMatroskaContainer(info))
        {
            return false;
        }

        if (string.Equals(applicabilityId, RecognizedFilenameAndContainer, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(applicabilityId, RecognizedFilenameAndContainerWithChapters, StringComparison.Ordinal))
        {
            return ChaptersPresent(info);
        }

        return false;
    }

    public static bool IsMatroskaContainer(MediaFileInfo info)
    {
        var format = info.RawSnapshot?.ContainerFormat.TextOrEmpty
                     ?? info.GeneralInfo.Format
                     ?? string.Empty;
        return string.Equals(format, "Matroska", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Chapters present: ChapterCount &gt; 0 or projected chapter rows non-empty.
    /// When count &gt; 0 but rows empty, still applicable (outcomes Unverifiable).
    /// </summary>
    public static bool ChaptersPresent(MediaFileInfo info)
    {
        var raw = info.RawSnapshot;
        if (raw is not null)
        {
            return raw.ChapterCount > 0 || raw.Chapters.Count > 0;
        }

        return info.GeneralInfo.ChapterCount > 0 || info.ChapterInfos.Count > 0;
    }
}
