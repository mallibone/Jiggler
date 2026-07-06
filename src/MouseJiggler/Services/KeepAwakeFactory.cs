namespace MouseJiggler.Services;

/// <summary>
/// Selects the platform <see cref="IKeepAwake"/> implementation at compile time, mirroring
/// <see cref="MouseInputFactory"/>. Each branch references only the type that is compiled for
/// that target framework, so the guards are required.
/// </summary>
internal static class KeepAwakeFactory
{
	public static IKeepAwake Create() =>
#if MACOS
		new Interop.MacOSKeepAwake();
#elif WINDOWS
		new Interop.WindowsKeepAwake();
#else
		throw new System.PlatformNotSupportedException("No IKeepAwake implementation for this platform.");
#endif
}
