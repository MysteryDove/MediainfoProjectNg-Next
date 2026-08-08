using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using MediainfoProjectNg.Next.Core.Presentation;

namespace MediainfoProjectNg.Next.Converters;

/// <summary>Maps semantic validation tokens to theme-aware accent/background/foreground brushes.</summary>
public sealed class ColorTokenToBrushConverter : IValueConverter
{
    private enum BrushRole
    {
        Accent,
        Background,
        Foreground,
    }

    public static readonly ColorTokenToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ColorToken token ? TokenToAccentBrush(token) : Brushes.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    /// <summary>Compatibility/default role used by filter swatches and other emphasis marks.</summary>
    public static IBrush TokenToBrush(ColorToken token) => TokenToAccentBrush(token);

    public static IBrush TokenToAccentBrush(ColorToken token) => Resolve(token, BrushRole.Accent);

    public static IBrush TokenToBackgroundBrush(ColorToken token) =>
        token == ColorToken.None ? Brushes.Transparent : Resolve(token, BrushRole.Background);

    public static IBrush TokenToForegroundBrush(ColorToken token) =>
        token == ColorToken.None ? DefaultForegroundBrush() : Resolve(token, BrushRole.Foreground);

    public static IBrush TokenToRowBackgroundBrush(ColorToken token) => TokenToBackgroundBrush(token);

    public static IBrush TokenToRowForegroundBrush(ColorToken token) =>
        token == ColorToken.ForegroundMultiSub ? TokenToAccentBrush(token) : DefaultForegroundBrush();

    public static IBrush DefaultForegroundBrush()
    {
        if (Application.Current?.TryGetResource(
                "Val.ForegroundDefault",
                Application.Current.ActualThemeVariant,
                out var resource) == true
            && resource is IBrush brush)
        {
            return brush;
        }

        return Application.Current?.ActualThemeVariant == ThemeVariant.Dark
            ? Brushes.White
            : Brushes.Black;
    }

    private static IBrush Resolve(ColorToken token, BrushRole role)
    {
        var key = token == ColorToken.None ? null : $"Val.{token}.{role}";
        if (key is not null
            && Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var resource) == true
            && resource is IBrush brush)
        {
            return brush;
        }

        var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        return new SolidColorBrush(Color.Parse(FallbackHex(token, role, isDark)));
    }

    private static string FallbackHex(ColorToken token, BrushRole role, bool isDark)
    {
        if (token == ColorToken.None)
        {
            return role == BrushRole.Foreground
                ? isDark ? "#F2F2F2" : "#000000"
                : "#00000000";
        }

        return (token, role, isDark) switch
        {
            (ColorToken.ErrorRed, BrushRole.Accent, false) => "#B91C1C",
            (ColorToken.ErrorRed, BrushRole.Background, false) => "#FEE2E2",
            (ColorToken.ErrorRed, BrushRole.Foreground, false) => "#7F1D1D",
            (ColorToken.ErrorRed, BrushRole.Accent, true) => "#FCA5A5",
            (ColorToken.ErrorRed, BrushRole.Background, true) => "#450A0A",
            (ColorToken.ErrorRed, BrushRole.Foreground, true) => "#FEE2E2",

            (ColorToken.ErrorViolet or ColorToken.FpsVfr, BrushRole.Accent, false) => "#7E22CE",
            (ColorToken.ErrorViolet or ColorToken.FpsVfr, BrushRole.Background, false) => "#F3E8FF",
            (ColorToken.ErrorViolet or ColorToken.FpsVfr, BrushRole.Foreground, false) => "#581C87",
            (ColorToken.ErrorViolet or ColorToken.FpsVfr, BrushRole.Accent, true) => "#D8B4FE",
            (ColorToken.ErrorViolet or ColorToken.FpsVfr, BrushRole.Background, true) => "#3B1D4A",
            (ColorToken.ErrorViolet or ColorToken.FpsVfr, BrushRole.Foreground, true) => "#F3E8FF",

            (ColorToken.WarningYellow, BrushRole.Accent, false) => "#A16207",
            (ColorToken.WarningYellow, BrushRole.Background, false) => "#FEF9C3",
            (ColorToken.WarningYellow, BrushRole.Foreground, false) => "#713F12",
            (ColorToken.WarningYellow, BrushRole.Accent, true) => "#FDE047",
            (ColorToken.WarningYellow, BrushRole.Background, true) => "#422006",
            (ColorToken.WarningYellow, BrushRole.Foreground, true) => "#FEF9C3",

            (ColorToken.WarningPaleVioletRed, BrushRole.Accent, false) => "#BE185D",
            (ColorToken.WarningPaleVioletRed, BrushRole.Background, false) => "#FCE7F3",
            (ColorToken.WarningPaleVioletRed, BrushRole.Foreground, false) => "#831843",
            (ColorToken.WarningPaleVioletRed, BrushRole.Accent, true) => "#F9A8D4",
            (ColorToken.WarningPaleVioletRed, BrushRole.Background, true) => "#500724",
            (ColorToken.WarningPaleVioletRed, BrushRole.Foreground, true) => "#FCE7F3",

            (ColorToken.WarningDelayTeal, BrushRole.Accent, false) => "#0F766E",
            (ColorToken.WarningDelayTeal, BrushRole.Background, false) => "#CCFBF1",
            (ColorToken.WarningDelayTeal, BrushRole.Foreground, false) => "#134E4A",
            (ColorToken.WarningDelayTeal, BrushRole.Accent, true) => "#5EEAD4",
            (ColorToken.WarningDelayTeal, BrushRole.Background, true) => "#103B38",
            (ColorToken.WarningDelayTeal, BrushRole.Foreground, true) => "#CCFBF1",

            (ColorToken.InfoGreenYellow, BrushRole.Accent, false) => "#15803D",
            (ColorToken.InfoGreenYellow, BrushRole.Background, false) => "#DCFCE7",
            (ColorToken.InfoGreenYellow, BrushRole.Foreground, false) => "#14532D",
            (ColorToken.InfoGreenYellow, BrushRole.Accent, true) => "#86EFAC",
            (ColorToken.InfoGreenYellow, BrushRole.Background, true) => "#052E16",
            (ColorToken.InfoGreenYellow, BrushRole.Foreground, true) => "#DCFCE7",

            (ColorToken.ForegroundMultiSub, BrushRole.Accent or BrushRole.Foreground, false) => "#115E59",
            (ColorToken.ForegroundMultiSub, BrushRole.Accent or BrushRole.Foreground, true) => "#5EEAD4",
            (ColorToken.ForegroundMultiSub, BrushRole.Background, _) => "#00000000",

            (ColorToken.FpsNtsc, BrushRole.Accent, false) => "#1D4ED8",
            (ColorToken.FpsNtsc, BrushRole.Background, false) => "#DBEAFE",
            (ColorToken.FpsNtsc, BrushRole.Foreground, false) => "#1E3A8A",
            (ColorToken.FpsNtsc, BrushRole.Accent, true) => "#93C5FD",
            (ColorToken.FpsNtsc, BrushRole.Background, true) => "#172554",
            (ColorToken.FpsNtsc, BrushRole.Foreground, true) => "#DBEAFE",

            (ColorToken.FpsRounded, BrushRole.Accent, false) => "#4338CA",
            (ColorToken.FpsRounded, BrushRole.Background, false) => "#E0E7FF",
            (ColorToken.FpsRounded, BrushRole.Foreground, false) => "#312E81",
            (ColorToken.FpsRounded, BrushRole.Accent, true) => "#A5B4FC",
            (ColorToken.FpsRounded, BrushRole.Background, true) => "#1E1B4B",
            (ColorToken.FpsRounded, BrushRole.Foreground, true) => "#E0E7FF",

            (ColorToken.FpsOther, BrushRole.Accent, false) => "#991B1B",
            (ColorToken.FpsOther, BrushRole.Background, false) => "#FEE2E2",
            (ColorToken.FpsOther, BrushRole.Foreground, false) => "#7F1D1D",
            (ColorToken.FpsOther, BrushRole.Accent, true) => "#FCA5A5",
            (ColorToken.FpsOther, BrushRole.Background, true) => "#450A0A",
            (ColorToken.FpsOther, BrushRole.Foreground, true) => "#FEE2E2",

            (ColorToken.ColorSpaceNon420, BrushRole.Accent, false) => "#C2410C",
            (ColorToken.ColorSpaceNon420, BrushRole.Background, false) => "#FFEDD5",
            (ColorToken.ColorSpaceNon420, BrushRole.Foreground, false) => "#7C2D12",
            (ColorToken.ColorSpaceNon420, BrushRole.Accent, true) => "#FDBA74",
            (ColorToken.ColorSpaceNon420, BrushRole.Background, true) => "#431407",
            (ColorToken.ColorSpaceNon420, BrushRole.Foreground, true) => "#FFEDD5",
            _ => isDark ? "#F2F2F2" : "#000000",
        };
    }
}
