using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MediainfoProjectNg.Next.Core.Loading;
using MediainfoProjectNg.Next.MediaInfo;
using MediainfoProjectNg.Next.ViewModels;
using MediainfoProjectNg.Next.Views;

namespace MediainfoProjectNg.Next;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Manual composition root (no DI container): reader → load service → VM.
            var reader = new MediaInfoMetadataReader();
            var loadService = new MediaLoadService(reader);
            var viewModel = new MainWindowViewModel(loadService, reader);

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
