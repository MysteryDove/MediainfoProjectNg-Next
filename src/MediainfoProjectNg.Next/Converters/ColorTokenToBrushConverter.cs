using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MediainfoProjectNg.Next.Core.Presentation;

namespace MediainfoProjectNg.Next.Converters;

/// <summary>
/// Maps <see cref="ColorToken"/> to theme resource brushes (Val.* keys).
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

    public static IBrush TokenToBrush(ColorToken token) =>
        token switch
        {
            ColorToken.None => Brushes.Transparent,
            ColorToken.ErrorRed => Solid(255, 0, 0),
            ColorToken.ErrorViolet => Solid(238, 130, 238), // Violet
            ColorToken.WarningYellow => Solid(255, 255, 0),
            ColorToken.WarningPaleVioletRed => Solid(219, 112, 147),
            ColorToken.WarningDelayTeal => Solid(0, 164, 172),
            ColorToken.InfoGreenYellow => Solid(173, 255, 47),
            ColorToken.ForegroundMultiSub => Solid(0, 0, 255),
            ColorToken.FpsVfr => Solid(148, 0, 211), // DarkViolet
            ColorToken.FpsNtsc => Solid(128, 128, 0), // Olive
            ColorToken.FpsRounded => Solid(106, 90, 205), // SlateBlue
            ColorToken.FpsOther => Solid(128, 0, 0), // Maroon
            ColorToken.ColorSpaceNon420 => Solid(255, 165, 0), // Orange
            _ => Brushes.Transparent
        };

    /// <summary>Legacy row background: no finding → White.</summary>
    public static IBrush TokenToRowBackgroundBrush(ColorToken token) =>
        token == ColorToken.None ? Brushes.White : TokenToBrush(token);

    /// <summary>Legacy row foreground: multi-sub → Blue, else Black.</summary>
    public static IBrush TokenToRowForegroundBrush(ColorToken token) =>
        token == ColorToken.ForegroundMultiSub ? Brushes.Blue : Brushes.Black;

    private static IBrush Solid(byte r, byte g, byte b) =>
        new SolidColorBrush(Color.FromRgb(r, g, b));
}
