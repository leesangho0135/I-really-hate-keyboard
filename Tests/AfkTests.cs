using System.Reflection;
using AutoKeyboundBrawler;
using AutoKeyboundBrawler.Core;

internal static class AfkTests
{
    internal static void Scope()
    {
        var shouldRun = typeof(KTGAntiAfk).GetMethod("ShouldRun", BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var phase in Enum.GetValues<EventPhase>())
        foreach (var enabled in new[] { false, true })
        foreach (var autoplay in new[] { false, true })
        foreach (var autoLoop in new[] { false, true })
        {
            var config = new Configuration
            {
                Enabled = enabled,
                MinigameAutoplay = autoplay,
                AutoLoopEnabled = autoLoop
            };
            var actual = (bool)shouldRun.Invoke(null, [config, phase])!;
            var expected = enabled && autoplay && phase == EventPhase.InMinigame;
            Require(actual == expected,
                $"Unexpected AFK scope for {phase}, enabled={enabled}, autoplay={autoplay}, loop={autoLoop}.");
        }
    }

    internal static void Schedule()
    {
        var pulse = new Pulse();
        var sent = new List<bool>();
        bool Send(bool down) { sent.Add(down); return true; }

        Require(!pulse.Tick(10_000, false, Send), "Busy automation must postpone the first activity pulse.");
        Events(sent, []);
        Require(pulse.Tick(10_001, true, Send) && pulse.IsHolding, "First available update must send key-down.");
        Events(sent, [true]);
        Require(pulse.Tick(10_200, false, Send), "The key must remain held until 200 ms have elapsed.");
        Events(sent, [true]);
        Require(!pulse.Tick(10_201, false, Send) && !pulse.IsHolding,
            "Key-up must run at the deadline even when a new pulse is not allowed.");
        Events(sent, [true, false]);

        Require(!pulse.Tick(25_200, true, Send), "The next pulse must wait 15 seconds after successful release.");
        Require(!pulse.Tick(25_201, false, Send), "Busy automation must postpone an otherwise due pulse.");
        Events(sent, [true, false]);
        Require(pulse.Tick(25_333, true, Send), "A postponed pulse must start when automation becomes available.");
        Events(sent, [true, false, true]);
    }

    internal static void Retry()
    {
        var pulse = new Pulse();
        var sent = new List<bool>();
        var acceptDown = false;
        var acceptUp = false;
        bool Send(bool down) { sent.Add(down); return down ? acceptDown : acceptUp; }

        Require(!pulse.Tick(0, true, Send) && !pulse.IsHolding, "Failed key-down must not enter the held state.");
        Events(sent, [true]);
        acceptDown = true;
        Require(!pulse.Tick(999, true, Send), "Failed key-down must be throttled before the retry deadline.");
        Events(sent, [true]);
        Require(pulse.Tick(1_000, true, Send), "Failed key-down must retry after one second.");
        Require(pulse.Tick(1_199, false, Send), "A retry success must still observe the full hold duration.");
        Events(sent, [true, true]);
        Require(pulse.Tick(1_200, false, Send) && pulse.IsHolding, "Failed key-up must retain the held state.");
        Events(sent, [true, true, false]);

        acceptUp = true;
        Require(pulse.Tick(2_199, false, Send), "Failed key-up must not be retried every frame.");
        Events(sent, [true, true, false]);
        Require(!pulse.Tick(2_200, false, Send) && !pulse.IsHolding,
            "Key-up retry must succeed even while new pulses are blocked.");
        Events(sent, [true, true, false, false]);
        Require(!pulse.Tick(17_199, true, Send), "The next interval must begin at successful key-up.");
        Require(pulse.Tick(17_200, true, Send), "Successful release must schedule the next pulse at 15 seconds.");
        Events(sent, [true, true, false, false, true]);
    }

    internal static void Reset()
    {
        var pulse = new Pulse();
        var sent = new List<bool>();
        bool Send(bool down) { sent.Add(down); return true; }
        Require(pulse.Reset(_ => throw new InvalidOperationException("Idle reset must not send a key.")),
            "Idle reset must succeed without any message.");

        Require(pulse.Tick(500, true, Send), "The activity key must be held before testing cleanup.");
        Require(!pulse.Reset(down => { sent.Add(down); return false; }) && pulse.IsHolding,
            "A failed cleanup release must remain pending.");
        Events(sent, [true, false]);
        Require(pulse.Reset(Send) && !pulse.IsHolding, "Cleanup must release a held key immediately.");
        Events(sent, [true, false, false]);
        Require(pulse.Reset(_ => throw new InvalidOperationException("Repeated idle reset must not send a key.")),
            "Repeated reset must be a no-op after successful cleanup.");
        Require(pulse.Tick(1, true, Send), "Successful cleanup must clear the previous scheduling clocks.");
        Events(sent, [true, false, false, true]);

        var completed = new Pulse();
        Require(completed.Tick(0, true, _ => true), "Pulse setup must send key-down.");
        Require(!completed.Tick(200, false, _ => true), "Pulse setup must finish key-up.");
        Require(completed.Reset(_ => throw new InvalidOperationException("Released pulse must not send another key-up.")),
            "Reset must clear an idle interval without another release.");
        Require(completed.Tick(201, true, _ => true), "Reset must clear a completed pulse's future interval.");
    }

    internal static void TypingQueue()
    {
        var simulator = new KTGInputSimulator();
        simulator.EnqueueWord("한국 A1", new Configuration());
        var before = PendingActions(simulator);
        var word = simulator.CurrentWord;
        var pulse = new Pulse();
        var sent = new List<bool>();
        bool Send(bool down) { sent.Add(down); return true; }

        Require(pulse.Tick(0, true, Send), "Fake activity pulse must start.");
        Require(pulse.Tick(199, false, Send), "Fake activity pulse must remain held.");
        Require(!pulse.Tick(200, false, Send), "Fake activity pulse must release while typing is busy.");
        Require(pulse.Reset(Send), "Completed pulse cleanup must succeed.");

        Require(before.SequenceEqual(PendingActions(simulator)), "Activity must not consume or change queued characters.");
        Require(simulator.CurrentWord == word && simulator.RemainingChars == before.Length && simulator.IsTyping,
            "Activity must not cancel or replace the word being typed.");
        Require(!simulator.JustSubmitted, "Activity must not report a word submission.");
        Events(sent, [true, false]);
    }

    private static KTGInputSimulator.KeyAction[] PendingActions(KTGInputSimulator simulator)
    {
        var queue = typeof(KTGInputSimulator).GetField("_pendingActions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((IEnumerable<KTGInputSimulator.KeyAction>)queue.GetValue(simulator)!).ToArray();
    }

    private static void Events(List<bool> actual, bool[] expected)
    {
        Require(actual.SequenceEqual(expected), "Unexpected activity message sequence.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // Exercises the real internal scheduler with explicit time and a fake sender.
    // Never calls KeepAlive, InputTimerModule.Instance, or an OS input API.
    private sealed class Pulse
    {
        private static readonly Type PulseType = typeof(KTGAutomationManager).Assembly
            .GetType("AutoKeyboundBrawler.Core.KTGActivityPulse", throwOnError: true)!;
        private static readonly MethodInfo TickMethod = PulseType.GetMethod("Tick")!;
        private static readonly MethodInfo ResetMethod = PulseType.GetMethod("Reset")!;
        private static readonly PropertyInfo HoldingProperty = PulseType.GetProperty("IsHoldingKey")!;
        private readonly object _instance = Activator.CreateInstance(PulseType, nonPublic: true)!;

        internal bool IsHolding => (bool)HoldingProperty.GetValue(_instance)!;
        internal bool Tick(long now, bool canStart, Func<bool, bool> send) =>
            (bool)TickMethod.Invoke(_instance, [now, canStart, send])!;
        internal bool Reset(Func<bool, bool> send) => (bool)ResetMethod.Invoke(_instance, [send])!;
    }
}
