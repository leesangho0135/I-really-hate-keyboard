using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoKeyboundBrawler.Data;

public static class KTGLocalizationRegistry
{
    // ------------------------------------------------------------------------
    // 1. NPC Identification (Keybound Registrar)
    // ------------------------------------------------------------------------
    public const uint KeyboundRegistrarDataId = 1058751;

    public static readonly string[] LocalizedRegistrarNames =
    [
        "Keybound registrar",       // English
        "参加受付",                  // Japanese
        "Rezeptionistin",           // German
        "préposée aux inscriptions", // French
        "참가 접수원"               // Korean (ENpcResident 1058751)
    ];

    public static bool IsRegistrar(uint dataId, string name)
    {
        if (dataId == KeyboundRegistrarDataId)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        foreach (var regName in LocalizedRegistrarNames)
        {
            if (name.Contains(regName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------------
    // 2. Halt Avoidance Spells (Dynamic Sensory Jammer & Cursed Gaze)
    // ------------------------------------------------------------------------
    public const uint DynamicSensoryJammerActionId = 49702;
    public const uint CursedGazeActionId = 49529;

    public static readonly uint[] HaltActionIds =
    [
        DynamicSensoryJammerActionId,
        CursedGazeActionId,
        21396, 17407, 8342, 8341, 7354, 7353, 32448, 512
    ];

    public static readonly string[] LocalizedHaltSpells =
    [
        // English
        "Dynamic Sensory Jammer",
        "Cursed Gaze",
        // Japanese
        "動体感知ジャマー",
        "呪いの視線",
        // German
        "Dynamischer Störsender",
        "Fluchblick",
        // French
        "Brouilleur sensoriel",
        "Regard maudit",
        // Korean (Action 49702 and 49529)
        "동작 감지 교란",
        "저주의 시선"
    ];

    public static bool IsHaltSpell(string? spellName, uint actionId = 0)
    {
        if (actionId > 0 && HaltActionIds.Contains(actionId))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(spellName))
        {
            return false;
        }

        foreach (var haltName in LocalizedHaltSpells)
        {
            if (spellName.Contains(haltName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------------
    // 3. Inputs Notification Banners (Addon Sheet Rows 17839 & 17840)
    // ------------------------------------------------------------------------
    public const uint InputsDisabledAddonRowId = 17839;
    public const uint InputsReEnabledAddonRowId = 17840;

    public static readonly string[] LocalizedInputsDisabledBanners =
    [
        "Inputs disabled",
        "が付与された",
        "Schreibblockade verursacht",
        "Impossible de saisir du texte",
        "효과가 부여되었습니다" // Korean (Addon 17839)
    ];

    public static readonly string[] LocalizedInputsReEnabledBanners =
    [
        "Inputs re-enabled",
        "が切れた",
        "Schreibblockade aufgehoben",
        "s'est dissipé",
        "효과가 사라졌습니다" // Korean (Addon 17840)
    ];

    public static bool MatchesInputsDisabled(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return LocalizedInputsDisabledBanners.Any(b => text.Contains(b, StringComparison.OrdinalIgnoreCase));
    }

    public static bool MatchesInputsReEnabled(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return LocalizedInputsReEnabledBanners.Any(b => text.Contains(b, StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------------
    // 4. Needles Spells & Cactuar Identification
    // ------------------------------------------------------------------------
    public static readonly uint[] CactuarRowIds = [501, 841, 987, 991, 1224, 1453, 1728];

    public static readonly uint[] NeedlesActionIds = [49524, 48580, 48581, 39023];

    public static readonly string[] LocalizedNeedlesSpellRoots =
    [
        "needle",       // English (??? Needles, 1,000 Needles, etc.)
        "stachel",      // German (???-Stacheln)
        "nadel",        // German (1.000 Nadeln, ??? Nadeln)
        "épine",        // French (??? Épines, 1 000 épines)
        "epine",        // French (??? Epines - unaccented fallback)
        "aiguille",     // French (1000 aiguilles)
        "針",           // Japanese Kanji (針？本, 針千本, 針幾千本)
        "はり",         // Japanese Hiragana
        "???",          // Signature of Cactuar ??? Needles across European localizations
        "？",           // Fullwidth question mark in Japanese 針？本
        "바늘",         // Korean (Action 49524)
        "침천개"        // Korean (KTGTypeWordTextData 1453)
    ];

    public static readonly string[] LocalizedCactuarRoots =
    [
        "cactuar",      // English
        "サボテンダー",  // Japanese
        "さぼてんだー",  // Japanese Hiragana
        "kaktor",       // German
        "pampa",        // French
        "sabotender",
        "사보텐",       // Korean (KTGTypeWordTextData 501, 841, 987, 1728)
        "해피텐더"      // Korean (KTGTypeWordTextData 991)
    ];

    public static bool IsNeedlesSpell(string? spellName, uint actionId = 0)
    {
        if (actionId > 0 && NeedlesActionIds.Contains(actionId))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(spellName)) return false;

        return LocalizedNeedlesSpellRoots.Any(root => spellName.Contains(root, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsCactuarPrompt(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return false;
        return LocalizedCactuarRoots.Any(root => prompt.Contains(root, StringComparison.OrdinalIgnoreCase)) ||
               LocalizedNeedlesSpellRoots.Any(root => prompt.Contains(root, StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------------
    // 5. Defensive Sentences (Shield prompts)
    // ------------------------------------------------------------------------
    public static readonly string[] LocalizedDefenseKeywords =
    [
        // English
        "defense", "defend", "barrier", "shield", "protect",
        // German
        "verteidigung", "schild", "barriere", "schützen",
        // French
        "défense", "bouclier", "barrière", "protéger",
        // Japanese
        "防", "守", "バリア", "シールド",
        // Korean (KTGTypeWordTextData 522, 1426 and Addon 17841)
        "방패", "방어", "보호막"
    ];

    public static bool ContainsDefenseKeyword(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;
        return LocalizedDefenseKeywords.Any(kw => word.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------------
    // 6. Registration Confirmation Button
    // ------------------------------------------------------------------------
    public static readonly string[] LocalizedRegisterButtonTexts =
    [
        "Register",     // English
        "参加登録",      // Japanese
        "登録",          // Japanese
        "Anmelden",     // German
        "S'inscrire",   // French
        "참가 신청"     // Korean (Addon 17824)
    ];

    public static bool IsRegisterButtonText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return LocalizedRegisterButtonTexts.Any(b => text.Contains(b, StringComparison.OrdinalIgnoreCase));
    }
}

