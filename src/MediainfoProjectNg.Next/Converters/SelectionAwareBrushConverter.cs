using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MediainfoProjectNg.Next.Converters;

/// <summary>
/// Ports OG DataGrid cell Style.Triggers: special cell colors only when <c>IsSelected == False</c>.
/// values[0] = normal brush (IBrush), values[1] = row IsSelected (bool).
/// ConverterParameter: "Foreground" (default) → white highlight text when selected;
/// "Background" → transparent when selected.
/// </summary>
public sealed class SelectionAwareBrushConverter : IMultiValueConverter
{
    public static readonly SelectionAwareBrushConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isSelected = values.Count > 1 && values[1] is true;
        var mode = parameter as string ?? "Foreground";

        if (isSelected)
        {
            return string.Equals(mode, "Background", StringComparison.OrdinalIgnoreCase)
                ? Brushes.Transparent
                : Brushes.White;
        }

        if (values.Count > 0 && values[0] is IBrush brush)
        {
            return brush;
        }

        return string.Equals(mode, "Background", StringComparison.OrdinalIgnoreCase)
            ? Brushes.Transparent
            : Brushes.Black;
    }
}
