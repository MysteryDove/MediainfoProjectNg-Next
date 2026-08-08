using System.Diagnostics;
using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Domain.IO;
using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Core.Loading;

/// <summary>
/// Loads independent media files with bounded parallelism while preserving input order.
/// Filter polarity: skip when <paramref name="filter"/> returns true (legacy: oldList.Contains).
/// Progress uses <see cref="IProgress{T}"/>; construct <see cref="Progress{T}"/> on the UI thread
/// so Report posts to the captured SynchronizationContext.
/// Profile defaults to <see cref="ValidationProfile.LegacyV1"/> when omitted.
/// </summary>
public sealed class MediaLoadService
{
    private const int MaxParallelReads = 4;
    private readonly IMediaMetadataReader _reader;
    private readonly ValidationProfile _profile;

    public MediaLoadService(IMediaMetadataReader reader, ValidationProfile profile = ValidationProfile.LegacyV1)
    {
        _reader = reader;
        _profile = profile;
    }

    public ValidationProfile Profile => _profile;

    public async Task<(IReadOnlyList<MediaFileInfo> Info, long DurationMs)> LoadAsync(
        string[] urls,
        Func<string, bool>? filter = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<string>();
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var seen = new HashSet<string>(pathComparer);
        var sw = Stopwatch.StartNew();

        void AddCandidate(string path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FolderEnumeration.IsExcludedExtension(path)
                || !seen.Add(Path.GetFullPath(path))
                || filter?.Invoke(path) == true)
            {
                return;
            }

            candidates.Add(path);
        }

        foreach (var file in urls.Where(File.Exists))
        {
            AddCandidate(file);
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
                AddCandidate(file);
            }
        }

        var loaded = new MediaFileInfo?[candidates.Count];
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = MaxParallelReads,
        };
        await Parallel.ForEachAsync(
            Enumerable.Range(0, candidates.Count),
            parallelOptions,
            (index, token) =>
            {
                token.ThrowIfCancellationRequested();
                var path = candidates[index];
                progress?.Report(path);
                loaded[index] = ReadAndValidate(path);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        sw.Stop();
        return (loaded.Where(info => info is not null).Cast<MediaFileInfo>().ToList(), sw.ElapsedMilliseconds);
    }

    public async Task<MediaFileInfo?> LoadFileAsync(
        string path,
        Func<string, bool>? filter = null,
        IProgress<string>? progress = null,
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

        // IProgress.Report posts to the SynchronizationContext captured when Progress<T> was created (UI thread).
        progress?.Report(path);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ReadAndValidate(path);
        }, cancellationToken).ConfigureAwait(false);
    }

    private MediaFileInfo ReadAndValidate(string path)
    {
        var info = _reader.Read(path);
        info.SetFindings(MediaValidator.CheckFile(info, _profile));
        return info;
    }
}
