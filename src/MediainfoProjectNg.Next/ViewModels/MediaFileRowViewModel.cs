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
    public MediaFileRowViewModel(MediaFileInfo model)
    {
        Model = model;

        var video = FirstVideo();
        FpsText = LegacyColorRules.FpsDisplayText(video);
        FpsColorToken = LegacyColorRules.FpsColorToken(video);
        ColorSpaceColorToken = LegacyColorRules.ColorSpaceColorToken(video);
        ChapterLanguage = LegacyColorRules.ChapterLanguageDisplay(model.ChapterInfos);
        ChapterLanguageBgToken = LegacyColorRules.ChapterLanguageBackgroundToken(model.ChapterInfos);
        // Row background continues to use first ordered visible ValidationFinding.
        RowBackgroundToken = LegacyColorRules.FirstFindingBackgroundToken(model.Findings);
        RowForegroundToken = LegacyColorRules.RowForegroundToken(model.GeneralInfo.TextCount);

        IssueItems = IssueDisplayProjector.Project(model);
        TooltipLines = IssueDisplayProjector.BuildTooltipLines(IssueItems);
        TooltipText = TooltipLines.Count == 0 ? null : string.Join('\n', TooltipLines);
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

    public string Audio1Format => AudioAt(0)?.Format ?? string.Empty;

    public string Audio1BitDepth
    {
        get
        {
            var a = AudioAt(0);
            return a is null ? string.Empty : a.BitDepth.ToString();
        }
    }

    public string Audio1Bitrate
    {
        get
        {
            var a = AudioAt(0);
            return a is null ? string.Empty : a.Bitrate.ToString();
        }
    }

    public string Audio1Language => AudioAt(0)?.Language ?? string.Empty;
    public string Audio1Default => AudioAt(0)?.Default ?? string.Empty;

    public string Audio2Format => AudioAt(1)?.Format ?? string.Empty;

    public string Audio2BitDepth
    {
        get
        {
            var a = AudioAt(1);
            return a is null ? string.Empty : a.BitDepth.ToString();
        }
    }

    public string Audio2Bitrate
    {
        get
        {
            var a = AudioAt(1);
            return a is null ? string.Empty : a.Bitrate.ToString();
        }
    }

    public string Audio2Language => AudioAt(1)?.Language ?? string.Empty;
    public string Audio2Default => AudioAt(1)?.Default ?? string.Empty;

    public string Sub1Format => SubAt(0)?.Format ?? string.Empty;
    public string Sub1Language => SubAt(0)?.Language ?? string.Empty;
    public string Sub1Default => SubAt(0)?.Default ?? string.Empty;

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
