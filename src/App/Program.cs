using Avalonia;
using System;

namespace App;

// Application entry point and Avalonia framework configuration.
// Configures cross-platform UI framework with professional theming and logging.
class Program
{
    // Main application entry point. Initializes Avalonia framework and starts desktop application.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Configures Avalonia application with cross-platform support and professional theming.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
