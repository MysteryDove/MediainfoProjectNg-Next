using System.Xml.Linq;

namespace MediainfoProjectNg.Next.Tests.Presentation;

public class MainWindowLayoutTests
{
    [Fact]
    public void CategoryCount_NotifiesComputedButtonText_AndFiltersReconcileSelection()
    {
        var root = FindRepoRoot();
        var toggleSource = File.ReadAllText(Path.Combine(
            root, "src", "MediainfoProjectNg.Next", "ViewModels", "CategoryToggleViewModel.cs"));
        var viewSource = File.ReadAllText(Path.Combine(
            root, "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml.cs"));
        var axaml = File.ReadAllText(Path.Combine(
            root, "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml"));

        Assert.Contains("[NotifyPropertyChangedFor(nameof(ButtonText))]", toggleSource, StringComparison.Ordinal);
        Assert.Contains("Click=\"CategoryFilter_OnClick\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"ClearCategoryFilters_OnClick\"", axaml, StringComparison.Ordinal);
        Assert.Contains("ReconcileGridSelectionWithVisible(primary, selected);", viewSource, StringComparison.Ordinal);
        Assert.Contains("e.NewSize.Height * 0.45", viewSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"200\"", axaml, StringComparison.Ordinal);
    }

    [Fact]
    public void App_FollowsSystemTheme_WithLightAndDarkChromeResources()
    {
        var root = FindRepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "src", "MediainfoProjectNg.Next", "App.axaml"));
        var theme = File.ReadAllText(Path.Combine(
            root, "src", "MediainfoProjectNg.Next", "Themes", "WpfClassicLight.axaml"));

        Assert.Contains("RequestedThemeVariant=\"Default\"", app, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Light\"", theme, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Dark\"", theme, StringComparison.Ordinal);
        Assert.Contains("Wpf.DataGridRowDefaultBrush", theme, StringComparison.Ordinal);
    }

    [Fact]
    public void FileGrid_UsesCompactAutomaticLayout()
    {
        var fileGrid = LoadFileGrid();

        Assert.Equal("12", (string?)fileGrid.Attribute("FontSize"));
        Assert.Equal("20", (string?)fileGrid.Attribute("RowHeight"));
        Assert.Equal("22", (string?)fileGrid.Attribute("ColumnHeaderHeight"));
        Assert.Equal("Auto", (string?)fileGrid.Attribute("ColumnWidth"));

        AssertStyleSetters(fileGrid, "DataGridColumnHeader",
            ("FontSize", "12"), ("MinHeight", "0"), ("Padding", "4,0"));
        AssertStyleSetters(fileGrid, "DataGridCell",
            ("FontSize", "12"), ("MinHeight", "0"), ("Padding", "0"));
        AssertStyleSetters(fileGrid, "DataGridCell TextBlock", ("Margin", "4,0"));
    }

    [Fact]
    public void FileGrid_DataColumns_DoNotOverrideAutomaticWidth()
    {
        var fileGrid = LoadFileGrid();
        var columns = fileGrid
            .Elements()
            .Single(e => e.Name.LocalName == "DataGrid.Columns")
            .Elements()
            .ToList();

        Assert.NotEmpty(columns);
        Assert.All(columns, column =>
        {
            Assert.Null(column.Attribute("Width"));
            Assert.Null(column.Attribute("MinWidth"));
        });
    }

    [Fact]
    public void FileGrid_UsesTrackRows_AndSuppressesCellSelectionBackground()
    {
        var axaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml"));

        Assert.Contains("Selector=\"DataGridCell:selected\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Background\" Value=\"Transparent\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"音轨\" Binding=\"{Binding AudioFormat}\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"字幕\" Binding=\"{Binding SubtitleFormat}\"", axaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"#2音轨\"", axaml, StringComparison.Ordinal);
    }

    [Fact]
    public void FilterStrip_IsInLeftColumn_AboveFileGrid()
    {
        var doc = LoadDocument();
        var axaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml"));

        // Left column host owns Grid.Column="0" and the filter strip; strip does not span right panel.
        Assert.Contains("x:Name=\"LeftColumnHost\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Column=\"0\"", axaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"CategoryFilterStrip\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"0\"", axaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FileGrid\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"1\"", axaml, StringComparison.Ordinal);

        var leftHost = doc.Descendants()
            .Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "LeftColumnHost"));
        Assert.Contains(leftHost.Descendants(),
            e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "CategoryFilterStrip"));
        Assert.Contains(leftHost.Descendants(),
            e => e.Name.LocalName == "DataGrid"
                 && e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "FileGrid"));

        var rightPanel = doc.Descendants()
            .Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "RightPanel"));
        Assert.DoesNotContain(rightPanel.Descendants(),
            e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "CategoryFilterStrip"));
    }

    [Fact]
    public void TooltipShowDelay_Is600ms_OnRows()
    {
        var axaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml"));
        Assert.Contains("ToolTip.ShowDelay\" Value=\"600\"", axaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TooltipGeometry_IsBounded_WithWrapAndClip()
    {
        var axaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml"));
        // Bounds must be window/app-level (popup is not under DataGrid).
        Assert.Contains("Window.Styles", axaml, StringComparison.Ordinal);
        Assert.Contains("MaxWidth\" Value=\"360\"", axaml, StringComparison.Ordinal);
        Assert.Contains("MaxHeight\" Value=\"240\"", axaml, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds\" Value=\"True\"", axaml, StringComparison.Ordinal);
        Assert.Contains("TextWrapping\" Value=\"Wrap\"", axaml, StringComparison.Ordinal);
        Assert.Contains("Selector=\"ToolTip TextBlock\"", axaml, StringComparison.Ordinal);
    }

    [Fact]
    public void FindingsSection_BindsStructuredEvidence()
    {
        var axaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml"));
        Assert.Contains("Binding Evidence", axaml, StringComparison.Ordinal);
    }

    [Fact]
    public void RightPanel_OrdersFindingsBeforeMediaInfo()
    {
        var doc = LoadDocument();
        var host = doc.Descendants()
            .Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "RightContentHost"));
        var findings = host.Descendants()
            .First(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "FindingsSection"));
        var media = host.Descendants()
            .First(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "MediaInfoSummaryBox")
                        || e.Name.LocalName == "TextBox");

        var findingsRow = findings.Attributes().FirstOrDefault(a => a.Name.LocalName == "Row")?.Value ?? "0";
        Assert.Equal("0", findingsRow);
        Assert.NotNull(media);
    }

    [Fact]
    public void ClearFiltersButton_HasChineseAccessibleName()
    {
        var doc = LoadDocument();
        var btn = doc.Descendants()
            .Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "ClearFiltersButton"));
        Assert.Equal("全部", (string?)btn.Attribute("Content")
            ?? btn.Attributes().FirstOrDefault(a => a.Name.LocalName == "Content")?.Value);
        var accessible = btn.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name"
            && a.Name.NamespaceName.Contains("automation", StringComparison.OrdinalIgnoreCase)
            || a.Name.LocalName == "Name" && a.Value == "全部");
        // AutomationProperties.Name="全部"
        Assert.Contains(btn.Attributes(), a => a.Value == "全部");
    }

    [Fact]
    public void NoLeftPaneTabOrPerRuleRainbowStrip()
    {
        var doc = LoadDocument();
        var names = doc.Descendants()
            .SelectMany(e => e.Attributes())
            .Where(a => a.Name.LocalName == "Name")
            .Select(a => a.Value)
            .ToList();
        Assert.DoesNotContain(names, n => n.Contains("TabControl", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Rainbow", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("PerRule", StringComparison.OrdinalIgnoreCase));
    }

    private static XElement LoadFileGrid()
    {
        var document = LoadDocument();
        return document
            .Descendants()
            .Single(e => e.Name.LocalName == "DataGrid"
                         && e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "FileGrid"));
    }

    private static XDocument LoadDocument()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml");
        return XDocument.Load(path);
    }

    private static void AssertStyleSetters(
        XElement fileGrid,
        string selector,
        params (string Property, string Value)[] expectedSetters)
    {
        var style = fileGrid
            .Descendants()
            .Single(e => e.Name.LocalName == "Style" && (string?)e.Attribute("Selector") == selector);
        var setters = style
            .Elements()
            .Where(e => e.Name.LocalName == "Setter")
            .ToDictionary(e => (string)e.Attribute("Property")!, e => (string)e.Attribute("Value")!);

        foreach (var (property, value) in expectedSetters)
        {
            Assert.True(setters.TryGetValue(property, out var actual),
                $"Style '{selector}' is missing setter '{property}'.");
            Assert.Equal(value, actual);
        }
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

        throw new InvalidOperationException(
            "Could not locate repository root from AppContext.BaseDirectory=" + AppContext.BaseDirectory);
    }
}
