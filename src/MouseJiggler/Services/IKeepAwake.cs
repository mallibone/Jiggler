namespace MouseJiggler.Services;

/// <summary>
/// Platform-neutral surface over the OS power-management APIs. While jiggling is active the
/// app must hold a keep-awake assertion: without one the display can sleep before the first
/// nudge, and (on macOS) App Nap throttles the background timer so jiggling stops entirely
/// when the app loses focus. Kept separate from <see cref="IMouseInput"/> — power management
/// is a different concern from input simulation.
/// </summary>
public interface IKeepAwake
{
	/// <summary>Acquires the keep-awake assertion. Idempotent.</summary>
	void Begin();

	/// <summary>Releases the keep-awake assertion. Idempotent; safe without a prior Begin.</summary>
	void End();
}
