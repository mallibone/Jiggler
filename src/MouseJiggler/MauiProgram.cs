#if MACOS
using Microsoft.Maui.Platforms.MacOS.Hosting;
using Microsoft.Maui.Platforms.MacOS.Essentials;
#elif WINDOWS
using Microsoft.Maui.Controls.Hosting.WPF;
using Microsoft.Maui.Platforms.Windows.WPF.Essentials;
#endif
#if DEBUG && MACOS
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace MouseJiggler;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

#if MACOS
		builder
			.UseMauiAppMacOS<App>()
			.AddMacOSEssentials();
#elif WINDOWS
		builder
			.UseMauiAppWPF<App>()
			.UseWPFEssentials();
#endif

#if DEBUG && MACOS
		builder.AddMauiDevFlowAgent();
#endif

		return builder.Build();
	}
}
