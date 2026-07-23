using System.Diagnostics;
using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Domain.IO;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Core.Loading;

/// <summary>
/// Ports legacy Utils.Load / LoadFile sequential workflow.
/// Filter polarity: skip when <paramref name="filter"/> returns true (legacy: oldList.Contains).
/// Progress is invoked on the caller's context (UI should call from UI thread).
/// </summary>
public sealed class MediaLoadService
{
    private readonly IMediaMetadataReader _reader;

    public MediaLoadService(IMediaMetadataReader reader)
    {
        _reader = reader;
    }

    public async Task<(IReadOnlyList<MediaFileInfo> Info, long DurationMs)> LoadAsync(
        string[] urls,
        Func<string, bool>? filter = null,
        Action<string>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var fileInfos = new List<MediaFileInfo>();
        var sw = Stopwatch.StartNew();

        foreach (var file in urls.Where(File.Exists))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = await LoadFileAsync(file, filter, progressCallback, cancellationToken).ConfigureAwait(false);
            if (info is not null)
            {
                fileInfos.Add(info);
            }
        }

        foreach (var dir in urls.Where(Directory.Exists))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FolderEnumeration.IsExcludedDirectoryName(dir))
            {
                continue;
            }

            foreach (var file in FolderEnumeration.EnumerateFolder(dir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var info = await LoadFileAsync(file, filter, progressCallback, cancellationToken).ConfigureAwait(false);
                if (info is not null)
                {
                    fileInfos.Add(info);
                }
            }
        }

        sw.Stop();
        return (fileInfos, sw.ElapsedMilliseconds);
    }

    public async Task<MediaFileInfo?> LoadFileAsync(
        string path,
        Func<string, bool>? filter = null,
        Action<string>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        if (FolderEnumeration.IsExcludedExtension(path))
        {
            return null;
        }

        // Legacy polarity: filter true => skip
        if (filter?.Invoke(path) ?? false)
        {
            return null;
        }

        progressCallback?.Invoke(path);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = _reader.Read(path);
            info.SetFindings(MediaValidator.CheckFile(info));
            return info;
        }, cancellationToken).ConfigureAwait(false);
    }
}
