using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LaserGRBL.Avalonia.Services;

namespace LaserGRBL.Avalonia;

public partial class App : Application
{
    public AppServices Services { get; private set; } = AppBootstrapper.CreateDefaultServices();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = Services.MainWindow };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
