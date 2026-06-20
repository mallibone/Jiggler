using Microsoft.Maui.Platforms.MacOS.Hosting;
using Microsoft.Maui.Platforms.MacOS.Essentials;
#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace MouseJiggler;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiAppMacOS<App>()
			.AddMacOSEssentials();

#if DEBUG
		builder.AddMauiDevFlowAgent();
#endif

		return builder.Build();
	}
}
