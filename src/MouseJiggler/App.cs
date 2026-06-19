using Microsoft.Maui.Platforms.MacOS.Platform;

namespace MouseJiggler;

public class App : Application
{
	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage())
		{
			Title = "Mouse Jiggler",
			Width = 430,
			Height = 600,
		};

		// Native macOS titlebar: keep a normal (non full-size) titlebar so content
		// sits cleanly below it and never overlaps the traffic lights.
		MacOSWindow.SetTitlebarStyle(window, MacOSTitlebarStyle.Unified);
		MacOSWindow.SetTitleVisibility(window, MacOSTitleVisibility.Visible);
		MacOSWindow.SetFullSizeContentView(window, false);

		return window;
	}
}
