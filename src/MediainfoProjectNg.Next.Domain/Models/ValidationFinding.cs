namespace MediainfoProjectNg.Next.Domain.Models;

/// <summary>
/// Semantic validation result. UI maps <see cref="Level"/> to theme colors (no brushes in domain).
/// </summary>
public sealed class ValidationFinding
{
    public ErrorLevel Level { get; }
    public string Description { get; }

    public ValidationFinding(ErrorLevel level, string description)
    {
        Level = level;
        Description = description;
    }
}
