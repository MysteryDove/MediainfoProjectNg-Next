using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Domain.Validation;

/// <summary>
/// Structured Collation evaluation result. Visible findings are projected from
/// <see cref="Violation"/> and selected <see cref="Unverifiable"/> outcomes.
/// </summary>
public sealed class RuleEvaluation
{
    public string RuleId { get; }
    public RuleOutcome Outcome { get; }
    public string PolicyRevision { get; }
    public string? Expected { get; }
    public string? Actual { get; }
    public string? Evidence { get; }
    public ErrorLevel? Severity { get; }
    public string Description { get; }

    public RuleEvaluation(
        string ruleId,
        RuleOutcome outcome,
        string policyRevision,
        string description,
        ErrorLevel? severity = null,
        string? expected = null,
        string? actual = null,
        string? evidence = null)
    {
        RuleId = ruleId;
        Outcome = outcome;
        PolicyRevision = policyRevision;
        Description = description;
        Severity = severity;
        Expected = expected;
        Actual = actual;
        Evidence = evidence;
    }

    public bool IsVisibleFinding =>
        Outcome is RuleOutcome.Violation or RuleOutcome.Unverifiable
        && Severity is not null;
}
