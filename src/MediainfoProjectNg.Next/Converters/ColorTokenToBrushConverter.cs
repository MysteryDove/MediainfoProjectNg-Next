using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using MediainfoProjectNg.Next.Core.Presentation;

namespace MediainfoProjectNg.Next.Converters;

/// <summary>
/// Maps <see cref="ColorToken"/> to theme resource brushes (Val.* keys).
/// Prefer DynamicResource-resolved brushes over hard-coded RGB.
/// </summary>
public sealed class ColorTokenToBrushConverter : IValueConverter
{
    public static readonly ColorTokenToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ColorToken token)
        {
            return Brushes.Transparent;
        }

        return TokenToBrush(token);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public static IBrush TokenToBrush(ColorToken token)
    {
        var key = token switch
        {
            ColorToken.None => "Val.None",
            ColorToken.ErrorRed => "Val.ErrorRed",
            ColorToken.ErrorViolet => "Val.ErrorViolet",
            ColorToken.WarningYellow => "Val.WarningYellow",
            ColorToken.WarningPaleVioletRed => "Val.WarningPaleVioletRed",
            ColorToken.WarningDelayTeal => "Val.WarningDelayTeal",
            ColorToken.InfoGreenYellow => "Val.InfoGreenYellow",
            ColorToken.ForegroundMultiSub => "Val.ForegroundMultiSub",
            ColorToken.FpsVfr => "Val.FpsVfr",
            ColorToken.FpsNtsc => "Val.FpsNtsc",
            ColorToken.FpsRounded => "Val.FpsRounded",
            ColorToken.FpsOther => "Val.FpsOther",
            ColorToken.ColorSpaceNon420 => "Val.ColorSpaceNon420",
            _ => null,
        };

        if (key is not null
            && Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var resource) == true
            && resource is IBrush brush)
        {
            return brush;
        }

        // Fallback RGB only when resources unavailable (design-time / early init).
        return token switch
        {
            ColorToken.None => Brushes.Transparent,
            ColorToken.ErrorRed => Solid(255, 0, 0),
            ColorToken.ErrorViolet => Solid(238, 130, 238),
            ColorToken.WarningYellow => Solid(255, 255, 0),
            ColorToken.WarningPaleVioletRed => Solid(219, 112, 147),
            ColorToken.WarningDelayTeal => Solid(0, 164, 172),
            ColorToken.InfoGreenYellow => Solid(173, 255, 47),
            ColorToken.ForegroundMultiSub => Solid(0, 0, 255),
            ColorToken.FpsVfr => Solid(148, 0, 211),
            ColorToken.FpsNtsc => Solid(128, 128, 0),
            ColorToken.FpsRounded => Solid(106, 90, 205),
            ColorToken.FpsOther => Solid(128, 0, 0),
            ColorToken.ColorSpaceNon420 => Solid(255, 165, 0),
            _ => Brushes.Transparent,
        };
    }

    /// <summary>Legacy row background: no finding → transparent.</summary>
    public static IBrush TokenToRowBackgroundBrush(ColorToken token) =>
        token == ColorToken.None ? Brushes.Transparent : TokenToBrush(token);

    /// <summary>Legacy row foreground: multi-sub → blue token, else default.</summary>
    public static IBrush TokenToRowForegroundBrush(ColorToken token) =>
        token == ColorToken.ForegroundMultiSub ? TokenToBrush(token) : DefaultForegroundBrush();

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

    private static IBrush Solid(byte r, byte g, byte b) =>
        new SolidColorBrush(Color.FromRgb(r, g, b));
}
