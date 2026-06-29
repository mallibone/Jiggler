namespace MouseJiggler.Services;

/// <summary>
/// Selects the platform <see cref="IMouseInput"/> implementation at compile time. Each branch
/// references only the type that is compiled for that target framework (CoreGraphics lives in
/// <c>Platforms/MacOS</c>, user32 in <c>Platforms/Windows</c>), so the guards are required.
/// </summary>
internal static class MouseInputFactory
{
	public static IMouseInput Create() =>
#if MACOS
		new Interop.CoreGraphicsNative();
#elif WINDOWS
		new Interop.WindowsInput();
#else
		throw new System.PlatformNotSupportedException("No IMouseInput implementation for this platform.");
#endif
}
