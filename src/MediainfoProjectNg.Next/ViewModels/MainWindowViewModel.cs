using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Core.Loading;

namespace MediainfoProjectNg.Next.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly MediaLoadService _loadService;
    private readonly IMediaMetadataReader _metadataReader;
    private int _loadGeneration;

    public MainWindowViewModel(MediaLoadService loadService, IMediaMetadataReader metadataReader)
    {
        _loadService = loadService;
        _metadataReader = metadataReader;
        Files.CollectionChanged += OnFilesCollectionChanged;
        ApplyMediaInfoVersionToTitle();
    }

    public ObservableCollection<MediaFileRowViewModel> Files { get; } = new();

    [ObservableProperty]
    public partial string TitleString { get; set; } = "mediainfo project ng next";

    [ObservableProperty]
    public partial string StatusString { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSummary))]
    public partial MediaFileRowViewModel? SelectedFile { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TogglePanelButtonText))]
    public partial bool IsSummaryPanelVisible { get; set; } = true;

    [ObservableProperty]
    public partial string FileCountText { get; set; } = "列表中共有 0 个文件";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public string SelectedSummary => SelectedFile?.Summary ?? string.Empty;

    public string TogglePanelButtonText => IsSummaryPanelVisible ? "隐藏右侧面板" : "显示右侧面板";

    public bool MediaInfoAvailable { get; private set; }

    public string? MediaInfoUnavailableMessage { get; private set; }

    private void ApplyMediaInfoVersionToTitle()
    {
        var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var baseTitle = $"mediainfo project ng next {appVersion}";

        var version = _metadataReader.GetLibraryVersion();
        if (string.IsNullOrWhiteSpace(version))
        {
            MediaInfoAvailable = false;
            MediaInfoUnavailableMessage = "无法载入适用的 mediainfo，请检查！";
            TitleString = $"{baseTitle} [Mediainfo: Unavailable]";
            StatusString = MediaInfoUnavailableMessage;
            return;
        }

        MediaInfoAvailable = true;
        // Legacy strips a fixed "MediaInfoLib - v" prefix (15 chars) when present.
        var display = version.StartsWith("MediaInfoLib - v", StringComparison.Ordinal)
            ? version[15..]
            : version;
        TitleString = $"{baseTitle} [Mediainfo: {display}]";
        StatusString = $"Mediainfo DLL {display} at your service.";
    }

    [RelayCommand]
    private void ToggleSummaryPanel()
    {
        IsSummaryPanelVisible = !IsSummaryPanelVisible;
    }

    /// <summary>
    /// Legacy Clear! is always clickable (even when empty or loading).
    /// Invalidates in-flight loads so results are not re-appended after clear.
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        _loadGeneration++;
        IsLoading = false;
        Files.Clear();
        SelectedFile = null;
        StatusString = string.Empty;
        OnPropertyChanged(nameof(SelectedSummary));
    }

    public void RemoveRows(IEnumerable<MediaFileRowViewModel> rows)
    {
        foreach (var row in rows.ToList())
        {
            Files.Remove(row);
        }

        if (SelectedFile is not null && !Files.Contains(SelectedFile))
        {
            SelectedFile = null;
            OnPropertyChanged(nameof(SelectedSummary));
        }
    }

    partial void OnSelectedFileChanged(MediaFileRowViewModel? value) =>
        OnPropertyChanged(nameof(SelectedSummary));

    /// <summary>
    /// Sequential load via MediaLoadService. Progress updates StatusString on the caller context.
    /// Call from the UI thread so status text stays on the UI sync context.
    /// </summary>
    public async Task LoadPathsAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        if (paths.Count == 0 || IsLoading)
        {
            return;
        }

        var generation = ++_loadGeneration;
        IsLoading = true;
        StatusString = string.Empty;

        try
        {
            // Legacy: oldList.Contains uses ordinal (case-sensitive) equality.
            var existing = Files.Select(f => f.FullPath).ToHashSet(StringComparer.Ordinal);
            var (infos, durationMs) = await _loadService.LoadAsync(
                paths.ToArray(),
                filter: path => existing.Contains(path),
                progressCallback: path =>
                {
                    if (generation == _loadGeneration)
                    {
                        StatusString = Path.GetFileName(path);
                    }
                },
                cancellationToken).ConfigureAwait(true);

            if (generation != _loadGeneration)
            {
                return;
            }

            foreach (var info in infos)
            {
                Files.Add(new MediaFileRowViewModel(info));
                existing.Add(info.GeneralInfo.FullPath);
            }

            // Legacy English timing string retained for V1 parity.
            StatusString = $"Total time cost: {durationMs}ms";
        }
        catch (OperationCanceledException)
        {
            if (generation == _loadGeneration)
            {
                StatusString = "已取消";
            }
        }
        catch (Exception ex)
        {
            if (generation == _loadGeneration)
            {
                StatusString = ex.Message;
            }
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                IsLoading = false;
            }
        }
    }

    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        FileCountText = $"列表中共有 {Files.Count} 个文件";
    }
}
