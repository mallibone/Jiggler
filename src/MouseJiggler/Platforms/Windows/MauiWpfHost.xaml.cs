using Microsoft.Maui.Platforms.Windows.WPF;

namespace MouseJiggler;

/// <summary>
/// Windows (WPF) host application — the maui-labs analogue of the macOS
/// <see cref="MouseJigglerDelegate"/>. Declared as the WPF ApplicationDefinition so the SDK
/// generates the <c>[STAThread]</c> entry point; it bootstraps the shared MAUI app.
/// </summary>
public partial class MauiWpfHost : MauiWPFApplication
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
