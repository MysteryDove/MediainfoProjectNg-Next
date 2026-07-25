using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Core.Presentation;

/// <summary>
/// Projects Domain findings + presentation-only legacy cell signals into ordered display items.
/// </summary>
public static class IssueDisplayProjector
{
    public const int TooltipMaxItems = 4;
    public const int TooltipMaxTextElements = 120;

    public static IReadOnlyList<IssueDisplayItem> Project(MediaFileInfo model)
    {
        var items = new List<IssueDisplayItem>();
        var presentRuleIds = new HashSet<string>(StringComparer.Ordinal);

        var index = 0;
        foreach (var finding in model.Findings)
        {
            var category = IssueCategoryRegistry.CategoryForLegacyFinding(finding);
            if (finding.RuleId is not null)
            {
                presentRuleIds.Add(finding.RuleId);
            }

            items.Add(new IssueDisplayItem
            {
                Key = finding.RuleId is not null
                    ? $"rule:{finding.RuleId}:{index}"
                    : $"legacy:{index}:{StableToken(finding.Description)}",
                Category = category,
                CategoryLabel = IssueCategoryRegistry.ChineseLabels[category],
                Description = finding.Description,
                Kind = IssueDisplayKind.Finding,
                SeverityLabel = IssueCategoryRegistry.SeverityLabel(finding.Level),
                RuleId = finding.RuleId,
                Expected = finding.Expected,
                Actual = finding.Actual,
                Evidence = finding.Evidence,
            });
            index++;
        }

        var video = model.VideoInfos.Count > 0 ? model.VideoInfos[0] : null;
        foreach (var signalId in LegacySignalIds.FixedOrder)
        {
            if (!ShouldEmitSignal(signalId, model, video))
            {
                continue;
            }

            if (IssueCategoryRegistry.SignalSupersession.TryGetValue(signalId, out var superseding)
                && superseding.Any(presentRuleIds.Contains))
            {
                continue;
            }

            var category = IssueCategoryRegistry.SignalCategories[signalId];
            items.Add(new IssueDisplayItem
            {
                Key = $"signal:{signalId}",
                Category = category,
                CategoryLabel = IssueCategoryRegistry.ChineseLabels[category],
                Description = SignalDescription(signalId, model, video),
                Kind = IssueDisplayKind.LegacyReviewSignal,
                SeverityLabel = "检查提示",
                SignalId = signalId,
            });
        }

        return items;
    }

    public static IReadOnlyList<string> BuildTooltipLines(IReadOnlyList<IssueDisplayItem> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var lines = new List<string>();
        var emitted = 0;
        foreach (var group in items.GroupBy(i => i.Category))
        {
            if (emitted >= TooltipMaxItems)
            {
                break;
            }

            lines.Add($"{group.First().CategoryLabel} ({group.Count()})");
            foreach (var item in group)
            {
                if (emitted >= TooltipMaxItems)
                {
                    break;
                }

                lines.Add($"  {Truncate(item.Description)}");
                emitted++;
            }
        }

        if (items.Count > TooltipMaxItems)
        {
            var n = items.Count - TooltipMaxItems;
            lines.Add($"另有 {n} 条，选中查看全部");
        }

        return lines;
    }

    public static string Truncate(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var count = 0;
        var acc = new List<string>();
        while (enumerator.MoveNext())
        {
            count++;
            if (count > TooltipMaxTextElements)
            {
                return string.Concat(acc) + "…";
            }

            acc.Add(enumerator.GetTextElement());
        }

        return text;
    }

    private static bool ShouldEmitSignal(string signalId, MediaFileInfo model, VideoInfo? video) =>
        signalId switch
        {
            LegacySignalIds.FpsReview => LegacyColorRules.FpsColorToken(video) != ColorToken.None,
            LegacySignalIds.ColorSpace => LegacyColorRules.ColorSpaceColorToken(video) != ColorToken.None,
            LegacySignalIds.ChapterLanguage =>
                LegacyColorRules.ChapterLanguageBackgroundToken(model.ChapterInfos) != ColorToken.None,
            LegacySignalIds.MultiSubtitle => model.GeneralInfo.TextCount > 1,
            _ => false,
        };

    private static string SignalDescription(string signalId, MediaFileInfo model, VideoInfo? video) =>
        signalId switch
        {
            LegacySignalIds.FpsReview => $"帧率提示: {LegacyColorRules.FpsDisplayText(video)}",
            LegacySignalIds.ColorSpace => $"色彩格式提示: {video?.ColorSpace}",
            LegacySignalIds.ChapterLanguage => "章节语言提示",
            LegacySignalIds.MultiSubtitle => "多字幕轨道提示",
            _ => signalId,
        };

    /// <summary>
    /// Process-stable identity token for legacy descriptions (not randomized GetHashCode).
    /// </summary>
    public static string StableToken(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
