// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Avalonia;

namespace Starquake.ScreenEditor;

// Public where the rest of the editor is internal, as is BuildAvaloniaApp below: the Avalonia
// visual designer looks both up by name from outside the assembly. Internal would compile and run,
// and break only the previewer.
public class Program
{
    // Nothing here may touch Avalonia, third-party APIs, or anything relying on a
    // SynchronizationContext until AppMain has been called. None of it is initialised yet.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Looked up by name by the visual designer as well as by Main. Do not remove or rename.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
