using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MediainfoProjectNg.Next.Core.Loading;
using MediainfoProjectNg.Next.Domain.Validation;
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
            // OG closes the process when the main window closes (even if tech windows open).
            // Prefer ShutdownMode over calling Shutdown() from Window.Closing (re-entrancy / SO on macOS).
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Manual composition root (no DI container): reader → load service → VM.
            // Post-V1 desktop is the only owner that activates CollationV1.
            var reader = new MediaInfoMetadataReader();
            var loadService = new MediaLoadService(reader, ValidationProfile.CollationV1);
            var viewModel = new MainWindowViewModel(loadService, reader);

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
