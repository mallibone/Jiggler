using Foundation;
using Microsoft.Maui.Platforms.MacOS.Platform;

namespace MouseJiggler;

[Register("MouseJigglerDelegate")]
public class MouseJigglerDelegate : MacOSMauiApplication
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
