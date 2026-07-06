using System.Runtime.InteropServices;
using MouseJiggler.Services;

namespace MouseJiggler.Interop;

/// <summary>
/// Windows <see cref="IKeepAwake"/> backed by <c>SetThreadExecutionState</c>. The synthetic
/// input already resets the idle timer on every nudge, but with a large idle threshold the
/// display could legitimately sleep before the first jiggle — this holds it awake for the
/// whole run.
///
/// The <c>ES_CONTINUOUS</c> state is per-thread: Begin and End must run on the same thread.
/// That holds today because both are driven by the Start/Stop button on the UI dispatcher.
/// </summary>
internal sealed class WindowsKeepAwake : IKeepAwake
{
	private const uint EsContinuous = 0x80000000;     // ES_CONTINUOUS
	private const uint EsSystemRequired = 0x00000001; // ES_SYSTEM_REQUIRED
	private const uint EsDisplayRequired = 0x00000002; // ES_DISPLAY_REQUIRED

	public void Begin()
		=> SetThreadExecutionState(EsContinuous | EsSystemRequired | EsDisplayRequired);

	public void End()
		=> SetThreadExecutionState(EsContinuous);

	[DllImport("kernel32.dll")]
	private static extern uint SetThreadExecutionState(uint esFlags);
}
