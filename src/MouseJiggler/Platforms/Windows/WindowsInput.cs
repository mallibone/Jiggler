using System.Runtime.InteropServices;
using MouseJiggler.Services;

namespace MouseJiggler.Interop;

/// <summary>
/// Windows <see cref="IMouseInput"/> backed by the user32 input APIs. Reads system idle time
/// (<c>GetLastInputInfo</c>) and nudges the cursor with a synthetic move (<c>SendInput</c>),
/// which resets the idle timer so the machine registers as "active".
///
/// Windows has no TCC/Accessibility gate for posting input, so synthetic events always work
/// for a normal desktop app — <see cref="CanPostEvents"/> returns <c>true</c> and the
/// cursor-warp fallback is effectively unused. (UIPI only blocks injecting input *into*
/// elevated windows, which is irrelevant to the global idle timer.)
/// </summary>
internal sealed class WindowsInput : IMouseInput
{
	private const uint InputMouse = 0;          // INPUT_MOUSE
	private const uint MouseEventfMove = 0x0001; // MOUSEEVENTF_MOVE (relative)

	/// <summary>Seconds since the last input event of any kind — the system idle time.</summary>
	public double GetIdleSeconds()
	{
		var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
		if (!GetLastInputInfo(ref info))
			return 0;

		// dwTime is a GetTickCount() stamp (32-bit, wraps ~49 days). Unsigned subtraction
		// against the current tick count yields the correct elapsed span across a wrap.
		uint idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
		return idleMs / 1000.0;
	}

	/// <summary>Current cursor location in screen coordinates.</summary>
	public MousePoint GetCursorPosition()
		=> GetCursorPos(out var p) ? new MousePoint(p.X, p.Y) : new MousePoint(0, 0);

	/// <summary>
	/// Posts a synthetic relative mouse move toward <paramref name="target"/>. This registers
	/// as user input and resets the idle timer. Sent as a relative delta from the current
	/// position so it works regardless of multi-monitor coordinate origins.
	/// </summary>
	public void PostMouseMove(MousePoint target)
	{
		var cur = GetCursorPosition();
		var input = new INPUT
		{
			type = InputMouse,
			mi = new MOUSEINPUT
			{
				dx = (int)Math.Round(target.X - cur.X),
				dy = (int)Math.Round(target.Y - cur.Y),
				dwFlags = MouseEventfMove,
			},
		};

		// A zero-delta move still resets the idle timer, but nudge by 1px so the cursor
		// visibly moves too (matching the macOS behaviour) when target == current.
		if (input.mi.dx == 0 && input.mi.dy == 0)
			input.mi.dx = 1;

		SendInput(1, [input], Marshal.SizeOf<INPUT>());
	}

	/// <summary>
	/// Moves the cursor without injecting an input event. Used only if synthetic events are
	/// ever found not to reset the idle timer (does not happen on a normal Windows desktop).
	/// </summary>
	public void WarpCursor(MousePoint target)
		=> SetCursorPos((int)Math.Round(target.X), (int)Math.Round(target.Y));

	/// <summary>No permission gate for posting input on Windows.</summary>
	public bool CanPostEvents() => true;

	/// <summary>No-op; Windows requires no permission prompt to post input.</summary>
	public bool RequestPostEventsAccess() => true;

	[StructLayout(LayoutKind.Sequential)]
	private struct LASTINPUTINFO
	{
		public uint cbSize;
		public uint dwTime;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MOUSEINPUT
	{
		public int dx;
		public int dy;
		public uint mouseData;
		public uint dwFlags;
		public uint time;
		public nuint dwExtraInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct INPUT
	{
		public uint type;
		public MOUSEINPUT mi;
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
