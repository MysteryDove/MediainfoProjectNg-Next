using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using MediainfoProjectNg.Next.ViewModels;

namespace MediainfoProjectNg.Next.Views;

public partial class MainWindow : Window
{
    private GridLength _rightPanelOriginalWidth = new(320);
    private bool _rightPanelVisible = true;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        // Do NOT call desktop.Shutdown() from Closing — that re-enters close and
        // stack-overflows on macOS (SIGABRT / HandleFatalStackOverflow). App uses
        // ShutdownMode.OnMainWindowClose instead (see App.axaml.cs).
        FileGrid.AddHandler(DragDrop.DropEvent, FileGrid_OnDrop);
        FileGrid.AddHandler(DragDrop.DragOverEvent, FileGrid_OnDragOver);
        // Tunnel so empty-area unselect runs before row selection steals the press.
        FileGrid.AddHandler(InputElement.PointerPressedEvent, FileGrid_OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (Vm is { MediaInfoAvailable: false, MediaInfoUnavailableMessage: { } message })
        {
            await ShowSimpleMessageAsync(message).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Legacy ToggleRightPanelButton_Click: collapse visibility + column width to 0 / restore 320.
    /// </summary>
    private void ToggleRightPanelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var right = MainContentGrid.ColumnDefinitions[2];
        var splitter = MainContentGrid.ColumnDefinitions[1];

        if (_rightPanelVisible)
        {
            _rightPanelOriginalWidth = right.Width;
            right.Width = new GridLength(0);
            right.MinWidth = 0;
            splitter.Width = new GridLength(0);
            RightPanel.IsVisible = false;
            RightSplitter.IsVisible = false;
            ToggleRightPanelButton.Content = "显示右侧面板";
            _rightPanelVisible = false;
        }
        else
        {
            right.Width = _rightPanelOriginalWidth.IsAbsolute && _rightPanelOriginalWidth.Value > 0
                ? _rightPanelOriginalWidth
                : new GridLength(320);
            right.MinWidth = 320;
            splitter.Width = new GridLength(2);
            RightPanel.IsVisible = true;
            RightSplitter.IsVisible = true;
            ToggleRightPanelButton.Content = "隐藏右侧面板";
            _rightPanelVisible = true;
        }
    }

    private async void OpenFilesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开文件",
            AllowMultiple = true,
        }).ConfigureAwait(true);

        if (files.Count == 0)
        {
            return;
        }

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Cast<string>()
            .ToArray();

        await Vm.LoadPathsAsync(paths).ConfigureAwait(true);
    }

    private async void OpenFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "打开文件夹",
            AllowMultiple = false,
        }).ConfigureAwait(true);

        if (folders.Count == 0)
        {
            return;
        }

        var path = folders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await Vm.LoadPathsAsync([path]).ConfigureAwait(true);
    }

    private void FileGrid_OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link
            : DragDropEffects.None;
    }

    private async void FileGrid_OnDrop(object? sender, DragEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var items = e.DataTransfer.TryGetFiles();
        if (items is null || items.Length == 0)
        {
            return;
        }

        var paths = items
            .Select(item => item.TryGetLocalPath())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Cast<string>()
            .ToList();

        if (paths.Count == 0)
        {
            return;
        }

        await Vm.LoadPathsAsync(paths).ConfigureAwait(true);
    }

    private void FileGrid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || Vm is null)
        {
            return;
        }

        var selected = FileGrid.SelectedItems
            .OfType<MediaFileRowViewModel>()
            .ToList();
        if (selected.Count == 0)
        {
            return;
        }

        Vm.RemoveRows(selected);
        e.Handled = true;
    }

    private void FileGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var row = (e.Source as Control)?.FindAncestorOfType<DataGridRow>();
        var item = row?.DataContext as MediaFileRowViewModel ?? Vm?.SelectedFile;
        if (item is null)
        {
            return;
        }

        Vm!.SelectedFile = item;
        var win = new TechnicalWindow(item.Model)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        win.Show(this);
        e.Handled = true;
    }

    /// <summary>
    /// Legacy: click empty ScrollViewer / non-row chrome → UnselectAll.
    /// Uses tunnel routing so we observe the true hit target before selection changes.
    /// </summary>
    private void FileGrid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(FileGrid).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is not Control source)
        {
            return;
        }

        // Click on a data row / cell / header → keep selection behavior.
        if (source.FindAncestorOfType<DataGridRow>() is not null
            || source.FindAncestorOfType<DataGridColumnHeader>() is not null
            || source.FindAncestorOfType<DataGridCell>() is not null)
        {
            return;
        }

        // Empty surface: scrollbars, presenters, or the grid itself (OG: ScrollViewer).
        var typeName = source.GetType().Name;
        if (source is ScrollBar
            || typeName.Contains("Scroll", StringComparison.Ordinal)
            || typeName.Contains("RowsPresenter", StringComparison.Ordinal)
            || typeName.Contains("CellsPresenter", StringComparison.Ordinal)
            || typeName.Contains("DataGrid", StringComparison.Ordinal)
            || ReferenceEquals(source, FileGrid))
        {
            ClearGridSelection();
        }
    }

    private void ClearGridSelection()
    {
        FileGrid.SelectedItems.Clear();
        FileGrid.SelectedItem = null;
        if (Vm is not null)
        {
            Vm.SelectedFile = null;
        }
    }

    private async Task ShowSimpleMessageAsync(string message)
    {
        var dialog = new Window
        {
            Title = "mediainfo project ng next",
            Width = 420,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                    new Button
                    {
                        Content = "确定",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        MinWidth = 72,
                    },
                },
            },
        };

        if (dialog.Content is StackPanel panel && panel.Children[1] is Button ok)
        {
            ok.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(this).ConfigureAwait(true);
    }
}
