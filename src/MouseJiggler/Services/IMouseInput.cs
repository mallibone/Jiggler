namespace MouseJiggler.Services;

/// <summary>A cursor position in the global, top-left-origin coordinate space.</summary>
public readonly record struct MousePoint(double X, double Y);

/// <summary>
/// Platform-neutral surface over the OS input/idle APIs the jiggler needs. Each platform
/// supplies an implementation: CoreGraphics on macOS, user32 on Windows. Keeps
/// <see cref="MouseJiggleService"/> free of any platform types so it can be shared verbatim.
/// </summary>
public interface IMouseInput
{
	/// <summary>Seconds since the last input event of any kind — the system idle time.</summary>
	double GetIdleSeconds();

	/// <summary>Current cursor location.</summary>
	MousePoint GetCursorPosition();

	/// <summary>
	/// Posts a synthetic mouse-move event. This resets the system idle timer, so the machine
	/// registers as "active". On some platforms this requires a permission grant.
	/// </summary>
	void PostMouseMove(MousePoint target);

	/// <summary>
	/// Moves the cursor without posting an input event. Needs no special permission but does
	/// not reset the idle timer; used as a fallback when synthetic events are blocked.
	/// </summary>
	void WarpCursor(MousePoint target);

	/// <summary>True if the app may already post synthetic events (permission granted).</summary>
	bool CanPostEvents();

	/// <summary>
	/// Asks the system to prompt for permission to post events. Returns the current grant
	/// state. A no-op returning <c>true</c> on platforms with no such gate.
	/// </summary>
	bool RequestPostEventsAccess();
}
