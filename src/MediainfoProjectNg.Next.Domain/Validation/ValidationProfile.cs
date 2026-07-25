namespace MediainfoProjectNg.Next.Domain.Validation;

/// <summary>
/// Explicit validation profile. Callers that omit a profile remain <see cref="LegacyV1"/>.
/// </summary>
public enum ValidationProfile
{
    LegacyV1 = 0,
    CollationV1 = 1,
}
