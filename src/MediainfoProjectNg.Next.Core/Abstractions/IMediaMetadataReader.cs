using MediainfoProjectNg.Next.Domain.Models;

namespace MediainfoProjectNg.Next.Core.Abstractions;

/// <summary>
/// MediaInfo adapter boundary. Core depends on this; MediaInfo implements it.
/// </summary>
public interface IMediaMetadataReader
{
    /// <summary>Open path and project into domain model (without validation findings).</summary>
    MediaFileInfo Read(string path);

    /// <summary>MediaInfo library version string for title/status, or null if unavailable.</summary>
    string? GetLibraryVersion();
}
