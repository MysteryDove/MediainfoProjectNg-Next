namespace MediainfoProjectNg.Next.Domain.IO;

/// <summary>
/// Ports legacy Utils.EnumerateFolder exclusion and ordering semantics.
/// </summary>
public static class FolderEnumeration
{
    public static readonly IReadOnlyList<string> ExcludeDirs = ["CDs", "Scans"];
    public static readonly IReadOnlyList<string> ExcludeExts = [".txt", ".log", ".torrent"];

    /// <summary>
    /// Yields files under folder: top-level files first, then BFS directories.
    /// Skips directories named CDs or Scans. Does not filter by extension (caller does).
    /// </summary>
    public static IEnumerable<string> EnumerateFolder(string folderPath, Func<string, IEnumerable<string>> getFiles, Func<string, IEnumerable<string>> getDirectories)
    {
        foreach (var file in getFiles(folderPath))
        {
            yield return file;
        }

        var folderQueue = new Queue<string>();
        foreach (var dir in getDirectories(folderPath))
        {
            folderQueue.Enqueue(dir);
        }

        while (folderQueue.Count > 0)
        {
            var currentFolder = folderQueue.Dequeue();
            if (ExcludeDirs.Contains(Path.GetFileName(currentFolder)))
            {
                continue;
            }

            foreach (var file in getFiles(currentFolder))
            {
                yield return file;
            }

            foreach (var dir in getDirectories(currentFolder))
            {
                folderQueue.Enqueue(dir);
            }
        }
    }

    public static IEnumerable<string> EnumerateFolder(string folderPath) =>
        EnumerateFolder(
            folderPath,
            Directory.GetFiles,
            Directory.GetDirectories);

    public static bool IsExcludedExtension(string path) =>
        ExcludeExts.Contains(Path.GetExtension(path));

    public static bool IsExcludedDirectoryName(string pathOrName) =>
        ExcludeDirs.Contains(Path.GetFileName(pathOrName));
}
