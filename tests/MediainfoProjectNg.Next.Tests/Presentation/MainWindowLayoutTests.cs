using System.Xml.Linq;

namespace MediainfoProjectNg.Next.Tests.Presentation;

public class MainWindowLayoutTests
{
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

    private static XElement LoadFileGrid()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src", "MediainfoProjectNg.Next", "Views", "MainWindow.axaml");
        var document = XDocument.Load(path);

        return document
            .Descendants()
            .Single(e => e.Name.LocalName == "DataGrid"
                         && e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "FileGrid"));
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
