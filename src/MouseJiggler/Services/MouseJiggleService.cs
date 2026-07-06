using System.Timers;
using Timer = System.Timers.Timer;

namespace MouseJiggler.Services;

/// <summary>How the cursor is being moved, as reported to the UI.</summary>
public enum JiggleMethod
{
	/// <summary>Running, but we haven't confirmed yet whether synthetic events work.</summary>
	Trying,

	/// <summary>Synthetic mouse events confirmed working — resets the idle timer.</summary>
	SyntheticActive,

	/// <summary>Synthetic events are blocked; falling back to cursor warp (no idle reset).</summary>
	WarpFallback,
}

/// <summary>Snapshot of the jiggler state, raised on every check.</summary>
public readonly record struct JiggleStatus(
	bool IsRunning,
	double IdleSeconds,
	double IdleThreshold,
	JiggleMethod Method,
	long JiggleCount,
	DateTimeOffset? LastJiggleAt,
	bool JustJiggled);

/// <summary>
/// Port of mouse_jiggle.py: once the system has been idle past a threshold, nudge the
/// cursor by ±1px (alternating direction) to keep the machine "active".
///
/// Method is resolved <b>empirically</b>: we always try a synthetic mouse event first
/// and look at whether the idle timer actually resets. If it does, synthetic events
/// work (and reset the idle timer, keeping the machine "active"); if posting is blocked
/// (permission not granted / not yet in effect) we fall back to a cursor warp so the
/// cursor still moves. We deliberately do NOT gate on a preflight permission check,
/// which (on macOS) caches per-process and would get stuck reporting "denied" after a grant.
///
/// All OS interaction goes through <see cref="IMouseInput"/> so the logic is identical on
/// every platform; only the injected implementation differs (CoreGraphics / user32).
/// </summary>
public sealed class MouseJiggleService : IDisposable
{
	private const int JigglePixels = 1;
	private const double CheckIntervalSeconds = 1.0;

	// A jiggle only happens when idle >= threshold, so idle is "high" at that moment.
	// After a working synthetic post the idle timer collapses toward 0; we treat a drop
	// of more than this many seconds as proof the event registered.
	private const double IdleResetProofSeconds = 2.0;

	private readonly IMouseInput _input;
	private readonly IKeepAwake _keepAwake;
	private readonly Timer _timer;
	private int _direction = 1; // alternates between +1 and -1, like the Python script
	private bool? _syntheticWorks; // null = unknown/untested yet
	private bool _proofPending;    // a synthetic post is awaiting an idle-reset check
	private double _proofBaseline; // idle seconds at the moment of that post

	public MouseJiggleService(IMouseInput input, IKeepAwake keepAwake)
	{
		_input = input;
		_keepAwake = keepAwake;
		_timer = new Timer(CheckIntervalSeconds * 1000) { AutoReset = true };
		_timer.Elapsed += OnTick;
	}

	/// <summary>Idle threshold in seconds before the first nudge (default 30).</summary>
	public double IdleThresholdSeconds { get; set; } = 30.0;

	public bool IsRunning { get; private set; }

	public long JiggleCount { get; private set; }

	public DateTimeOffset? LastJiggleAt { get; private set; }

	public JiggleMethod Method =>
		!IsRunning ? JiggleMethod.Trying
		: _syntheticWorks switch
		{
			true => JiggleMethod.SyntheticActive,
			false => JiggleMethod.WarpFallback,
			null => JiggleMethod.Trying,
		};

	/// <summary>Raised on every check (≈1/s) and on every jiggle. May fire off the UI thread.</summary>
	public event Action<JiggleStatus>? StatusChanged;

	public void Start()
	{
		if (IsRunning)
			return;

		// Read the grant state once as a hint and surface the system prompt if needed.
		// We do NOT use this to choose the method — detection is empirical (see Jiggle).
		if (!_input.CanPostEvents())
			_input.RequestPostEventsAccess();

		IsRunning = true;
		_keepAwake.Begin();
		_timer.Start();
		RaiseStatus(idle: _input.GetIdleSeconds(), justJiggled: false);
	}

	public void Stop()
	{
		if (!IsRunning)
			return;

		_timer.Stop();
		_keepAwake.End();
		IsRunning = false;
		RaiseStatus(idle: _input.GetIdleSeconds(), justJiggled: false);
	}

	/// <summary>Perform one jiggle immediately, regardless of idle time (manual "Test").</summary>
	public void JiggleNow()
	{
		Jiggle();
		RaiseStatus(idle: _input.GetIdleSeconds(), justJiggled: true);
	}

	private void OnTick(object? sender, ElapsedEventArgs e)
	{
		var idle = _input.GetIdleSeconds();

		// Resolve a synthetic post from a previous tick. We check on a later tick (not
		// immediately after posting) so the HID idle counter has time to reflect the
		// event — reading it in the same instant gives false negatives.
		if (_proofPending)
		{
			_syntheticWorks = idle <= _proofBaseline - IdleResetProofSeconds;
			_proofPending = false;
		}

		var jiggled = false;
		if (idle >= IdleThresholdSeconds)
		{
			Jiggle(idleBefore: idle);
			jiggled = true;
		}

		RaiseStatus(_input.GetIdleSeconds(), jiggled);
	}

	/// <param name="idleBefore">
	/// Idle seconds measured just before the nudge. When this is high enough, a working
	/// synthetic post will visibly reset it — which the next tick uses as proof.
	/// </param>
	private void Jiggle(double idleBefore = 0)
	{
		var pos = _input.GetCursorPosition();
		var target = new MousePoint(pos.X + (_direction * JigglePixels), pos.Y);

		// Prefer synthetic events unless we've proven they're blocked.
		if (_syntheticWorks != false)
		{
			_input.PostMouseMove(target);

			// Arm an idle-reset proof, but only when we were genuinely idle (so a reset
			// is meaningful) and haven't concluded yet.
			if (_syntheticWorks is null && idleBefore >= IdleResetProofSeconds)
			{
				_proofPending = true;
				_proofBaseline = idleBefore;
			}
		}

		// If synthetic is known-blocked, warp so the cursor still moves.
		if (_syntheticWorks == false)
			_input.WarpCursor(target);

		_direction = -_direction; // flip for next time
		JiggleCount++;
		LastJiggleAt = DateTimeOffset.Now;
	}

	private void RaiseStatus(double idle, bool justJiggled)
		=> StatusChanged?.Invoke(new JiggleStatus(
			IsRunning, idle, IdleThresholdSeconds, Method, JiggleCount, LastJiggleAt, justJiggled));

	public void Dispose()
	{
		_timer.Elapsed -= OnTick;
		_timer.Dispose();
		_keepAwake.End();
	}
}
