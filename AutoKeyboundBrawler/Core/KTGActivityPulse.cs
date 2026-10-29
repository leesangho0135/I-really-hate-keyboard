using System;

namespace AutoKeyboundBrawler.Core;

internal sealed class KTGActivityPulse
{
    private const long IntervalMs = 15_000;
    private const long HoldMs = 200;
    private const long RetryMs = 1_000;
    private long _nextPulseAt;
    private long _releaseAt;

    public bool IsHoldingKey { get; private set; }

    // The sender returns whether the game-window message was accepted.
    // Explicit time and sender keep this sequence testable without game input.
    public bool Tick(long now, bool canStart, Func<bool, bool> send)
    {
        if (IsHoldingKey)
        {
            if (now < _releaseAt) return true;
            if (!send(false))
            {
                _releaseAt = now + RetryMs;
                return true;
            }

            IsHoldingKey = false;
            _nextPulseAt = now + IntervalMs;
            return false;
        }

        if (!canStart || now < _nextPulseAt) return false;
        if (!send(true))
        {
            _nextPulseAt = now + RetryMs;
            return false;
        }

        IsHoldingKey = true;
        _releaseAt = now + HoldMs;
        return true;
    }

    public bool Reset(Func<bool, bool> send)
    {
        // Keep a failed release pending so the next update can retry it.
        if (IsHoldingKey && !send(false)) return false;
        IsHoldingKey = false;
        _nextPulseAt = 0;
        _releaseAt = 0;
        return true;
    }
}
