namespace MediainfoProjectNg.Next.Core.Presentation;

public enum IssueCategory
{
    ContainerNaming = 0,
    Track = 1,
    FrameRate = 2,
    VideoColor = 3,
    Chapter = 4,
    Uncategorized = 5,
}

public enum IssueDisplayKind
{
    Finding = 0,
    LegacyReviewSignal = 1,
}

/// <summary>
/// Stable presentation projection for findings discovery. Not a Domain validation type.
/// </summary>
public sealed class IssueDisplayItem
{
    public required string Key { get; init; }
    public required IssueCategory Category { get; init; }
    public required string CategoryLabel { get; init; }
    public required string Description { get; init; }
    public required IssueDisplayKind Kind { get; init; }
    public string? SeverityLabel { get; init; }
    public string? RuleId { get; init; }
    public string? SignalId { get; init; }
    public string? Expected { get; init; }
    public string? Actual { get; init; }
    /// <summary>Structured evidence/provenance from Collation evaluations (test-spec case 31).</summary>
    public string? Evidence { get; init; }
    public bool IsFilterable => Category != IssueCategory.Uncategorized;
}
