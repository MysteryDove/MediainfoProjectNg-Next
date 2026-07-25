using MediainfoProjectNg.Next.Core.Presentation;

namespace MediainfoProjectNg.Next.Tests.Presentation;

public class AppVersionInfoTests
{
    [Fact]
    public void FormatWindowTitle_WithMediaInfo_UsesMiddleDotLayout()
    {
        var title = AppVersionInfo.FormatWindowTitle("1.0.0", "MediaInfoLib - v24.06");
        Assert.Equal("mediainfo project ng next 1.0.0  ·  MediaInfo 24.06", title);
    }

    [Fact]
    public void FormatWindowTitle_WithoutMediaInfo_ShowsUnavailable()
    {
        var title = AppVersionInfo.FormatWindowTitle("1.0.0", null);
        Assert.Equal("mediainfo project ng next 1.0.0  ·  MediaInfo Unavailable", title);
    }

    [Fact]
    public void FormatWindowTitle_RawLibraryString_IsNotDoublePrefixed()
    {
        var title = AppVersionInfo.FormatWindowTitle("1.2.3", "25.01");
        Assert.Equal("mediainfo project ng next 1.2.3  ·  MediaInfo 25.01", title);
    }

    [Fact]
    public void GetProductVersion_ReturnsNonEmpty()
    {
        var version = AppVersionInfo.GetProductVersion();
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain('+', version);
    }
}
