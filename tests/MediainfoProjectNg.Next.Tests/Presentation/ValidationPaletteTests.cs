using System.Globalization;
using System.Xml.Linq;

namespace MediainfoProjectNg.Next.Tests.Presentation;

public class ValidationPaletteTests
{
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void SemanticBackgroundForegroundPairs_MeetWcagContrast(string theme)
    {
        var resources = LoadThemeResources("ValidationBrushes.axaml", theme);

        foreach (var (key, background) in resources.Where(pair => pair.Key.EndsWith(".Background", StringComparison.Ordinal)))
        {
            if (string.Equals(background, "Transparent", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var foregroundKey = key[..^"Background".Length] + "Foreground";
            Assert.True(resources.TryGetValue(foregroundKey, out var foreground),
                $"Missing paired resource {foregroundKey}.");
            AssertContrastAtLeast(background, foreground!, 4.5, $"{theme} {key}");
        }
    }

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void AccentAndSelectionText_MeetWcagContrast(string theme)
    {
        var validation = LoadThemeResources("ValidationBrushes.axaml", theme);
        var chrome = LoadThemeResources("WpfClassicLight.axaml", theme);
        var neutralBackground = chrome["Wpf.DataGridRowDefaultBrush"];

        foreach (var (key, accent) in validation.Where(pair => pair.Key.EndsWith(".Accent", StringComparison.Ordinal)))
        {
            AssertContrastAtLeast(neutralBackground, accent, 4.5, $"{theme} {key}");
        }

        AssertContrastAtLeast(
            chrome["Wpf.HighlightBrush"],
            chrome["Wpf.HighlightTextBrush"],
            4.5,
            $"{theme} selection");
    }

    [Fact]
    public void CategoryPalette_UsesSpecifiedPairs_AndFrameRateDiffersFromChapter()
    {
        var light = LoadThemeResources("ValidationBrushes.axaml", "Light");
        var dark = LoadThemeResources("ValidationBrushes.axaml", "Dark");

        Assert.Equal("#F3E8FF", light["Val.ErrorViolet.Background"]);
        Assert.Equal("#581C87", light["Val.ErrorViolet.Foreground"]);
        Assert.Equal("#CCFBF1", light["Val.WarningDelayTeal.Background"]);
        Assert.Equal("#134E4A", light["Val.WarningDelayTeal.Foreground"]);
        Assert.Equal("#DBEAFE", light["Val.FpsNtsc.Background"]);
        Assert.Equal("#1E3A8A", light["Val.FpsNtsc.Foreground"]);
        Assert.Equal("#FFEDD5", light["Val.ColorSpaceNon420.Background"]);
        Assert.Equal("#7C2D12", light["Val.ColorSpaceNon420.Foreground"]);
        Assert.Equal("#FEF9C3", light["Val.WarningYellow.Background"]);
        Assert.Equal("#713F12", light["Val.WarningYellow.Foreground"]);

        Assert.Equal("#3B1D4A", dark["Val.ErrorViolet.Background"]);
        Assert.Equal("#103B38", dark["Val.WarningDelayTeal.Background"]);
        Assert.Equal("#172554", dark["Val.FpsNtsc.Background"]);
        Assert.Equal("#431407", dark["Val.ColorSpaceNon420.Background"]);
        Assert.Equal("#422006", dark["Val.WarningYellow.Background"]);

        Assert.NotEqual(light["Val.FpsNtsc.Accent"], light["Val.WarningYellow.Accent"]);
        Assert.NotEqual(dark["Val.FpsNtsc.Accent"], dark["Val.WarningYellow.Accent"]);
    }

    [Fact]
    public void LegacyLightPairs_UseReadablePastelBackgrounds()
    {
        var light = LoadThemeResources("ValidationBrushes.axaml", "Light");

        Assert.Equal(("#FEE2E2", "#7F1D1D"),
            (light["Val.ErrorRed.Background"], light["Val.ErrorRed.Foreground"]));
        Assert.Equal(("#FCE7F3", "#831843"),
            (light["Val.WarningPaleVioletRed.Background"], light["Val.WarningPaleVioletRed.Foreground"]));
        Assert.Equal(("#DCFCE7", "#14532D"),
            (light["Val.InfoGreenYellow.Background"], light["Val.InfoGreenYellow.Foreground"]));
    }

    private static Dictionary<string, string> LoadThemeResources(string filename, string theme)
    {
        var path = Path.Combine(
            FindRepoRoot(), "src", "MediainfoProjectNg.Next", "Themes", filename);
        var doc = XDocument.Load(path);
        var dictionary = doc.Descendants()
            .Single(element => element.Name.LocalName == "ResourceDictionary"
                && element.Attributes().Any(attribute =>
                    attribute.Name.LocalName == "Key" && attribute.Value == theme));

        return dictionary.Elements()
            .Where(element => element.Name.LocalName == "SolidColorBrush")
            .ToDictionary(
                element => element.Attributes().Single(attribute => attribute.Name.LocalName == "Key").Value,
                element => element.Attribute("Color")!.Value,
                StringComparer.Ordinal);
    }

    private static void AssertContrastAtLeast(
        string background,
        string foreground,
        double minimum,
        string context)
    {
        var ratio = ContrastRatio(ParseRgb(background), ParseRgb(foreground));
        Assert.True(ratio >= minimum,
            $"{context}: {background}/{foreground} contrast {ratio.ToString("F2", CultureInfo.InvariantCulture)} is below {minimum}.");
    }

    private static (byte R, byte G, byte B) ParseRgb(string value)
    {
        var hex = value.TrimStart('#');
        if (hex.Length == 8)
        {
            hex = hex[2..];
        }

        return (
            byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static double ContrastRatio((byte R, byte G, byte B) first, (byte R, byte G, byte B) second)
    {
        var light = RelativeLuminance(first);
        var dark = RelativeLuminance(second);
        if (light < dark)
        {
            (light, dark) = (dark, light);
        }

        return (light + 0.05) / (dark + 0.05);
    }

    private static double RelativeLuminance((byte R, byte G, byte B) color) =>
        0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

    private static double Linear(byte channel)
    {
        var value = channel / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MediainfoProjectNg.Next.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
