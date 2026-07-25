using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Core.Presentation;

public static class IssueCategoryRegistry
{
    public static readonly IReadOnlyList<IssueCategory> FilterableCategories =
    [
        IssueCategory.ContainerNaming,
        IssueCategory.Track,
        IssueCategory.FrameRate,
        IssueCategory.VideoColor,
        IssueCategory.Chapter,
    ];

    public static readonly IReadOnlyDictionary<IssueCategory, string> ChineseLabels =
        new Dictionary<IssueCategory, string>
        {
            [IssueCategory.ContainerNaming] = "容器命名",
            [IssueCategory.Track] = "轨道",
            [IssueCategory.FrameRate] = "帧率",
            [IssueCategory.VideoColor] = "视频色彩",
            [IssueCategory.Chapter] = "章节",
            [IssueCategory.Uncategorized] = "未分类",
        };

    private static readonly Dictionary<string, IssueCategory> RuleCategories =
        new(StringComparer.Ordinal)
        {
            [CollationRuleIds.FnResolution] = IssueCategory.ContainerNaming,
            [CollationRuleIds.FnProfile] = IssueCategory.ContainerNaming,
            [CollationRuleIds.FnVideoEncoder] = IssueCategory.ContainerNaming,
            [CollationRuleIds.FnAudioEncoders] = IssueCategory.ContainerNaming,
            [CollationRuleIds.TrackVideoPresent] = IssueCategory.Track,
            [CollationRuleIds.TrackAudioPresent] = IssueCategory.Track,
            [CollationRuleIds.TrackVideoLanguage] = IssueCategory.Track,
            [CollationRuleIds.TrackVideoDefault] = IssueCategory.Track,
            [CollationRuleIds.TrackAudioLanguage] = IssueCategory.Track,
            [CollationRuleIds.TrackAudioDefaultCardinality] = IssueCategory.Track,
            [CollationRuleIds.TrackPgsLanguage] = IssueCategory.Track,
            [CollationRuleIds.TrackPgsDefault] = IssueCategory.Track,
            [CollationRuleIds.VideoScanType] = IssueCategory.FrameRate,
            [CollationRuleIds.VideoColorRange] = IssueCategory.VideoColor,
            [CollationRuleIds.VideoColorMatrix] = IssueCategory.VideoColor,
            [CollationRuleIds.VideoColorReview] = IssueCategory.VideoColor,
            [CollationRuleIds.ChapterLanguageMissing] = IssueCategory.Chapter,
            [CollationRuleIds.ChapterLanguageMixed] = IssueCategory.Chapter,
        };

    /// <summary>
    /// Signal → frozen set of RuleIds that supersede the signal when any mapped finding is present.
    /// Category co-membership is required for every edge.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> SignalSupersession =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [LegacySignalIds.ChapterLanguage] = new HashSet<string>(StringComparer.Ordinal)
            {
                CollationRuleIds.ChapterLanguageMissing,
                CollationRuleIds.ChapterLanguageMixed,
            },
            // Matrix authority: SIG.FpsReview is not superseded by VIDEO.ScanType.
            [LegacySignalIds.FpsReview] = new HashSet<string>(StringComparer.Ordinal),
            [LegacySignalIds.ColorSpace] = new HashSet<string>(StringComparer.Ordinal)
            {
                CollationRuleIds.VideoColorRange,
                CollationRuleIds.VideoColorMatrix,
                CollationRuleIds.VideoColorReview,
            },
            [LegacySignalIds.MultiSubtitle] = new HashSet<string>(StringComparer.Ordinal),
        };

    public static readonly IReadOnlyDictionary<string, IssueCategory> SignalCategories =
        new Dictionary<string, IssueCategory>(StringComparer.Ordinal)
        {
            [LegacySignalIds.ChapterLanguage] = IssueCategory.Chapter,
            [LegacySignalIds.FpsReview] = IssueCategory.FrameRate,
            [LegacySignalIds.ColorSpace] = IssueCategory.VideoColor,
            [LegacySignalIds.MultiSubtitle] = IssueCategory.Track,
        };

    public static IssueCategory CategoryForRuleId(string? ruleId)
    {
        if (ruleId is null)
        {
            return IssueCategory.Uncategorized;
        }

        return RuleCategories.TryGetValue(ruleId, out var cat)
            ? cat
            : IssueCategory.Uncategorized;
    }

    public static IssueCategory CategoryForLegacyFinding(ValidationFinding finding)
    {
        if (finding.RuleId is not null)
        {
            return CategoryForRuleId(finding.RuleId);
        }

        return LegacyFindingMatchers.MatchCategory(finding.Description);
    }

    public static string SeverityLabel(ErrorLevel level) => level switch
    {
        ErrorLevel.Error => "错误",
        ErrorLevel.Warning => "警告",
        ErrorLevel.Info => "信息",
        _ => level.ToString(),
    };

    public static void EnsureSignalCategoryCoMembership()
    {
        foreach (var (signalId, rules) in SignalSupersession)
        {
            var signalCat = SignalCategories[signalId];
            foreach (var ruleId in rules)
            {
                var ruleCat = CategoryForRuleId(ruleId);
                if (ruleCat != signalCat)
                {
                    throw new InvalidOperationException(
                        $"Signal {signalId} category {signalCat} != rule {ruleId} category {ruleCat}");
                }
            }
        }
    }
}

public static class LegacySignalIds
{
    public const string FpsReview = "SIG.FpsReview";
    public const string ColorSpace = "SIG.ColorSpace";
    public const string ChapterLanguage = "SIG.ChapterLanguage";
    public const string MultiSubtitle = "SIG.MultiSubtitle";

    public static readonly IReadOnlyList<string> FixedOrder =
    [
        FpsReview,
        ColorSpace,
        ChapterLanguage,
        MultiSubtitle,
    ];
}

/// <summary>
/// Shared predicates with <see cref="LegacyColorRules"/> — do not duplicate strings elsewhere.
/// </summary>
public static class LegacyFindingMatchers
{
    public const string DescExtMismatchPrefix = "文件后缀和与容器不符";
    public const string DescDelay = "容器中含有延时非 0 的轨道。";
    public const string DescDuration = "轨道间长度相差过大。";
    public const string DescSingleChapter = "文件只有一个章节。";
    public const string DescMultiChapterSets = "文件存在多组章节。";
    public const string DescUselessChapter = "文件末尾有无用章节。";
    public const string DescFirstChapter = "首个章节时间戳非零。";
    public const string DescFilenameMismatch = "内容物和文件名描述不符。";
    public const string DescMultiAudio = "文件含有多条音轨。";

    public static bool IsExtensionMismatch(string description) =>
        description.StartsWith(DescExtMismatchPrefix, StringComparison.Ordinal);

    public static bool IsDelay(string description) => description == DescDelay;
    public static bool IsDuration(string description) => description == DescDuration;
    public static bool IsFilenameMismatch(string description) => description == DescFilenameMismatch;
    public static bool IsMultiAudio(string description) => description == DescMultiAudio;

    public static bool IsChapterLegacy(string description) =>
        description is DescSingleChapter or DescMultiChapterSets or DescUselessChapter or DescFirstChapter;

    public static IssueCategory MatchCategory(string description)
    {
        if (IsExtensionMismatch(description) || IsFilenameMismatch(description))
        {
            return IssueCategory.ContainerNaming;
        }

        if (IsDelay(description) || IsDuration(description) || IsMultiAudio(description))
        {
            return IssueCategory.Track;
        }

        if (IsChapterLegacy(description))
        {
            return IssueCategory.Chapter;
        }

        return IssueCategory.Uncategorized;
    }
}
