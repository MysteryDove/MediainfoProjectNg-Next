using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediainfoProjectNg.Next.Core.Abstractions;
using MediainfoProjectNg.Next.Core.Loading;
using MediainfoProjectNg.Next.Core.Presentation;

namespace MediainfoProjectNg.Next.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly MediaLoadService _loadService;
    private readonly IMediaMetadataReader _metadataReader;
    private int _loadGeneration;
    private readonly List<MediaFileRowViewModel> _canonicalFiles = new();
    private readonly HashSet<IssueCategory> _activeCategories = new();

    public MainWindowViewModel(MediaLoadService loadService, IMediaMetadataReader metadataReader)
    {
        _loadService = loadService;
        _metadataReader = metadataReader;
        Files.CollectionChanged += OnFilesCollectionChanged;

        CategoryToggles = IssueCategoryRegistry.FilterableCategories
            .Select(c => new CategoryToggleViewModel(
                c,
                IssueCategoryRegistry.ChineseLabels[c],
                CategorySwatchToken(c)))
            .ToList();

        ApplyMediaInfoVersionToTitle();
        RefreshCategoryCounts();
    }

    /// <summary>Visible (filtered) collection bound to FileGrid.</summary>
    public ObservableCollection<MediaFileRowViewModel> Files { get; } = new();

    public IReadOnlyList<CategoryToggleViewModel> CategoryToggles { get; }

    [ObservableProperty]
    public partial string TitleString { get; set; } = "mediainfo project ng next";

    [ObservableProperty]
    public partial string StatusString { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSummary))]
    [NotifyPropertyChangedFor(nameof(SelectedIssueItems))]
    [NotifyPropertyChangedFor(nameof(HasSelectedIssues))]
    public partial MediaFileRowViewModel? SelectedFile { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TogglePanelButtonText))]
    public partial bool IsSummaryPanelVisible { get; set; } = true;

    [ObservableProperty]
    public partial string FileCountText { get; set; } = "列表中共有 0 个文件";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public string SelectedSummary => SelectedFile?.Summary ?? string.Empty;

    public IReadOnlyList<IssueDisplayItem> SelectedIssueItems =>
        SelectedFile?.IssueItems ?? Array.Empty<IssueDisplayItem>();

    public bool HasSelectedIssues => SelectedIssueItems.Count > 0;

    public string TogglePanelButtonText => IsSummaryPanelVisible ? "隐藏右侧面板" : "显示右侧面板";

    public bool MediaInfoAvailable { get; private set; }

    public string? MediaInfoUnavailableMessage { get; private set; }

    /// <summary>Exposed for tests: canonical loaded set size.</summary>
    public int CanonicalCount => _canonicalFiles.Count;

    private void ApplyMediaInfoVersionToTitle()
    {
        // Prefer the desktop host assembly (entry) so Version from Directory.Build.props is used.
        var appVersion = AppVersionInfo.GetProductVersion(
            System.Reflection.Assembly.GetEntryAssembly()
            ?? System.Reflection.Assembly.GetExecutingAssembly());

        var version = _metadataReader.GetLibraryVersion();
        if (string.IsNullOrWhiteSpace(version))
        {
            MediaInfoAvailable = false;
            MediaInfoUnavailableMessage = "无法载入适用的 mediainfo，请检查！";
            TitleString = AppVersionInfo.FormatWindowTitle(appVersion, mediaInfoLibraryVersion: null);
            StatusString = MediaInfoUnavailableMessage;
            return;
        }

        MediaInfoAvailable = true;
        var display = version.StartsWith("MediaInfoLib - v", StringComparison.Ordinal)
            ? version["MediaInfoLib - v".Length..]
            : version;
        TitleString = AppVersionInfo.FormatWindowTitle(appVersion, version);
        StatusString = $"Mediainfo DLL {display} at your service.";
    }

    [RelayCommand]
    private void ToggleSummaryPanel()
    {
        IsSummaryPanelVisible = !IsSummaryPanelVisible;
    }

    [RelayCommand]
    private void Clear()
    {
        _loadGeneration++;
        IsLoading = false;
        _canonicalFiles.Clear();
        _activeCategories.Clear();
        foreach (var t in CategoryToggles)
        {
            t.IsSelected = false;
        }

        RebuildVisible(clearSelection: true);
        StatusString = string.Empty;
    }

    [RelayCommand]
    private void ClearCategoryFilters()
    {
        _activeCategories.Clear();
        foreach (var t in CategoryToggles)
        {
            t.IsSelected = false;
        }

        RebuildVisible(clearSelection: false);
    }

    [RelayCommand]
    private void ToggleCategory(CategoryToggleViewModel? toggle)
    {
        if (toggle is null || !toggle.IsEnabled)
        {
            return;
        }

        if (_activeCategories.Contains(toggle.Category))
        {
            _activeCategories.Remove(toggle.Category);
            toggle.IsSelected = false;
        }
        else
        {
            _activeCategories.Add(toggle.Category);
            toggle.IsSelected = true;
        }

        RebuildVisible(clearSelection: false);
    }

    public void RemoveRows(IEnumerable<MediaFileRowViewModel> rows)
    {
        foreach (var row in rows.ToList())
        {
            _canonicalFiles.Remove(row);
            Files.Remove(row);
        }

        RefreshCategoryCounts();
        if (SelectedFile is not null && !_canonicalFiles.Contains(SelectedFile))
        {
            SelectedFile = null;
            OnPropertyChanged(nameof(SelectedSummary));
            OnPropertyChanged(nameof(SelectedIssueItems));
            OnPropertyChanged(nameof(HasSelectedIssues));
        }

        // Re-apply filter in case counts/visibility need refresh
        RebuildVisible(clearSelection: false);
    }

    /// <summary>
    /// View-selection boundary helper for Extended selection after filter changes.
    /// </summary>
    public (MediaFileRowViewModel? Primary, IReadOnlyList<MediaFileRowViewModel> Selected)
        ReconcileExtendedSelection(
            MediaFileRowViewModel? primary,
            IReadOnlyList<MediaFileRowViewModel> selected)
    {
        var visible = Files.ToHashSet();
        return SelectionReconciler.Reconcile(primary, selected, visible);
    }

    partial void OnSelectedFileChanged(MediaFileRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedSummary));
        OnPropertyChanged(nameof(SelectedIssueItems));
        OnPropertyChanged(nameof(HasSelectedIssues));
    }

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
            var pathComparer = OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            var distinctPaths = paths.Distinct(pathComparer).ToArray();
            var existing = _canonicalFiles.Select(f => f.FullPath).ToHashSet(pathComparer);
            // Progress<T> captures the current SynchronizationContext (UI) at construction.
            var progress = new Progress<string>(path =>
            {
                if (generation == _loadGeneration)
                {
                    StatusString = Path.GetFileName(path);
                }
            });
            var (infos, durationMs) = await _loadService.LoadAsync(
                distinctPaths,
                filter: path => existing.Contains(path),
                progress: progress,
                cancellationToken).ConfigureAwait(true);

            if (generation != _loadGeneration)
            {
                return;
            }

            foreach (var info in infos)
            {
                if (!existing.Add(info.GeneralInfo.FullPath))
                {
                    continue;
                }

                var row = new MediaFileRowViewModel(info);
                _canonicalFiles.Add(row);
            }

            RebuildVisible(clearSelection: false);
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

    private void RebuildVisible(bool clearSelection)
    {
        RefreshCategoryCounts();

        var filtered = CategoryFilterEngine.FilterRows(
            _canonicalFiles,
            r => r.IssueItems,
            _activeCategories);

        var previousPrimary = SelectedFile;
        var visible = filtered.ToHashSet();
        if (clearSelection || previousPrimary is not null && !visible.Contains(previousPrimary))
        {
            // Publish cleared detail state before replacing the visible collection.
            SelectedFile = null;
        }

        Files.Clear();
        foreach (var row in filtered)
        {
            Files.Add(row);
        }

        if (!clearSelection && previousPrimary is not null && visible.Contains(previousPrimary))
        {
            var (primary, _) = SelectionReconciler.Reconcile(
                previousPrimary,
                previousPrimary is null ? Array.Empty<MediaFileRowViewModel>() : [previousPrimary],
                visible);
            SelectedFile = primary;
        }

        OnPropertyChanged(nameof(SelectedSummary));
        OnPropertyChanged(nameof(SelectedIssueItems));
        OnPropertyChanged(nameof(HasSelectedIssues));
    }

    private void RefreshCategoryCounts()
    {
        var counts = CategoryFilterEngine.ComputeDistinctFileCounts(
            _canonicalFiles.Select(r => r.IssueItems).ToList());

        foreach (var toggle in CategoryToggles)
        {
            toggle.Count = counts[toggle.Category];
            toggle.IsEnabled = toggle.Count > 0;
            if (!toggle.IsEnabled && toggle.IsSelected)
            {
                toggle.IsSelected = false;
                _activeCategories.Remove(toggle.Category);
            }
        }
    }

    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        FileCountText = $"列表中共有 {Files.Count} 个文件";
    }

    private static ColorToken CategorySwatchToken(IssueCategory category) => category switch
    {
        IssueCategory.ContainerNaming => ColorToken.ErrorViolet,
        IssueCategory.Track => ColorToken.WarningDelayTeal,
        IssueCategory.FrameRate => ColorToken.FpsNtsc,
        IssueCategory.VideoColor => ColorToken.ColorSpaceNon420,
        IssueCategory.Chapter => ColorToken.WarningYellow,
        _ => ColorToken.None,
    };
}
