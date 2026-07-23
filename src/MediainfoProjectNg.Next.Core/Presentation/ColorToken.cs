namespace MediainfoProjectNg.Next.Core.Presentation;

/// <summary>
/// Semantic presentation tokens mapped from legacy WPF brushes (mpng converters / ErrorInfo.Brush).
/// UI maps these to theme-aware brushes; Domain stays brush-free.
/// </summary>
public enum ColorToken
{
    None = 0,

    /// <summary>Legacy Brushes.Red — extension/container mismatch.</summary>
    ErrorRed,

    /// <summary>Legacy Brushes.Violet — filename/content mismatch.</summary>
    ErrorViolet,

    /// <summary>Legacy Brushes.Yellow — chapter warnings; chapter language issue cell.</summary>
    WarningYellow,

    /// <summary>Legacy Brushes.PaleVioletRed — track duration delta.</summary>
    WarningPaleVioletRed,

    /// <summary>Legacy RGB(0,164,172) — non-zero delay.</summary>
    WarningDelayTeal,

    /// <summary>Legacy Brushes.GreenYellow — multi-audio info.</summary>
    InfoGreenYellow,

    /// <summary>Legacy Brushes.Blue — TextCount &gt; 1 row foreground.</summary>
    ForegroundMultiSub,

    /// <summary>Legacy Brushes.DarkViolet — VFR.</summary>
    FpsVfr,

    /// <summary>Legacy Brushes.Olive — 29.97/59.94 (x/1001 family).</summary>
    FpsNtsc,

    /// <summary>Legacy Brushes.SlateBlue — 23.976/29.970 (x/1000 family).</summary>
    FpsRounded,

    /// <summary>Legacy Brushes.Maroon — other frame rates.</summary>
    FpsOther,

    /// <summary>Legacy Brushes.Orange — color space not YUV420.</summary>
    ColorSpaceNon420,
}
