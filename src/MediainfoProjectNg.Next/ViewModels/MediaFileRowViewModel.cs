using System.Text.RegularExpressions;
using Avalonia.Media;
using MediainfoProjectNg.Next.Converters;
using MediainfoProjectNg.Next.Core.Presentation;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.ViewModels;

/// <summary>
/// Safe row projection for the main DataGrid. File identity remains available on every row,
/// while continuation rows expose empty file-level display fields.
/// </summary>
public sealed class MediaFileRowViewModel
{
    private static readonly Regex MenuTagPattern = new(
        @"\[Menu[^\]]*\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public MediaFileRowViewModel(MediaFileInfo model, int trackIndex = 0)
        : this(model, trackIndex, isMenuPgsAggregate: false, sharedProjection: null)
    {
    }

    private MediaFileRowViewModel(
        MediaFileInfo model,
        int trackIndex,
        bool isMenuPgsAggregate,
        MediaFileRowViewModel? sharedProjection)
    {
        Model = model;
        TrackIndex = trackIndex;
        IsContinuation = trackIndex > 0;
        IsMenuPgsAggregate = isMenuPgsAggregate;

        var video = IsContinuation ? null : FirstVideo();
        FpsText = LegacyColorRules.FpsDisplayText(video);
        FpsColorToken = LegacyColorRules.FpsColorToken(video);
        ColorSpaceColorToken = LegacyColorRules.ColorSpaceColorToken(video);
        ChapterLanguage = IsContinuation
            ? string.Empty
            : LegacyColorRules.ChapterLanguageDisplay(model.ChapterInfos);
        ChapterLanguageBgToken = IsContinuation
            ? ColorToken.None
            : LegacyColorRules.ChapterLanguageBackgroundToken(model.ChapterInfos);

        var findingBackground = LegacyColorRules.FirstFindingBackgroundToken(model.Findings);
        RowBackgroundToken = findingBackground == ColorToken.None && isMenuPgsAggregate
            ? ColorToken.WarningDelayTeal
            : findingBackground;
        RowForegroundToken = LegacyColorRules.RowForegroundToken(model.GeneralInfo.TextCount);

        IssueItems = sharedProjection?.IssueItems ?? IssueDisplayProjector.Project(model);
        TooltipLines = sharedProjection?.TooltipLines ?? IssueDisplayProjector.BuildTooltipLines(IssueItems);
        TooltipText = sharedProjection?.TooltipText
            ?? (TooltipLines.Count == 0 ? null : string.Join('\n', TooltipLines));
        HasTooltip = TooltipText is not null;

        RowBackgroundBrush = ColorTokenToBrushConverter.TokenToBackgroundBrush(RowBackgroundToken);
        RowForegroundBrush = RowBackgroundToken != ColorToken.None
            ? ColorTokenToBrushConverter.TokenToForegroundBrush(RowBackgroundToken)
            : RowForegroundToken == ColorToken.ForegroundMultiSub
                ? ColorTokenToBrushConverter.TokenToAccentBrush(RowForegroundToken)
                : ColorTokenToBrushConverter.DefaultForegroundBrush();
        FpsForegroundBrush = RowBackgroundToken != ColorToken.None
            ? RowForegroundBrush
            : FpsColorToken == ColorToken.None
            ? RowForegroundBrush
            : ColorTokenToBrushConverter.TokenToAccentBrush(FpsColorToken);
        ColorSpaceForegroundBrush = RowBackgroundToken != ColorToken.None
            ? RowForegroundBrush
            : ColorSpaceColorToken == ColorToken.None
            ? RowForegroundBrush
            : ColorTokenToBrushConverter.TokenToAccentBrush(ColorSpaceColorToken);
        ChapterLanguageBackgroundBrush = ColorTokenToBrushConverter.TokenToBackgroundBrush(
            ChapterLanguageBgToken);
        ChapterLanguageForegroundBrush = ChapterLanguageBgToken == ColorToken.None
            ? RowForegroundBrush
            : ColorTokenToBrushConverter.TokenToForegroundBrush(ChapterLanguageBgToken);
    }

    public MediaFileInfo Model { get; }
    public int TrackIndex { get; }
    public bool IsContinuation { get; }
    public bool IsMenuPgsAggregate { get; }

    public static IReadOnlyList<MediaFileRowViewModel> CreateRows(MediaFileInfo model)
    {
        if (ShouldAggregateMenuPgs(model))
        {
            return [new MediaFileRowViewModel(model, 0, isMenuPgsAggregate: true, sharedProjection: null)];
        }

        var rowCount = Math.Max(1, Math.Max(model.AudioInfos.Count, model.SubInfos.Count));
        var first = new MediaFileRowViewModel(model);
        if (rowCount == 1)
        {
            return [first];
        }

        var rows = new List<MediaFileRowViewModel>(rowCount) { first };
        for (var index = 1; index < rowCount; index++)
        {
            rows.Add(new MediaFileRowViewModel(model, index, isMenuPgsAggregate: false, first));
        }

        return rows;
    }

    public IReadOnlyList<IssueDisplayItem> IssueItems { get; }
    public IReadOnlyList<string> TooltipLines { get; }
    public string? TooltipText { get; }
    public bool HasTooltip { get; }

    // Identity fields are deliberately populated on continuation rows.
    public string Filename => Model.GeneralInfo.Filename;
    public string FullPath => Model.GeneralInfo.FullPath;
    public string Container => Model.GeneralInfo.Format;
    public string Summary => Model.Summary;
    public IReadOnlyList<ValidationFinding> Findings => Model.Findings;

    // File-level display fields are blank on continuation rows.
    public string DisplayFilename => IsContinuation ? string.Empty : Filename;
    public string DisplayContainer => IsContinuation ? string.Empty : Container;
    public string DisplayFullPath => IsContinuation ? string.Empty : FullPath;

    public string VideoFormat => DisplayVideo(v => v.Format);

    public string Resolution => DisplayVideo(v => $"{v.Width}x{v.Height}");

    public string VideoBitDepth => DisplayVideo(v => v.BitDepth.ToString());

    public string FpsText { get; }
    public string ColorSpace => DisplayVideo(v => v.ColorSpace);
    public string VideoLanguage => DisplayVideo(v => v.Language);
    public string VideoDefault => DisplayVideo(v => v.Default);

    public string AudioTrackLabel => IsMenuPgsAggregate
        ? AggregateLabel(Model.AudioInfos.Count)
        : AudioAt(TrackIndex) is null ? string.Empty : $"#{TrackIndex + 1}";

    public string AudioFormat => IsMenuPgsAggregate
        ? AggregateText(Model.AudioInfos, a => a.Format)
        : AudioAt(TrackIndex)?.Format ?? string.Empty;

    public string AudioBitDepth => IsMenuPgsAggregate
        ? AggregateValue(Model.AudioInfos, a => a.BitDepth)
        : AudioAt(TrackIndex)?.BitDepth.ToString() ?? string.Empty;

    public string AudioBitrate => IsMenuPgsAggregate
        ? AggregateValue(Model.AudioInfos, a => a.Bitrate)
        : AudioAt(TrackIndex)?.Bitrate.ToString() ?? string.Empty;

    public string AudioLanguage => IsMenuPgsAggregate
        ? AggregateText(Model.AudioInfos, a => a.Language)
        : AudioAt(TrackIndex)?.Language ?? string.Empty;

    public string AudioDefault => IsMenuPgsAggregate
        ? AggregateText(Model.AudioInfos, a => a.Default)
        : AudioAt(TrackIndex)?.Default ?? string.Empty;

    public string SubtitleTrackLabel => IsMenuPgsAggregate
        ? AggregateLabel(Model.SubInfos.Count)
        : SubAt(TrackIndex) is null ? string.Empty : $"#{TrackIndex + 1}";

    public string SubtitleFormat => IsMenuPgsAggregate
        ? AggregateText(Model.SubInfos, s => s.Format)
        : SubAt(TrackIndex)?.Format ?? string.Empty;

    public string SubtitleLanguage => IsMenuPgsAggregate
        ? AggregateText(Model.SubInfos, s => s.Language)
        : SubAt(TrackIndex)?.Language ?? string.Empty;

    public string SubtitleDefault => IsMenuPgsAggregate
        ? AggregateText(Model.SubInfos, s => s.Default)
        : SubAt(TrackIndex)?.Default ?? string.Empty;

    public string ChapterState => IsContinuation
        ? string.Empty
        : Model.GeneralInfo.ChapterCount != 0 ? "有" : string.Empty;

    public string ChapterLanguage { get; }

    public ColorToken RowBackgroundToken { get; }
    public ColorToken RowForegroundToken { get; }
    public ColorToken FpsColorToken { get; }
    public ColorToken ColorSpaceColorToken { get; }
    public ColorToken ChapterLanguageBgToken { get; }

    public IBrush RowBackgroundBrush { get; }
    public IBrush RowForegroundBrush { get; }
    public IBrush FpsForegroundBrush { get; }
    public IBrush ColorSpaceForegroundBrush { get; }
    public IBrush ChapterLanguageBackgroundBrush { get; }
    public IBrush ChapterLanguageForegroundBrush { get; }

    private static bool ShouldAggregateMenuPgs(MediaFileInfo model) =>
        MenuTagPattern.IsMatch(model.GeneralInfo.Filename)
        && model.SubInfos.Count(IsPgs) >= 2;

    private static bool IsPgs(SubInfo subtitle) =>
        string.Equals(subtitle.Format?.Trim(), "PGS", StringComparison.OrdinalIgnoreCase)
        || string.Equals(subtitle.Format?.Trim(), "HDMV PGS", StringComparison.OrdinalIgnoreCase);

    private static string AggregateLabel(int count) => count switch
    {
        <= 0 => string.Empty,
        1 => "#1",
        _ => $"#1-#{count}",
    };

    private static string AggregateText<T>(IReadOnlyList<T> tracks, Func<T, string> selector)
    {
        if (tracks.Count == 0)
        {
            return string.Empty;
        }

        var first = selector(tracks[0]) ?? string.Empty;
        return tracks.All(track => string.Equals(
                selector(track) ?? string.Empty,
                first,
                StringComparison.OrdinalIgnoreCase))
            ? first
            : "多种";
    }

    private static string AggregateValue<T>(IReadOnlyList<T> tracks, Func<T, long> selector)
    {
        if (tracks.Count == 0)
        {
            return string.Empty;
        }

        var first = selector(tracks[0]);
        return tracks.All(track => selector(track) == first) ? first.ToString() : "多种";
    }

    private string DisplayVideo(Func<VideoInfo, string> selector)
    {
        var video = IsContinuation ? null : FirstVideo();
        return video is null ? string.Empty : selector(video);
    }

    private VideoInfo? FirstVideo() => Model.VideoInfos.Count > 0 ? Model.VideoInfos[0] : null;

    private AudioInfo? AudioAt(int index) =>
        index >= 0 && index < Model.AudioInfos.Count ? Model.AudioInfos[index] : null;

    private SubInfo? SubAt(int index) =>
        index >= 0 && index < Model.SubInfos.Count ? Model.SubInfos[index] : null;
}
