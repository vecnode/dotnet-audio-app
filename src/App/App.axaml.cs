using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace App;

// Main application class managing Avalonia framework lifecycle and window creation.
// Handles application initialization and main window instantiation for desktop environments.
public partial class App : Application
{
    // Initializes the Avalonia application by loading XAML resources and themes.
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Called after Avalonia framework initialization is complete.
    // Creates and displays the main application window for desktop environments.
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
