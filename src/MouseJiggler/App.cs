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

		return window;
	}
}
