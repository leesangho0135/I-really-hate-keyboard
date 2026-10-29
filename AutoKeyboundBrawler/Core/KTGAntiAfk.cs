using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Runtime.InteropServices;

namespace AutoKeyboundBrawler.Core;

/// <summary>
/// Event-scoped inactivity protection. A short game-window key pulse complements
/// the local timer resets; zero-motion mouse events alone do not establish that
/// the client processed keyboard activity.
/// </summary>
public static unsafe class KTGAntiAfk
{
    private static readonly KTGActivityPulse ActivityPulse = new();
    private static DateTime _lastDiagLogTime = DateTime.MinValue;
    private static DateTime _lastWarningTime = DateTime.MinValue;
    private static IntPtr _cachedGameHwnd;

    internal static bool ShouldRun(Configuration config, EventPhase phase)
    {
        return config.Enabled && config.MinigameAutoplay && phase == EventPhase.InMinigame;
    }

    // Returns true while Ctrl is held, so the caller leaves the typing queue
    // untouched until the matching key-up has been accepted.
    public static bool KeepAlive(bool canStartPulse, bool diagnosticLogging)
    {
        ResetGameTimers();

        var wasHolding = ActivityPulse.IsHoldingKey;
        var isHolding = ActivityPulse.Tick(
            Environment.TickCount64,
            canStartPulse && (GetAsyncKeyState(LeftControlKey) & 0x8000) == 0,
            SendActivityMessage);

        if (!wasHolding && isHolding && diagnosticLogging)
        {
            Svc.Log.Information("[AntiAfk] Started event activity key pulse; typing waits for key release.");
        }

        return isHolding;
    }

    public static bool Reset()
    {
        if (!ActivityPulse.Reset(SendActivityMessage)) return false;
        _cachedGameHwnd = IntPtr.Zero;
        return true;
    }

    private static void ResetGameTimers()
    {
        try
        {
            var timerModule = InputTimerModule.Instance();
            if (timerModule == null)
            {
                LogWarning("[AntiAfk] InputTimerModule is unavailable; using the activity key pulse.");
                return;
            }

            if ((DateTime.UtcNow - _lastDiagLogTime).TotalSeconds >= 10)
            {
                _lastDiagLogTime = DateTime.UtcNow;
                Svc.Log.Verbose(
                    $"[AntiAfk] Timers before reset: Content={timerModule->ContentInputTimer:F2} " +
                    $"Afk={timerModule->AfkTimer:F2} Input={timerModule->InputTimer:F2}");
            }

            timerModule->ContentInputTimer = 0f;
            timerModule->AfkTimer = 0f;
            timerModule->InputTimer = 0f;
            ResetInputTimerDataSpan(timerModule->ControllerInputTimers);
            ResetInputTimerDataSpan(timerModule->MouseInputTimers);
        }
        catch (Exception ex)
        {
            LogWarning($"[AntiAfk] Input timer reset failed: {ex.Message}");
        }
    }

    private static void ResetInputTimerDataSpan(Span<InputTimerModule.InputTimerData> span)
    {
        for (var i = 0; i < span.Length; i++) span[i].Timer = 0f;
    }

    private static bool SendActivityMessage(bool keyDown)
    {
        var window = GetGameWindowHandle();
        if (window == IntPtr.Zero)
        {
            LogWarning("[AntiAfk] Game window not found; activity pulse will retry.");
            return false;
        }

        // Target this game's message queue, including in the background.
        // The OS cursor and other applications receive no input from this path.
        var keyFlags = keyDown ? 0x001D0001u : 0xC01D0001u;
        if (PostMessage(window, keyDown ? WmKeyDown : WmKeyUp,
                (IntPtr)LeftControlKey, unchecked((IntPtr)(int)keyFlags)))
        {
            return true;
        }

        LogWarning($"[AntiAfk] Activity key message failed (Win32 {Marshal.GetLastWin32Error()}); will retry.");
        return false;
    }

    private static IntPtr GetGameWindowHandle()
    {
        if (IsCurrentGameWindow(_cachedGameHwnd)) return _cachedGameHwnd;

        _cachedGameHwnd = IntPtr.Zero;
        var candidate = IntPtr.Zero;
        while ((candidate = FindWindowEx(IntPtr.Zero, candidate, "FFXIVGAME", null)) != IntPtr.Zero)
        {
            if (!IsCurrentGameWindow(candidate)) continue;
            _cachedGameHwnd = candidate;
            break;
        }

        return _cachedGameHwnd;
    }

    private static bool IsCurrentGameWindow(IntPtr window)
    {
        if (window == IntPtr.Zero || !IsWindow(window)) return false;
        GetWindowThreadProcessId(window, out var processId);
        return processId == Environment.ProcessId;
    }

    private static void LogWarning(string message)
    {
        if ((DateTime.UtcNow - _lastWarningTime).TotalSeconds < 30) return;
        _lastWarningTime = DateTime.UtcNow;
        Svc.Log.Warning(message);
    }

    private const int LeftControlKey = 0xA2;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr key, IntPtr flags);
}
