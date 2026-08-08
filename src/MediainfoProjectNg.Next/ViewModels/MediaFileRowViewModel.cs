using Avalonia.Media;
using MediainfoProjectNg.Next.Converters;
using MediainfoProjectNg.Next.Core.Presentation;
using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.ViewModels;

/// <summary>
/// Safe row projection for the main DataGrid. Never indexes empty track lists.
/// Exposes legacy presentation tokens/brushes for parity with mpng converters.
/// Owns the immutable ordered <see cref="IssueDisplayItem"/> projection.
/// </summary>
public sealed class MediaFileRowViewModel
{
    public MediaFileRowViewModel(MediaFileInfo model, int trackIndex = 0)
        : this(model, trackIndex, sharedProjection: null)
    {
    }

    private MediaFileRowViewModel(
        MediaFileInfo model,
        int trackIndex,
        MediaFileRowViewModel? sharedProjection)
    {
        Model = model;
        TrackIndex = trackIndex;

        var video = FirstVideo();
        FpsText = LegacyColorRules.FpsDisplayText(video);
        FpsColorToken = LegacyColorRules.FpsColorToken(video);
        ColorSpaceColorToken = LegacyColorRules.ColorSpaceColorToken(video);
        ChapterLanguage = LegacyColorRules.ChapterLanguageDisplay(model.ChapterInfos);
        ChapterLanguageBgToken = LegacyColorRules.ChapterLanguageBackgroundToken(model.ChapterInfos);
        // Row background continues to use first ordered visible ValidationFinding.
        RowBackgroundToken = LegacyColorRules.FirstFindingBackgroundToken(model.Findings);
        RowForegroundToken = LegacyColorRules.RowForegroundToken(model.GeneralInfo.TextCount);

        IssueItems = sharedProjection?.IssueItems ?? IssueDisplayProjector.Project(model);
        TooltipLines = sharedProjection?.TooltipLines ?? IssueDisplayProjector.BuildTooltipLines(IssueItems);
        TooltipText = sharedProjection?.TooltipText
            ?? (TooltipLines.Count == 0 ? null : string.Join('\n', TooltipLines));
        HasTooltip = TooltipText is not null;

        // Legacy: no finding → White row; TextCount>1 → Blue fg else Black (including when selected).
        RowBackgroundBrush = ColorTokenToBrushConverter.TokenToRowBackgroundBrush(RowBackgroundToken);
        RowForegroundBrush = RowForegroundToken == ColorToken.ForegroundMultiSub
            ? ColorTokenToBrushConverter.TokenToRowForegroundBrush(RowForegroundToken)
            : RowBackgroundToken == ColorToken.None
                ? ColorTokenToBrushConverter.DefaultForegroundBrush()
                : Brushes.Black;
        FpsForegroundBrush = FpsColorToken == ColorToken.None
            ? ColorTokenToBrushConverter.DefaultForegroundBrush()
            : ColorTokenToBrushConverter.TokenToBrush(FpsColorToken);
        ColorSpaceForegroundBrush = ColorSpaceColorToken == ColorToken.None
            ? ColorTokenToBrushConverter.DefaultForegroundBrush()
            : ColorTokenToBrushConverter.TokenToBrush(ColorSpaceColorToken);
        ChapterLanguageBackgroundBrush = ChapterLanguageBgToken == ColorToken.None
            ? Brushes.Transparent
            : ColorTokenToBrushConverter.TokenToBrush(ChapterLanguageBgToken);
        ChapterLanguageForegroundBrush = ChapterLanguageBgToken == ColorToken.None
            ? ColorTokenToBrushConverter.DefaultForegroundBrush()
            : Brushes.Black;
    }

    public MediaFileInfo Model { get; }
    public int TrackIndex { get; }

    public static IReadOnlyList<MediaFileRowViewModel> CreateRows(MediaFileInfo model)
    {
        var rowCount = Math.Max(1, Math.Max(model.AudioInfos.Count, model.SubInfos.Count));
        var first = new MediaFileRowViewModel(model);
        if (rowCount == 1)
        {
            return [first];
        }

        var rows = new List<MediaFileRowViewModel>(rowCount) { first };
        for (var index = 1; index < rowCount; index++)
        {
            rows.Add(new MediaFileRowViewModel(model, index, first));
        }

        return rows;
    }

    public IReadOnlyList<IssueDisplayItem> IssueItems { get; }
    public IReadOnlyList<string> TooltipLines { get; }
    public string? TooltipText { get; }
    public bool HasTooltip { get; }

    public string Filename => Model.GeneralInfo.Filename;
    public string Container => Model.GeneralInfo.Format;
    public string FullPath => Model.GeneralInfo.FullPath;
    public string Summary => Model.Summary;
    public IReadOnlyList<ValidationFinding> Findings => Model.Findings;

    public string VideoFormat => FirstVideo()?.Format ?? string.Empty;

    public string Resolution
    {
        get
        {
            var v = FirstVideo();
            return v is null ? string.Empty : $"{v.Width}x{v.Height}";
        }
    }

    public string VideoBitDepth
    {
        get
        {
            var v = FirstVideo();
            return v is null ? string.Empty : v.BitDepth.ToString();
        }
    }

    public string FpsText { get; }
    public string ColorSpace => FirstVideo()?.ColorSpace ?? string.Empty;
    public string VideoLanguage => FirstVideo()?.Language ?? string.Empty;
    public string VideoDefault => FirstVideo()?.Default ?? string.Empty;

    public string AudioFormat => AudioAt(TrackIndex)?.Format ?? string.Empty;

    public string AudioBitDepth
    {
        get
        {
            var a = AudioAt(TrackIndex);
            return a is null ? string.Empty : a.BitDepth.ToString();
        }
    }

    public string AudioBitrate
    {
        get
        {
            var a = AudioAt(TrackIndex);
            return a is null ? string.Empty : a.Bitrate.ToString();
        }
    }

    public string AudioLanguage => AudioAt(TrackIndex)?.Language ?? string.Empty;
    public string AudioDefault => AudioAt(TrackIndex)?.Default ?? string.Empty;

    public string SubtitleFormat => SubAt(TrackIndex)?.Format ?? string.Empty;
    public string SubtitleLanguage => SubAt(TrackIndex)?.Language ?? string.Empty;
    public string SubtitleDefault => SubAt(TrackIndex)?.Default ?? string.Empty;

    public string ChapterState => Model.GeneralInfo.ChapterCount != 0 ? "有" : string.Empty;
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

    private VideoInfo? FirstVideo() => Model.VideoInfos.Count > 0 ? Model.VideoInfos[0] : null;

    private AudioInfo? AudioAt(int index) =>
        index >= 0 && index < Model.AudioInfos.Count ? Model.AudioInfos[index] : null;

    private SubInfo? SubAt(int index) =>
        index >= 0 && index < Model.SubInfos.Count ? Model.SubInfos[index] : null;
}
