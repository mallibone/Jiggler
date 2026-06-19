using System.Runtime.InteropServices;
using CoreGraphics;
using ObjCRuntime;

namespace MouseJiggler.Interop;

/// <summary>
/// Thin P/Invoke layer over the CoreGraphics event APIs used to read system idle
/// time and nudge the mouse cursor. All entry points are part of the public
/// CoreGraphics framework and are App Sandbox compatible.
/// </summary>
internal static class CoreGraphicsNative
{
	// CGEventSourceStateID
	private const int HidSystemState = 1; // kCGEventSourceStateHIDSystemState

	// CGEventType
	private const uint AnyInputEventType = 0xFFFFFFFF; // kCGAnyInputEventType
	private const uint MouseMoved = 5;                 // kCGEventMouseMoved

	// CGEventTapLocation
	private const uint HidEventTap = 0; // kCGHIDEventTap

	// CGMouseButton
	private const uint MouseButtonLeft = 0; // kCGMouseButtonLeft

	/// <summary>
	/// Seconds since the last HID input event of any kind — the system idle time.
	/// Mirrors the idle tracking the original Python script did with a listener.
	/// </summary>
	public static double GetIdleSeconds()
		=> CGEventSourceSecondsSinceLastEventType(HidSystemState, AnyInputEventType);

	/// <summary>Current cursor location in the global (top-left origin) coordinate space.</summary>
	public static CGPoint GetCursorPosition()
	{
		var ev = CGEventCreate(IntPtr.Zero);
		try
		{
			return ev == IntPtr.Zero ? CGPoint.Empty : CGEventGetLocation(ev);
		}
		finally
		{
			if (ev != IntPtr.Zero)
				CFRelease(ev);
		}
	}

	/// <summary>
	/// Posts a synthetic mouse-move event. This resets the HID idle timer, so the
	/// machine registers as "active" (Teams/Slack stay available). Requires the
	/// user to have granted Accessibility (Post Events) permission.
	/// </summary>
	public static void PostMouseMove(CGPoint target)
	{
		var ev = CGEventCreateMouseEvent(IntPtr.Zero, MouseMoved, target, MouseButtonLeft);
		if (ev == IntPtr.Zero)
			return;
		try
		{
			CGEventPost(HidEventTap, ev);
		}
		finally
		{
			CFRelease(ev);
		}
	}

	/// <summary>
	/// Warps the cursor to a position without posting an event. Needs no special
	/// permission and is fully sandbox-clean, but does not reset the HID idle timer
	/// as reliably as a synthetic event.
	/// </summary>
	public static void WarpCursor(CGPoint target)
		=> CGWarpMouseCursorPosition(target);

	/// <summary>True if the app may already post events (Accessibility granted).</summary>
	public static bool CanPostEvents() => CGPreflightPostEventAccess();

	/// <summary>
	/// Asks the system to prompt the user for the Accessibility (Post Events)
	/// permission. Returns the current grant state; the prompt is handled by macOS.
	/// </summary>
	public static bool RequestPostEventsAccess() => CGRequestPostEventAccess();

	[DllImport(Constants.CoreGraphicsLibrary)]
	private static extern double CGEventSourceSecondsSinceLastEventType(int stateID, uint eventType);

	[DllImport(Constants.CoreGraphicsLibrary)]
	private static extern IntPtr CGEventCreate(IntPtr source);

	[DllImport(Constants.CoreGraphicsLibrary)]
	private static extern CGPoint CGEventGetLocation(IntPtr @event);

	[DllImport(Constants.CoreGraphicsLibrary)]
	private static extern IntPtr CGEventCreateMouseEvent(IntPtr source, uint mouseType, CGPoint mouseCursorPosition, uint mouseButton);

	[DllImport(Constants.CoreGraphicsLibrary)]
	private static extern void CGEventPost(uint tap, IntPtr @event);

	[DllImport(Constants.CoreGraphicsLibrary)]
	private static extern int CGWarpMouseCursorPosition(CGPoint newCursorPosition);

	[DllImport(Constants.CoreGraphicsLibrary)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool CGPreflightPostEventAccess();

	[DllImport(Constants.CoreGraphicsLibrary)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool CGRequestPostEventAccess();

	[DllImport(Constants.CoreFoundationLibrary)]
	private static extern void CFRelease(IntPtr cf);
}
