using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Domain.Models;

/// <summary>
/// Semantic validation result. UI maps <see cref="Level"/> to theme colors (no brushes in domain).
/// Collation instances may expose optional structured fields; Legacy construction remains source-compatible.
/// </summary>
public sealed class ValidationFinding
{
    public ErrorLevel Level { get; }
    public string Description { get; }

    public string? RuleId { get; }
    public RuleOutcome? Outcome { get; }
    public string? PolicyRevision { get; }
    public string? Expected { get; }
    public string? Actual { get; }
    public string? Evidence { get; }

    public ValidationFinding(ErrorLevel level, string description)
    {
        Level = level;
        Description = description;
    }

    public ValidationFinding(
        ErrorLevel level,
        string description,
        string ruleId,
        RuleOutcome outcome,
        string policyRevision,
        string? expected = null,
        string? actual = null,
        string? evidence = null)
    {
        Level = level;
        Description = description;
        RuleId = ruleId;
        Outcome = outcome;
        PolicyRevision = policyRevision;
        Expected = expected;
        Actual = actual;
        Evidence = evidence;
    }

    public static ValidationFinding FromEvaluation(RuleEvaluation evaluation)
    {
        if (!evaluation.IsVisibleFinding || evaluation.Severity is null)
        {
            throw new InvalidOperationException(
                $"Evaluation {evaluation.RuleId} is not a visible finding.");
        }

        return new ValidationFinding(
            evaluation.Severity.Value,
            evaluation.Description,
            evaluation.RuleId,
            evaluation.Outcome,
            evaluation.PolicyRevision,
            evaluation.Expected,
            evaluation.Actual,
            evaluation.Evidence);
    }
}
