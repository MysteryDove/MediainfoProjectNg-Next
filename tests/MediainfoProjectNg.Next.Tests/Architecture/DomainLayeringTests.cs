using System.Xml.Linq;

namespace MediainfoProjectNg.Next.Tests.Architecture;

/// <summary>
/// Architecture guards (AC15): Domain stays UI-framework free.
/// Parses the Domain csproj XML — no runtime dependency on Avalonia assemblies.
/// </summary>
public class DomainLayeringTests
{
    private static readonly string[] ForbiddenPackagePrefixes =
    [
        "Avalonia",
        "AvaloniaUI",
        "Microsoft.WindowsDesktop",
        "System.Windows",
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase",
    ];

    [Fact]
    public void Domain_Csproj_DoesNotReference_Avalonia_Or_Wpf_Packages()
    {
        var csprojPath = FindDomainCsproj();
        Assert.True(File.Exists(csprojPath), $"Domain csproj not found at '{csprojPath}'.");

        var doc = XDocument.Load(csprojPath);
        var packageIncludes = doc
            .Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Select(e => (string?)e.Attribute("Include") ?? e.Element(e.Name.Namespace + "Include")?.Value ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var violations = packageIncludes
            .Where(IsForbiddenUiPackage)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Domain project must not reference Avalonia/WPF UI packages. Found: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void Domain_Csproj_DoesNotProjectReference_Desktop_Host()
    {
        var csprojPath = FindDomainCsproj();
        var doc = XDocument.Load(csprojPath);
        var projectRefs = doc
            .Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        Assert.DoesNotContain(
            projectRefs,
            r => r.Contains("MediainfoProjectNg.Next.csproj", StringComparison.OrdinalIgnoreCase)
                 && !r.Contains("MediainfoProjectNg.Next.Domain", StringComparison.OrdinalIgnoreCase)
                 && !r.Contains("MediainfoProjectNg.Next.Core", StringComparison.OrdinalIgnoreCase)
                 && !r.Contains("MediainfoProjectNg.Next.MediaInfo", StringComparison.OrdinalIgnoreCase)
                 && !r.Contains("MediainfoProjectNg.Next.Tests", StringComparison.OrdinalIgnoreCase));

        // Explicit: Domain must not reference the Avalonia desktop project path.
        Assert.DoesNotContain(
            projectRefs,
            r => r.Replace('\\', '/').Contains("/MediainfoProjectNg.Next/MediainfoProjectNg.Next.csproj",
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsForbiddenUiPackage(string packageId) =>
        ForbiddenPackagePrefixes.Any(prefix =>
            packageId.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || packageId.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));

    private static string FindDomainCsproj()
    {
        var root = FindRepoRoot();
        return Path.Combine(
            root,
            "src",
            "MediainfoProjectNg.Next.Domain",
            "MediainfoProjectNg.Next.Domain.csproj");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hasMarker =
                File.Exists(Path.Combine(dir.FullName, "SPEC.md"))
                || File.Exists(Path.Combine(dir.FullName, "MediainfoProjectNg.Next.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "MediainfoProjectNg.Next.sln"));
            if (hasMarker)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate repository root from AppContext.BaseDirectory=" + AppContext.BaseDirectory);
    }
}
