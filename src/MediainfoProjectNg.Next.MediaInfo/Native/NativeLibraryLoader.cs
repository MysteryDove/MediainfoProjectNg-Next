using System.Runtime.InteropServices;

namespace MediainfoProjectNg.Next.MediaInfo.Native;

/// <summary>
/// Resolves and loads the bundled MediaInfo shared library for the current platform.
/// Full packaging paths filled in Phase 5; V1 spike supports process/app-base probing.
/// </summary>
public static class NativeLibraryLoader
{
    public const string LibraryBaseName = "mediainfo";

    public static string GetLibraryFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Official MediaInfo packages use MediaInfo.dll; CMake target may emit mediainfo.dll.
            return "MediaInfo.dll";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "libmediainfo.dylib";
        }

        return "libmediainfo.so";
    }

    /// <summary>All file names the loader accepts for the current OS (primary first).</summary>
    public static IReadOnlyList<string> GetLibraryFileNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ["MediaInfo.dll", "mediainfo.dll"];
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ["libmediainfo.dylib"];
        }

        return ["libmediainfo.so"];
    }

    public static IEnumerable<string> CandidatePaths(string? appBase = null)
    {
        appBase ??= AppContext.BaseDirectory;
        foreach (var fileName in GetLibraryFileNames())
        {
            yield return Path.Combine(appBase, fileName);
            yield return Path.Combine(appBase, "runtimes", GetRid(), "native", fileName);
            yield return Path.Combine(appBase, "native", fileName);
        }
    }

    public static string GetRid()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"win-{arch}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"osx-{arch}";
        }

        return $"linux-{arch}";
    }

    public static bool TryResolve(out string path, string? appBase = null)
    {
        foreach (var candidate in CandidatePaths(appBase))
        {
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }
}
