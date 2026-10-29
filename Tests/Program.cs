using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
using System.Text;
using AutoKeyboundBrawler;
using AutoKeyboundBrawler.Core;
using AutoKeyboundBrawler.Data;
using AutoKeyboundBrawler.UI;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Excel.Exceptions;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var dalamudPath = Environment.GetEnvironmentVariable("DALAMUD_HOME") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "addon", "Hooks", "dev");
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            var file = Path.Combine(dalamudPath, name.Name + ".dll");
            return File.Exists(file) ? context.LoadFromAssemblyPath(file) : null;
        };

        var group = "all";
        string? gameDataPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--game-data" && i + 1 < args.Length)
                gameDataPath = args[++i];
            else if (i == 0 && !args[i].StartsWith('-'))
                group = args[i];
            else
            {
                Console.Error.WriteLine("Usage: AutoKeyboundBrawler.Tests [all|core|regression|boundary|afk] [--game-data <game/sqpack path>]");
                return 2;
            }
        }
        return RegressionTests.Run(group, gameDataPath);
    }
}

internal static class RegressionTests
{
    private static readonly string[] LocalizationKeys =
    [
        "TypingSolver", "TypingSolverSub", "TypingSolverTip", "AfkLoop", "AfkLoopSub", "AfkLoopTip",
        "AutoOpenReg", "AutoOpenRegSub", "AutoOpenRegTip", "IgnorePunct", "IgnorePunctSub", "IgnorePunctTip",
        "Speed", "MinCharDelay", "MaxCharDelay", "SubDelay", "Standby", "BadgeHalt", "BadgeTyping",
        "BadgeCombat", "BadgeLoop", "BadgeIdle", "Close", "Compact", "Expand", "LangTooltip", "LangAuto"
    ];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(string group, string? gameDataPath)
    {
        var tests = new List<(string Group, string Name, System.Action Body)>
        {
            ("core", "Language settings survive legacy and Korean JSON", SettingsRoundTrip),
            ("core", "All settings labels have Korean translations", KoreanLabels),
            ("core", "Korean and decomposed Hangul queue complete Unicode text", KoreanInput),
            ("core", "Korean word lookup normalizes exact and punctuation matches", KoreanLookup),
            ("core", "Korean game text preserves stop and priority detection", KoreanRegistry),
            ("regression", "Existing UI language values and labels are preserved", ExistingLanguages),
            ("regression", "Existing ASCII German French and Japanese queues are preserved", ExistingInput),
            ("boundary", "Korean-client Latin text stays independent of Hangul IME", KoreanMixedInput),
            ("boundary", "Punctuation option and cancellation keep their contract", InputBoundaries),
            ("boundary", "Unknown localization keys and obscured words keep their contract", LookupBoundaries),
            ("afk", "Activity protection is limited to enabled minigame automation", AfkTests.Scope),
            ("afk", "Activity pulses respect hold interval and busy boundaries", AfkTests.Schedule),
            ("afk", "Failed activity down and up messages retry without losing state", AfkTests.Retry),
            ("afk", "Reset releases held keys and preserves failed release state", AfkTests.Reset),
            ("afk", "Activity pulses preserve the pending Korean typing queue", AfkTests.TypingQueue)
        };
        // Keep installed-data integration last: it replaces the database with
        // the real static sheet after the synthetic fixtures have been restored.
        if (gameDataPath != null)
            tests.Add(("core", "Installed Korean-only data loads without English or repeated reloads", () => KoreanDataIntegration(gameDataPath)));
        else if (group is "all" or "core")
            Console.WriteLine("SKIP [core] Installed Korean static-data integration (no --game-data path supplied).");
        var selected = tests.Where(t => group == "all" || t.Group == group).ToArray();
        if (selected.Length == 0)
        {
            Console.Error.WriteLine("Choose all, core, regression, boundary, or afk.");
            return 2;
        }

        var failed = 0;
        foreach (var test in selected)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS [{test.Group}] {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"FAIL [{test.Group}] {test.Name}: {ex}");
            }
        }
        Console.WriteLine($"{selected.Length - failed}/{selected.Length} passed. No game input was sent.");
        return failed == 0 ? 0 : 1;
    }

    private static void SettingsRoundTrip()
    {
        foreach (var (value, language) in new[]
        {
            (0, UiLanguage.Japanese), (1, UiLanguage.English), (2, UiLanguage.German),
            (3, UiLanguage.French), (4, UiLanguage.Korean)
        })
        {
            var config = JsonConvert.DeserializeObject<Configuration>($"{{\"SelectedLanguage\":{value}}}")!;
            Equal<UiLanguage?>(language, config.SelectedLanguage);
            Equal(value, JObject.Parse(JsonConvert.SerializeObject(config))["SelectedLanguage"]!.Value<int>());
            var named = JsonConvert.DeserializeObject<Configuration>($"{{\"SelectedLanguage\":\"{language}\"}}")!;
            Equal<UiLanguage?>(language, named.SelectedLanguage);
        }
        Equal<UiLanguage?>(null, JsonConvert.DeserializeObject<Configuration>("{\"SelectedLanguage\":null}")!.SelectedLanguage);
        Equal<UiLanguage?>(null, JsonConvert.DeserializeObject<Configuration>("{}")!.SelectedLanguage);
    }

    private static void KoreanLabels()
    {
        WithUiLanguage(UiLanguage.Korean, () =>
        {
            Equal("KO", Loc.GetLanguageShortName(Loc.CurrentLanguage));
            foreach (var key in LocalizationKeys)
                Require(Loc.Get(key).Any(c => c is >= '\uAC00' and <= '\uD7A3'), $"Missing Korean translation: {key}");
            Equal("닫기", Loc.Get("Close"));
            Equal("문장부호 생략", Loc.Get("IgnorePunct"));
        });
    }

    private static void KoreanInput()
    {
        WithWordLanguage("Korean", () =>
        {
            AssertUnicodeWord("한국 방패", "한국 방패");
            AssertUnicodeWord("한글".Normalize(NormalizationForm.FormD), "한글");
            AssertUnicodeWord("한국 A1 방패!", "한국 A1 방패");
        });
    }

    private static void KoreanLookup()
    {
        WithSyntheticWords(() =>
        {
            Equal(900001u, KTGWordDatabase.FindMatch("한국 방패!")?.RowId);
            Equal(900001u, KTGWordDatabase.FindMatch("한국 방패".Normalize(NormalizationForm.FormD))?.RowId);
            Equal(900002u, KTGWordDatabase.FindMatch("합성 별칭".Normalize(NormalizationForm.FormD))?.RowId);
            Require(KTGWordDatabase.FindMatch("없는 단어") == null, "Unrelated text must not match.");
        });
    }

    private static void KoreanRegistry()
    {
        Require(KTGLocalizationRegistry.IsRegistrar(0, "참가 접수원"), "Korean registrar name must be recognized without an ID.");
        foreach (var spell in new[] { "동작 감지 교란", "저주의 시선" })
            Require(KTGLocalizationRegistry.IsHaltSpell(spell), $"Korean safety stop not recognized: {spell}");
        Require(KTGLocalizationRegistry.IsNeedlesSpell("바늘 ?개"), "Korean Needles spell must be recognized.");
        foreach (var prompt in new[] { "사보텐더", "해피텐더", "침천개" })
            Require(KTGLocalizationRegistry.IsCactuarPrompt(prompt), $"Korean Cactuar prompt not recognized: {prompt}");
        Require(KTGLocalizationRegistry.MatchesInputsDisabled("효과가 부여되었습니다……."), "Korean input-disabled banner must be recognized.");
        Require(KTGLocalizationRegistry.MatchesInputsReEnabled("효과가 사라졌습니다!"), "Korean input-enabled banner must be recognized.");
        Require(!KTGLocalizationRegistry.MatchesInputsDisabled("효과가 사라졌습니다!"), "Input-enabled text must not stop input.");
        Require(!KTGLocalizationRegistry.IsHaltSpell("일반 공격"), "An unrelated spell must not stop input.");
        Require(KTGLocalizationRegistry.ContainsDefenseKeyword("한국 방패"), "Verified Korean defense keyword must be recognized.");
        Require(KTGLocalizationRegistry.IsRegisterButtonText("참가 신청"), "Korean registration button must be recognized.");
    }

    private static void KoreanDataIntegration(string gameDataPath)
    {
        Require(Directory.Exists(gameDataPath), "The supplied static game-data directory does not exist.");
        using var data = new GameData(gameDataPath, new LuminaOptions
        {
            DefaultExcelLanguage = Language.English,
            PanicOnSheetChecksumMismatch = true,
            CacheFileResources = false
        });
        var header = data.GetFile<ExcelHeaderFile>("exd/ktgtypewordtextdata.exh");
        Require(header != null && header.Languages.Contains(Language.Korean) &&
                !header.Languages.Contains(Language.English),
            "This integration scenario requires physically Korean-only word data.");
        try
        {
            // KR Lumina redirects an unavailable English request to Korean;
            // upstream Lumina instead throws UnsupportedLanguageException.
            var redirected = data.Excel.GetSheet<KTGTypeWordTextData>(Language.English);
            Equal(Language.Korean, redirected.Language);
            var korean = data.Excel.GetSheet<KTGTypeWordTextData>(Language.Korean);
            Equal(korean.GetRow(501).Unknown0.ExtractText(), redirected.GetRow(501).Unknown0.ExtractText());
        }
        catch (UnsupportedLanguageException) { }

        var manager = DispatchProxy.Create<IDataManager, StaticGameDataProxy>();
        ((StaticGameDataProxy)(object)manager).Data = data;
        Require(KTGWordDatabase.Load(manager), "The real Korean word database must load when the English sheet is absent.");
        Equal(Language.Korean, KTGWordDatabase.CurrentLanguage);
        Equal(UiLanguage.Korean, Loc.GameLanguage);
        Require(KTGWordDatabase.Count > 0, "The Korean word database must contain entries.");
        Require(!KTGWordDatabase.NeedsReload(manager), "Resolved Korean data must not trigger a reload every frame when Dalamud requests English.");
        var expected = data.Excel.GetSheet<KTGTypeWordTextData>(Language.Korean).GetRow(501).Unknown0.ExtractText().Trim();
        var cached = KTGWordDatabase.GetByRowId(501);
        Require(cached != null, "Verified Korean row 501 must be loaded.");
        Equal(expected, cached!.Original);
        Equal(expected, KTGWordDatabase.FindMatch(expected)?.Original);
        Require(KTGWordDatabase.Load(manager), "Repeated load of the same client data must succeed.");
        Require(ReferenceEquals(cached, KTGWordDatabase.GetByRowId(501)), "Repeated load must reuse the existing entries.");
        Require(!KTGWordDatabase.NeedsReload(manager), "The language cache must remain current after repeated load.");
    }

    private static void ExistingLanguages()
    {
        foreach (var (language, value, code, label, close) in new[]
        {
            (UiLanguage.Japanese, 0, "JA", "タイピング自動入力", "閉じる"),
            (UiLanguage.English, 1, "EN", "Typing Solver", "Close"),
            (UiLanguage.German, 2, "DE", "Tipp-Automatik", "Schließen"),
            (UiLanguage.French, 3, "FR", "Solveur de frappe", "Fermer")
        })
        {
            Equal(value, (int)language);
            WithUiLanguage(language, () =>
            {
                Equal(code, Loc.GetLanguageShortName(Loc.CurrentLanguage));
                Equal(label, Loc.Get("TypingSolver"));
                Equal(close, Loc.Get("Close"));
            });
        }
        foreach (var spell in new[] { "Dynamic Sensory Jammer", "動体感知ジャマー", "Dynamischer Störsender", "Brouilleur sensoriel" })
            Require(KTGLocalizationRegistry.IsHaltSpell(spell), $"Existing language safety stop must be preserved: {spell}");
    }

    private static void ExistingInput()
    {
        WithWordLanguage("English", () =>
        {
            AssertVirtualKeys("ab19", SeVirtualKey.A, SeVirtualKey.B, SeVirtualKey.NUMPAD1, SeVirtualKey.NUMPAD9);
            AssertVirtualKeys("äöüß", (SeVirtualKey)0xDE, (SeVirtualKey)0xC0, (SeVirtualKey)0xBA, (SeVirtualKey)0xDB);
            AssertVirtualKeys("éèàçù", (SeVirtualKey)0x32, (SeVirtualKey)0x37, (SeVirtualKey)0x30, (SeVirtualKey)0x39, (SeVirtualKey)0xC0);
            var circumflex = QueueFor("âêîôû");
            Equal(11, circumflex.Length);
            var vowels = new[] { SeVirtualKey.A, SeVirtualKey.E, SeVirtualKey.I, SeVirtualKey.O, SeVirtualKey.U };
            for (var i = 0; i < vowels.Length; i++)
            {
                Require(!circumflex[i * 2].IsUnicode && circumflex[i * 2].VirtualKey != SeVirtualKey.NO_KEY,
                    "Circumflex must retain the dead-key action.");
                Equal(vowels[i], circumflex[i * 2 + 1].VirtualKey);
            }
            AssertSubmit(circumflex[^1]);
        });
        WithWordLanguage("Japanese", () => AssertUnicodeWord("あいう", "あいう"));
    }

    private static void KoreanMixedInput()
    {
        WithWordLanguage("Korean", () =>
        {
            AssertUnicodeWord("ABC 123", "ABC 123");
            AssertUnicodeWord("한국 äéâ A1", "한국 äéâ A1");
        });
        WithWordLanguage("English", () => AssertUnicodeWord("한국 A1", "한국 A1"));
    }

    private static void InputBoundaries()
    {
        var inputType = typeof(KTGInputSimulator).GetNestedType("INPUT", BindingFlags.NonPublic)!;
        Equal(40, Marshal.SizeOf(inputType));
        WithWordLanguage("Korean", () =>
        {
            AssertUnicodeWord("한글!?'-", "한글!?'-", ignorePunctuation: false);
            Equal("한국 방패", KTGWordDatabase.StripPunctuation(" 한국 방패 !? "));
            Equal("tous", KTGWordDatabase.StripPunctuation("tous !"));
            var simulator = new KTGInputSimulator();
            simulator.EnqueueWord("한국 A1", new Configuration());
            Require(simulator.IsTyping, "Enqueued word must be active.");
            simulator.Clear();
            Equal(0, simulator.RemainingChars);
            Equal(string.Empty, simulator.CurrentWord);
            Require(!simulator.IsTyping, "Cancellation must stop the queue.");
            Require(!simulator.JustSubmitted, "Cancellation must not report submission.");
        });
    }

    private static void LookupBoundaries()
    {
        WithUiLanguage(UiLanguage.Korean, () => Equal("UnknownKey", Loc.Get("UnknownKey")));
        WithSyntheticWords(() =>
        {
            Equal(900001u, KTGWordDatabase.FindMatch("한*방패")?.RowId);
            Require(KTGWordDatabase.FindMatch(" ") == null, "Blank text must not match.");
            Require(KTGStateReader.IsDefenseSentence("한국 방패", 7), "A synthetic defense prompt must retain defense priority.");
            Require(!KTGStateReader.IsDefenseSentence("한국 방패", 2), "The boss slot must remain excluded from defensive prompts.");
        });
    }

    private static void AssertUnicodeWord(string input, string expected, bool ignorePunctuation = true)
    {
        var actions = QueueFor(input, ignorePunctuation);
        Equal(expected.Length + 1, actions.Length);
        Require(actions[..^1].All(a => a.IsUnicode), "Printable Korean-mode text must use Unicode rather than the active IME key layout.");
        Equal(expected, new string(actions[..^1].Select(a => a.Character).ToArray()));
        AssertSubmit(actions[^1]);
    }

    private static void AssertVirtualKeys(string input, params SeVirtualKey[] expected)
    {
        var actions = QueueFor(input);
        Equal(expected.Length + 1, actions.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            Require(!actions[i].IsUnicode, $"Expected a virtual-key action at {i}.");
            Equal(expected[i], actions[i].VirtualKey);
        }
        AssertSubmit(actions[^1]);
    }

    private static KTGInputSimulator.KeyAction[] QueueFor(string input, bool ignorePunctuation = true)
    {
        var simulator = new KTGInputSimulator();
        simulator.EnqueueWord(input, new Configuration { IgnorePunctuation = ignorePunctuation });
        var queue = typeof(KTGInputSimulator).GetField("_pendingActions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((IEnumerable<KTGInputSimulator.KeyAction>)queue.GetValue(simulator)!).ToArray();
    }

    private static void AssertSubmit(KTGInputSimulator.KeyAction action)
    {
        Require(!action.IsUnicode, "Submission must remain a control key.");
        Equal(SeVirtualKey.RETURN, action.VirtualKey);
    }

    private static void WithUiLanguage(UiLanguage language, System.Action test)
    {
        var property = typeof(Plugin).GetProperty(nameof(Plugin.Configuration), BindingFlags.Public | BindingFlags.Static)!;
        var original = property.GetValue(null);
        try
        {
            property.SetValue(null, new Configuration { SelectedLanguage = language });
            test();
        }
        finally { property.SetValue(null, original); }
    }

    private static void WithWordLanguage(string language, System.Action test)
    {
        var property = typeof(KTGWordDatabase).GetProperty(nameof(KTGWordDatabase.CurrentLanguage))!;
        var original = property.GetValue(null);
        try
        {
            property.SetValue(null, Enum.Parse(property.PropertyType, language));
            test();
        }
        finally { property.SetValue(null, original); }
    }

    private static void WithSyntheticWords(System.Action test)
    {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var entries = (List<WordEntry>)typeof(KTGWordDatabase).GetField("Entries", flags)!.GetValue(null)!;
        var exact = (Dictionary<string, WordEntry>)typeof(KTGWordDatabase).GetField("ByExactMatch", flags)!.GetValue(null)!;
        var stripped = (Dictionary<string, WordEntry>)typeof(KTGWordDatabase).GetField("ByStrippedMatch", flags)!.GetValue(null)!;
        var oldEntries = entries.ToArray();
        var oldExact = exact.ToArray();
        var oldStripped = stripped.ToArray();
        try
        {
            entries.Clear(); exact.Clear(); stripped.Clear();
            var shield = new WordEntry { RowId = 900001, Original = "한국 방패!", Normalized = "한국 방패!", Stripped = "한국 방패", IsDefense = true };
            var alias = new WordEntry { RowId = 900002, Original = "합성 단어", Normalized = "합성 단어", Stripped = "합성 단어", Reading = "합성 별칭" };
            entries.AddRange([shield, alias]);
            exact.Add("한국 방패!", shield); stripped.Add("한국 방패", shield);
            exact.Add("합성 단어", alias); stripped.Add("합성 단어", alias);
            exact.Add("합성 별칭", alias); stripped.Add("합성 별칭", alias);
            test();
        }
        finally
        {
            entries.Clear(); entries.AddRange(oldEntries);
            exact.Clear(); foreach (var item in oldExact) exact.Add(item.Key, item.Value);
            stripped.Clear(); foreach (var item in oldStripped) stripped.Add(item.Key, item.Value);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}; got {actual}.");
    }
}

public class StaticGameDataProxy : DispatchProxy
{
    public GameData? Data { get; set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        return targetMethod?.Name switch
        {
            "get_GameData" => Data ?? throw new InvalidOperationException("Static data was not initialized."),
            "get_Excel" => (Data ?? throw new InvalidOperationException("Static data was not initialized.")).Excel,
            _ => throw new NotSupportedException($"The static-data integration test does not use {targetMethod?.Name}.")
        };
    }
}
