namespace MediainfoProjectNg.Next.Domain.Models;

/// <summary>
/// Domain media item. Named to avoid collision with <see cref="System.IO.FileInfo"/>.
/// </summary>
public sealed class MediaFileInfo
{
    public GeneralInfo GeneralInfo { get; }
    public List<VideoInfo> VideoInfos { get; } = new();
    public List<AudioInfo> AudioInfos { get; } = new();
    public List<ChapterInfo> ChapterInfos { get; } = new();
    public List<SubInfo> SubInfos { get; } = new();
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<ValidationFinding> Findings { get; private set; } = Array.Empty<ValidationFinding>();

    /// <summary>
    /// Optional presence-preserving raw snapshot for Collation rules.
    /// </summary>
    public RawMediaSnapshot? RawSnapshot { get; set; }

    /// <summary>
    /// When set, HDR/DVD declared review profile suppresses false SDR hard failures.
    /// </summary>
    public string? DeclaredColorReviewProfile { get; set; }

    public MediaFileInfo(GeneralInfo generalInfo)
    {
        GeneralInfo = generalInfo;
    }

    public void SetFindings(IReadOnlyList<ValidationFinding> findings)
    {
        Findings = findings;
    }
}
