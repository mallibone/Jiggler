using System.Timers;
using CoreGraphics;
using MouseJiggler.Interop;
using Timer = System.Timers.Timer;

namespace MouseJiggler.Services;

/// <summary>How the cursor is being moved.</summary>
public enum JiggleMethod
{
	/// <summary>Synthetic mouse-move event — resets the idle timer. Needs Accessibility permission.</summary>
	SyntheticEvent,

	/// <summary>Cursor warp — no permission needed, sandbox-clean fallback.</summary>
	CursorWarp,
}

/// <summary>Snapshot of the jiggler state, raised on every check.</summary>
public readonly record struct JiggleStatus(
	bool IsRunning,
	double IdleSeconds,
	double IdleThreshold,
	JiggleMethod Method,
	bool PermissionGranted,
	bool JustJiggled);

/// <summary>
/// Port of mouse_jiggle.py: once the system has been idle past a threshold, nudge
/// the cursor by ±1px (alternating direction) to keep the machine "active".
///
/// Default behaviour uses a synthetic mouse event (resets the idle timer); when the
/// Accessibility permission has not been granted it falls back to a cursor warp and
/// automatically upgrades to synthetic events once the user grants permission.
/// </summary>
public sealed class MouseJiggleService : IDisposable
{
	private const int JigglePixels = 1;
	private const double CheckIntervalSeconds = 1.0;

	private readonly Timer _timer;
	private int _direction = 1; // alternates between +1 and -1, like the Python script

	public MouseJiggleService()
	{
		_timer = new Timer(CheckIntervalSeconds * 1000) { AutoReset = true };
		_timer.Elapsed += OnTick;
	}

	/// <summary>Idle threshold in seconds before the first nudge (default 30).</summary>
	public double IdleThresholdSeconds { get; set; } = 30.0;

	public bool IsRunning { get; private set; }

	public JiggleMethod ActiveMethod { get; private set; } = JiggleMethod.CursorWarp;

	public bool PermissionGranted { get; private set; }

	/// <summary>Raised on every check (≈1/s) with the current state. May fire off the UI thread.</summary>
	public event Action<JiggleStatus>? StatusChanged;

	public void Start()
	{
		if (IsRunning)
			return;

		ResolveMethod(requestIfMissing: true);
		IsRunning = true;
		_timer.Start();
		RaiseStatus(idle: CoreGraphicsNative.GetIdleSeconds(), justJiggled: false);
	}

	public void Stop()
	{
		if (!IsRunning)
			return;

		_timer.Stop();
		IsRunning = false;
		RaiseStatus(idle: CoreGraphicsNative.GetIdleSeconds(), justJiggled: false);
	}

	private void OnTick(object? sender, ElapsedEventArgs e)
	{
		// If we're warping only because permission was missing, re-check: the user
		// may have granted it via the system prompt since we started.
		if (ActiveMethod == JiggleMethod.CursorWarp)
			ResolveMethod(requestIfMissing: false);

		var idle = CoreGraphicsNative.GetIdleSeconds();
		var jiggled = false;

		if (idle >= IdleThresholdSeconds)
		{
			Jiggle();
			jiggled = true;
		}

		RaiseStatus(idle, jiggled);
	}

	private void Jiggle()
	{
		var pos = CoreGraphicsNative.GetCursorPosition();
		var target = new CGPoint(pos.X + (_direction * JigglePixels), pos.Y);

		if (ActiveMethod == JiggleMethod.SyntheticEvent)
			CoreGraphicsNative.PostMouseMove(target);
		else
			CoreGraphicsNative.WarpCursor(target);

		_direction = -_direction; // flip for next time
	}

	private void ResolveMethod(bool requestIfMissing)
	{
		PermissionGranted = CoreGraphicsNative.CanPostEvents();

		if (PermissionGranted)
		{
			ActiveMethod = JiggleMethod.SyntheticEvent;
			return;
		}

		ActiveMethod = JiggleMethod.CursorWarp;
		if (requestIfMissing)
			CoreGraphicsNative.RequestPostEventsAccess(); // macOS shows the prompt
	}

	private void RaiseStatus(double idle, bool justJiggled)
		=> StatusChanged?.Invoke(new JiggleStatus(
			IsRunning, idle, IdleThresholdSeconds, ActiveMethod, PermissionGranted, justJiggled));

	public void Dispose()
	{
		_timer.Elapsed -= OnTick;
		_timer.Dispose();
	}
}
