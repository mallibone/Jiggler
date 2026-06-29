#if MACOS
using Microsoft.Maui.Platforms.MacOS.Platform;
#endif

namespace MouseJiggler;

public class App : Application
{
	protected override Window CreateWindow(IActivationState? activationState)
	{
		var mainPage = IPlatformApplication.Current!.Services.GetRequiredService<MainPage>();
		var window = new Window(mainPage)
		{
			Title = "Mouse Jiggler",
			Width = 430,
			Height = 600,
		};

#if MACOS
		// Native macOS titlebar: keep a normal (non full-size) titlebar so content
		// sits cleanly below it and never overlaps the traffic lights.
		MacOSWindow.SetTitlebarStyle(window, MacOSTitlebarStyle.Unified);
		MacOSWindow.SetTitleVisibility(window, MacOSTitleVisibility.Visible);
		MacOSWindow.SetFullSizeContentView(window, false);
#endif

#if WINDOWS
		// Explicitly set the WPF window icon once the platform window exists. This drives
		// the top-left title-bar icon and the taskbar icon reliably — including in the
		// single-file Release build, where the .exe-icon fallback can fail. The icon is
		// embedded as a WPF resource (see the .csproj <Resource> entry).
		window.HandlerChanged += (_, _) =>
		{
			if (window.Handler?.PlatformView is System.Windows.Window platformWindow)
			{
				platformWindow.Icon = new System.Windows.Media.Imaging.BitmapImage(
					new Uri("pack://application:,,,/appicon.ico"));
			}
		};
#endif

		return window;
	}
}
