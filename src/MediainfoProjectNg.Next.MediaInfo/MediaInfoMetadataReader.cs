using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.MediaInfo.Native;
using MediainfoProjectNg.Next.MediaInfo.Projection;

namespace MediainfoProjectNg.Next.MediaInfo;

/// <summary>
/// MediaInfo-backed <see cref="IMediaMetadataReader"/> using LibraryImport C API bindings.
/// Requires a bundled or resolvable native MediaInfo shared library.
/// </summary>
public sealed class MediaInfoMetadataReader : IMediaMetadataReader
{
    public MediaFileInfo Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Media file not found.", path);
        }

        IntPtr handle = IntPtr.Zero;
        try
        {
            handle = MediaInfoNative.New();
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "无法载入适用的 mediainfo，请检查！Native MediaInfo library failed to create a handle.");
            }

            if (MediaInfoNative.Open(handle, path) == 0)
            {
                throw new InvalidOperationException($"MediaInfo failed to open: {path}");
            }

            return MediaInfoProjector.Project(handle, path);
        }
        catch (DllNotFoundException ex)
        {
            throw new InvalidOperationException(
                "无法载入适用的 mediainfo，请检查！Native MediaInfo library was not found. Build natives via native/build-host.sh.",
                ex);
        }
        catch (EntryPointNotFoundException ex)
        {
            throw new InvalidOperationException(
                "MediaInfo native entry point missing. Ensure the correct shared library is bundled.",
                ex);
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                try
                {
                    MediaInfoNative.Close(handle);
                }
                catch
                {
                    // ignore close failures
                }

                try
                {
                    MediaInfoNative.Delete(handle);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    public string? GetLibraryVersion()
    {
        IntPtr handle = IntPtr.Zero;
        try
        {
            handle = MediaInfoNative.New();
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var version = MediaInfoNative.Option(handle, "Info_Version");
            if (string.IsNullOrEmpty(version) || version.Contains("Unable to load", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return version;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                try { MediaInfoNative.Close(handle); } catch { /* ignore */ }
                try { MediaInfoNative.Delete(handle); } catch { /* ignore */ }
            }
        }
    }
}
