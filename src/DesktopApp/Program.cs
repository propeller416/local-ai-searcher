using Avalonia;
using System;
using Velopack;

namespace DesktopApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        PreloadMacOsNativeLibraries();

        VelopackApp.Build().Run();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void PreloadMacOsNativeLibraries()
    {
        if (OperatingSystem.IsMacOS())
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var runtimesDirArm64 = System.IO.Path.Combine(baseDir, "runtimes", "osx-arm64", "native");
                var runtimesDirX64 = System.IO.Path.Combine(baseDir, "runtimes", "osx-x64", "native");
                var searchDirs = new[] { baseDir, runtimesDirArm64, runtimesDirX64 };
                
                // Загружаем зависимости libllama.dylib вручную, чтобы dyld смог их найти
                // несмотря на отсутствие @loader_path в LC_RPATH.
                string[] libs = [
                    "libggml-base.dylib",
                    "libggml-cpu.dylib",
                    "libggml-blas.dylib",
                    "libggml-metal.dylib",
                    "libggml.dylib",
                    "libllama.dylib"
                ];

                foreach (var lib in libs)
                {
                    foreach (var dir in searchDirs)
                    {
                        var path = System.IO.Path.Combine(dir, lib);
                        if (System.IO.File.Exists(path))
                        {
                            try
                            {
                                System.Runtime.InteropServices.NativeLibrary.Load(path);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load {lib} from {path}: {ex.Message}");
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to preload macOS native libraries: {ex}");
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
