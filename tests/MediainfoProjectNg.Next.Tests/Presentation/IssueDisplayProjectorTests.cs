using MediainfoProjectNg.Next.Core.Presentation;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Tests.Presentation;

public class IssueDisplayProjectorTests
{
    [Fact]
    public void StructuredRuleId_MapsToExactCategory()
    {
        var finding = new ValidationFinding(
            ErrorLevel.Error, "res", CollationRuleIds.FnResolution, RuleOutcome.Violation,
            CollationPolicyMatrix.PolicyRevision);
        Assert.Equal(IssueCategory.ContainerNaming, IssueCategoryRegistry.CategoryForLegacyFinding(finding));
        Assert.Equal(IssueCategory.Track,
            IssueCategoryRegistry.CategoryForRuleId(CollationRuleIds.TrackVideoLanguage));
        Assert.Equal(IssueCategory.Uncategorized,
            IssueCategoryRegistry.CategoryForRuleId("FUTURE.Unknown"));
    }

    [Fact]
    public void EveryEnabledRuleId_MapsToOneFilterableCategory()
    {
        foreach (var ruleId in CollationRuleIds.EnabledOrder)
        {
            var category = IssueCategoryRegistry.CategoryForRuleId(ruleId);
            Assert.NotEqual(IssueCategory.Uncategorized, category);
            Assert.Contains(category, IssueCategoryRegistry.FilterableCategories);
        }
    }

    [Fact]
    public void Project_CopiesEvidence_AndStableLegacyKeys()
    {
        var model = new MediaFileInfo(new GeneralInfo("s", "/s.mkv", "Matroska", 0, 1, 1, 0, 0));
        model.SetFindings(
        [
            new ValidationFinding(
                ErrorLevel.Error,
                "分辨率声明与内容不符",
                CollationRuleIds.FnResolution,
                RuleOutcome.Violation,
                CollationPolicyMatrix.PolicyRevision,
                expected: "1080p",
                actual: "720p",
                evidence: "raw width=1280 height=720"),
            new ValidationFinding(ErrorLevel.Warning, "容器中含有延时非 0 的轨道。"),
        ]);

        var items = IssueDisplayProjector.Project(model);
        var structured = items.Single(i => i.RuleId == CollationRuleIds.FnResolution);
        Assert.Equal("raw width=1280 height=720", structured.Evidence);
        Assert.Equal("1080p", structured.Expected);
        Assert.Equal("720p", structured.Actual);

        var legacy = items.Single(i => i.RuleId is null && i.Kind == IssueDisplayKind.Finding);
        Assert.StartsWith("legacy:1:", legacy.Key, StringComparison.Ordinal);
        var token = IssueDisplayProjector.StableToken("容器中含有延时非 0 的轨道。");
        Assert.Equal($"legacy:1:{token}", legacy.Key);
        Assert.Equal(token, IssueDisplayProjector.StableToken("容器中含有延时非 0 的轨道。"));
    }

    [Fact]
    public void LegacyExtensionMismatch_MapsViaSharedPrefixPredicate()
    {
        var finding = new ValidationFinding(
            ErrorLevel.Error, "文件后缀和与容器不符。后缀：.mp4，容器Matroska");
        Assert.Equal(IssueCategory.ContainerNaming, IssueCategoryRegistry.CategoryForLegacyFinding(finding));
        Assert.Equal(ColorToken.ErrorRed, LegacyColorRules.TokenForFinding(finding));
    }

    [Fact]
    public void UnknownOnly_DoesNotAffectFiveCounts()
    {
        var items = new List<IssueDisplayItem>
        {
            new()
            {
                Key = "u",
                Category = IssueCategory.Uncategorized,
                CategoryLabel = "未分类",
                Description = "unknown",
                Kind = IssueDisplayKind.Finding,
                RuleId = "FUTURE.X",
            },
        };
        var counts = CategoryFilterEngine.ComputeDistinctFileCounts([items]);
        Assert.All(counts.Values, c => Assert.Equal(0, c));
        Assert.True(CategoryFilterEngine.RowMatches(items, new HashSet<IssueCategory>()));
        Assert.False(CategoryFilterEngine.RowMatches(items, new HashSet<IssueCategory> { IssueCategory.Track }));
    }

    [Fact]
    public void OrFilter_UnionWithoutDuplicates()
    {
        var track = new IssueDisplayItem
        {
            Key = "t", Category = IssueCategory.Track, CategoryLabel = "轨道",
            Description = "t", Kind = IssueDisplayKind.Finding,
        };
        var chapter = new IssueDisplayItem
        {
            Key = "c", Category = IssueCategory.Chapter, CategoryLabel = "章节",
            Description = "c", Kind = IssueDisplayKind.Finding,
        };
        var rows = new[]
        {
            (Id: "a", Items: (IReadOnlyList<IssueDisplayItem>)[track]),
            (Id: "b", Items: (IReadOnlyList<IssueDisplayItem>)[chapter]),
            (Id: "c", Items: (IReadOnlyList<IssueDisplayItem>)[track, chapter]),
        };

        var active = new HashSet<IssueCategory> { IssueCategory.Track, IssueCategory.Chapter };
        var matched = rows.Where(r => CategoryFilterEngine.RowMatches(r.Items, active)).Select(r => r.Id).ToList();
        Assert.Equal(3, matched.Count);
        Assert.Equal(matched.Distinct().Count(), matched.Count);
    }

    [Fact]
    public void SignalSupersession_OmitsDuplicateDisplay_KeepsCategoryCoMembership()
    {
        IssueCategoryRegistry.EnsureSignalCategoryCoMembership();

        var model = new MediaFileInfo(new GeneralInfo("s", "/s.mkv", "Matroska", 0, 1, 1, 0, 2));
        model.VideoInfos.Add(new VideoInfo(
            "HEVC", "", "CFR", "23.976", 0, 10, 1000, 1080, 1920, "UND", 0,
            new ProfileInfo(""), "YUV420", "Yes"));
        model.ChapterInfos.Add(new ChapterInfo(0, "a", ""));
        model.ChapterInfos.Add(new ChapterInfo(1, "b", "ENG"));
        model.SetFindings(
        [
            new ValidationFinding(
                ErrorLevel.Warning, "章节语言缺失", CollationRuleIds.ChapterLanguageMissing,
                RuleOutcome.Violation, CollationPolicyMatrix.PolicyRevision),
        ]);

        var items = IssueDisplayProjector.Project(model);
        Assert.Contains(items, i => i.RuleId == CollationRuleIds.ChapterLanguageMissing);
        Assert.DoesNotContain(items, i => i.SignalId == LegacySignalIds.ChapterLanguage);
    }

    [Fact]
    public void Tooltip_TruncatesAndOverflows()
    {
        var items = Enumerable.Range(0, 6)
            .Select(i => new IssueDisplayItem
            {
                Key = i.ToString(),
                Category = IssueCategory.Track,
                CategoryLabel = "轨道",
                Description = new string('字', 200),
                Kind = IssueDisplayKind.Finding,
            })
            .ToList();

        var lines = IssueDisplayProjector.BuildTooltipLines(items);
        Assert.Equal(6, lines.Count); // category header + 4 descriptions + overflow
        Assert.Equal("轨道 (6)", lines[0]);
        Assert.Contains("另有 2 条", lines[^1]);
        Assert.True(lines[1].Length < 200);
    }

    [Fact]
    public void SelectionReconciler_PrimaryExcluded_ClearsAll()
    {
        var a = new object();
        var b = new object();
        var visible = new HashSet<object> { b };
        var (primary, selected) = SelectionReconciler.Reconcile(a, [a, b], visible);
        Assert.Null(primary);
        Assert.Empty(selected);
    }

    [Fact]
    public void SelectionReconciler_PrimaryRemains_DropsHiddenSecondary()
    {
        var a = new object();
        var b = new object();
        var visible = new HashSet<object> { a };
        var (primary, selected) = SelectionReconciler.Reconcile(a, [a, b], visible);
        Assert.Same(a, primary);
        Assert.Single(selected);
        Assert.Same(a, selected[0]);
    }

    [Fact]
    public void Counts_DoNotChangeWhenFiltersToggle()
    {
        var itemsA = new List<IssueDisplayItem>
        {
            new()
            {
                Key = "1", Category = IssueCategory.Track, CategoryLabel = "轨道",
                Description = "t", Kind = IssueDisplayKind.Finding,
            },
        };
        var itemsB = new List<IssueDisplayItem>
        {
            new()
            {
                Key = "2", Category = IssueCategory.Chapter, CategoryLabel = "章节",
                Description = "c", Kind = IssueDisplayKind.Finding,
            },
        };
        var counts1 = CategoryFilterEngine.ComputeDistinctFileCounts([itemsA, itemsB]);
        var counts2 = CategoryFilterEngine.ComputeDistinctFileCounts([itemsA, itemsB]);
        Assert.Equal(counts1[IssueCategory.Track], counts2[IssueCategory.Track]);
        Assert.Equal(1, counts1[IssueCategory.Track]);
        Assert.Equal(1, counts1[IssueCategory.Chapter]);
    }
}
