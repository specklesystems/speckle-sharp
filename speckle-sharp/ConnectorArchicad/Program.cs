using Avalonia;
using Avalonia.ReactiveUI;
using System.IO;


namespace Archicad.Launcher
{
    class Program
    {
        public static void Main (string[] args)
        {
            BuildAvaloniaApp ().StartWithClassicDesktopLifetime (args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
        }

        public static AppBuilder BuildAvaloniaApp ()
        {
            return AppBuilder.Configure (() => new DesktopUI2.App { ConnectorBindings = new ArchicadBinding () })
                .UsePlatformDetect ()
                .With (new X11PlatformOptions { UseGpu = false })
                .With (new MacOSPlatformOptions { ShowInDock = true })
                .With (new AvaloniaNativePlatformOptions { AvaloniaNativeLibraryPath = GetAvaloniaNativeLibraryPath () })
                .With (new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
                .With (new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
                .LogToTrace ()
                .UseReactiveUI ();
        }

        private static string? GetAvaloniaNativeLibraryPath ()
		{
			string path = Path.GetDirectoryName (typeof (DesktopUI2.App).Assembly.Location);
            if (path is null)
			{
                return null;
			}

            return Path.Combine (path, "Native", "libAvalonia.Native.OSX.dylib");
        }
    }
}
