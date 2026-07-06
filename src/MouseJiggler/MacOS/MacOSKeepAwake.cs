using Foundation;
using MouseJiggler.Services;

namespace MouseJiggler.Interop;

/// <summary>
/// macOS <see cref="IKeepAwake"/> backed by <see cref="NSProcessInfo.BeginActivity"/>.
///
/// <c>UserInitiated</c> opts the process out of App Nap (and implies
/// <c>IdleSystemSleepDisabled</c>) — without it the jiggle timer is throttled whenever the
/// app is not frontmost, so the idle counter climbs and the screen locks anyway.
/// <c>IdleDisplaySleepDisabled</c> additionally keeps the display awake between nudges.
/// Sandbox-safe, no entitlement needed; the assertion (and its reason string) is visible in
/// <c>pmset -g assertions</c> and is released automatically if the process exits.
/// </summary>
internal sealed class MacOSKeepAwake : IKeepAwake
{
	private NSObject? _activity;

	public void Begin() =>
		_activity ??= NSProcessInfo.ProcessInfo.BeginActivity(
			NSActivityOptions.UserInitiated | NSActivityOptions.IdleDisplaySleepDisabled,
			"Mouse Jiggler is actively keeping this Mac awake");

	public void End()
	{
		if (_activity is null)
			return;

		NSProcessInfo.ProcessInfo.EndActivity(_activity);
		_activity.Dispose();
		_activity = null;
	}
}
