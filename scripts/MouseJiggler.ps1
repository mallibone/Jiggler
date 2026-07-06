<#
.SYNOPSIS
    Keeps the machine "active" without any binary — a script version of the Mouse Jiggler app.

.DESCRIPTION
    For machines where the unsigned MouseJiggler.exe is blocked by SmartScreen/AV policy.
    Mirrors the app's core loop: once the system has been idle past a threshold, nudge the
    cursor by 1 px (alternating direction) via SendInput, which resets the system idle timer
    so the machine (and Teams/Slack presence) registers as active. While running it also
    holds a SetThreadExecutionState keep-awake so the display cannot sleep between nudges.

    Stop with Ctrl+C — the keep-awake state is always released on exit.

    Requires Windows PowerShell in FullLanguage mode (Add-Type compiles a small P/Invoke
    shim). Under Constrained Language Mode (WDAC/AppLocker lockdown) the script cannot run
    and says so up front.

.PARAMETER IdleThresholdSeconds
    Seconds of inactivity before the first nudge (default 30).

.PARAMETER CheckIntervalSeconds
    Seconds between idle checks (default 1).

.EXAMPLE
    .\MouseJiggler.ps1
.EXAMPLE
    .\MouseJiggler.ps1 -IdleThresholdSeconds 60
#>
[CmdletBinding()]
param(
    [ValidateRange(5, 3600)]
    [double]$IdleThresholdSeconds = 30,

    [ValidateRange(1, 60)]
    [double]$CheckIntervalSeconds = 1
)

$mode = $ExecutionContext.SessionState.LanguageMode
if ($mode -ne 'FullLanguage') {
    Write-Error ("This script needs PowerShell FullLanguage mode, but this session runs in '$mode' " +
        "(Add-Type is blocked by your organization's lockdown policy). The script cannot work here.")
    exit 1
}

# Same P/Invoke surface as the app's Platforms/Windows/WindowsInput.cs + WindowsKeepAwake.cs.
Add-Type -Namespace MouseJiggler -Name Native -MemberDefinition @'
[StructLayout(LayoutKind.Sequential)]
public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }

[StructLayout(LayoutKind.Sequential)]
public struct INPUT { public uint type; public MOUSEINPUT mi; }

[DllImport("user32.dll")]
public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

[DllImport("user32.dll", SetLastError = true)]
public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

[DllImport("kernel32.dll")]
public static extern uint SetThreadExecutionState(uint esFlags);

public const uint MOUSEEVENTF_MOVE    = 0x0001;
public const uint ES_CONTINUOUS       = 0x80000000;
public const uint ES_SYSTEM_REQUIRED  = 0x00000001;
public const uint ES_DISPLAY_REQUIRED = 0x00000002;

// dwTime is a GetTickCount() stamp (32-bit, wraps ~49 days). Unsigned subtraction
// against the current tick count yields the correct elapsed span across a wrap.
public static double GetIdleSeconds()
{
    var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
    if (!GetLastInputInfo(ref info)) return 0;
    unchecked { return ((uint)Environment.TickCount - info.dwTime) / 1000.0; }
}

// Posts a synthetic 1 px relative mouse move. This registers as real user input and
// resets the system idle timer (Teams/Slack presence stays "active").
public static void Jiggle(int direction)
{
    var input = new INPUT { type = 0, mi = new MOUSEINPUT { dx = direction, dy = 0, dwFlags = MOUSEEVENTF_MOVE } };
    SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
}
'@

$direction = 1
$jiggleCount = 0

Write-Host "Mouse Jiggler (script edition)"
Write-Host "Idle threshold: $IdleThresholdSeconds s, check every $CheckIntervalSeconds s. Ctrl+C to stop."
Write-Host ""

try {
    # Belt and braces: hold a display/system keep-awake for the whole run so the display
    # cannot sleep before the first nudge when the threshold is long.
    [void][MouseJiggler.Native]::SetThreadExecutionState(
        [MouseJiggler.Native]::ES_CONTINUOUS -bor
        [MouseJiggler.Native]::ES_SYSTEM_REQUIRED -bor
        [MouseJiggler.Native]::ES_DISPLAY_REQUIRED)

    while ($true) {
        $idle = [MouseJiggler.Native]::GetIdleSeconds()

        if ($idle -ge $IdleThresholdSeconds) {
            [MouseJiggler.Native]::Jiggle($direction)
            $direction = -$direction
            $jiggleCount++
        }

        $status = "Idle: {0,6:N1}s / {1:N0}s   Jiggles: {2}" -f $idle, $IdleThresholdSeconds, $jiggleCount
        Write-Host ("`r" + $status.PadRight(60)) -NoNewline

        Start-Sleep -Seconds $CheckIntervalSeconds
    }
}
finally {
    # Runs on Ctrl+C too — always release the keep-awake state.
    [void][MouseJiggler.Native]::SetThreadExecutionState([MouseJiggler.Native]::ES_CONTINUOUS)
    Write-Host ""
    Write-Host "Stopped. Keep-awake released after $jiggleCount jiggle(s)."
}
