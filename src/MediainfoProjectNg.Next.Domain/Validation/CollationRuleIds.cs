namespace MediainfoProjectNg.Next.Domain.Validation;

public static class CollationRuleIds
{
    public const string FnResolution = "FN.Resolution";
    public const string FnProfile = "FN.Profile";
    public const string FnVideoEncoder = "FN.VideoEncoder";
    public const string FnAudioEncoders = "FN.AudioEncoders";

    public const string TrackVideoPresent = "TRACK.VideoPresent";
    public const string TrackAudioPresent = "TRACK.AudioPresent";
    public const string TrackVideoLanguage = "TRACK.VideoLanguage";
    public const string TrackVideoDefault = "TRACK.VideoDefault";
    public const string TrackAudioLanguage = "TRACK.AudioLanguage";
    public const string TrackAudioDefaultCardinality = "TRACK.AudioDefaultCardinality";
    public const string TrackPgsLanguage = "TRACK.PgsLanguage";
    public const string TrackPgsDefault = "TRACK.PgsDefault";

    public const string VideoScanType = "VIDEO.ScanType";
    public const string VideoColorRange = "VIDEO.ColorRange";
    public const string VideoColorMatrix = "VIDEO.ColorMatrix";
    public const string VideoColorReview = "VIDEO.ColorReview";

    public const string ChapterLanguageMissing = "CH.LanguageMissing";
    public const string ChapterLanguageMixed = "CH.LanguageMixed";

    public const string MkaAudioOnlyDefaults = "MKA.AudioOnlyDefaults";
    public const string Mp4MobileTrackLayout = "MP4.MobileTrackLayout";

    /// <summary>Filename field rules that supersede the generic legacy mismatch, in matrix order.</summary>
    public static readonly IReadOnlyList<string> FilenameRuleOrder =
    [
        FnResolution,
        FnProfile,
        FnVideoEncoder,
        FnAudioEncoders,
    ];

    /// <summary>All enabled Collation rule IDs in Phase 0 matrix order.</summary>
    public static readonly IReadOnlyList<string> EnabledOrder =
    [
        FnResolution,
        FnProfile,
        FnVideoEncoder,
        FnAudioEncoders,
        TrackVideoPresent,
        TrackAudioPresent,
        TrackVideoLanguage,
        TrackVideoDefault,
        TrackAudioLanguage,
        TrackAudioDefaultCardinality,
        TrackPgsLanguage,
        TrackPgsDefault,
        VideoScanType,
        VideoColorRange,
        VideoColorMatrix,
        VideoColorReview,
        ChapterLanguageMissing,
        ChapterLanguageMixed,
    ];
}
