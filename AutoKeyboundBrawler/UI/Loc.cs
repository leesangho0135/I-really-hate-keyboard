using Dalamud.Game;
using ECommons.DalamudServices;

namespace AutoKeyboundBrawler.UI;

public static class Loc
{
    public static UiLanguage CurrentLanguage
    {
        get
        {
            if (Plugin.Configuration?.SelectedLanguage.HasValue == true)
            {
                return Plugin.Configuration.SelectedLanguage.Value;
            }

            return GameLanguage;
        }
    }

    public static UiLanguage GameLanguage
    {
        get
        {
            if (Data.KTGWordDatabase.IsLoaded &&
                Data.KTGWordDatabase.CurrentLanguage == Lumina.Data.Language.Korean)
            {
                return UiLanguage.Korean;
            }

            var gameLang = Svc.ClientState.ClientLanguage;
            // Korean client forks may expose a language absent from upstream Dalamud.
            if (gameLang.ToString() == "Korean") return UiLanguage.Korean;

            return gameLang switch
            {
                ClientLanguage.Japanese => UiLanguage.Japanese,
                ClientLanguage.German => UiLanguage.German,
                ClientLanguage.French => UiLanguage.French,
                _ => UiLanguage.English
            };
        }
    }

    public static string GetLanguageShortName(UiLanguage lang)
    {
        return lang switch
        {
            UiLanguage.Japanese => "JA",
            UiLanguage.German => "DE",
            UiLanguage.French => "FR",
            UiLanguage.Korean => "KO",
            _ => "EN"
        };
    }

    public static string Get(string key)
    {
        var lang = CurrentLanguage;
        return (key, lang) switch
        {
            // --- Toggles ---
            ("TypingSolver", UiLanguage.Japanese) => "タイピング自動入力",
            ("TypingSolver", UiLanguage.German) => "Tipp-Automatik",
            ("TypingSolver", UiLanguage.French) => "Solveur de frappe",
            ("TypingSolver", UiLanguage.Korean) => "자동 입력",
            ("TypingSolver", _) => "Typing Solver",

            ("TypingSolverSub", UiLanguage.Japanese) => "人間らしい入力速度・バリア検知・敵優先度",
            ("TypingSolverSub", UiLanguage.German) => "Menschliche Tippgeschwindigkeit & Prioritäten",
            ("TypingSolverSub", UiLanguage.French) => "Vitesse humaine, bouclier & priorités",
            ("TypingSolverSub", UiLanguage.Korean) => "입력 속도 조절 · 방어 · 대상 우선순위",
            ("TypingSolverSub", _) => "Humanized typing, shield detection & adds priority",

            ("TypingSolverTip", UiLanguage.Japanese) => "敵のセリフや技名を一文字ずつ自動入力して素早く撃破します。",
            ("TypingSolverTip", UiLanguage.German) => "Löst gegnerische Phrasen Buchstabe für Buchstabe mit realistischer Verzögerung.",
            ("TypingSolverTip", UiLanguage.French) => "Tape automatiquement les phrases ennemies lettre par lettre avec un délai humanisé.",
            ("TypingSolverTip", UiLanguage.Korean) => "적의 문구를 한 글자씩 자동으로 입력합니다. 글자 사이의 입력 간격을 조절할 수 있습니다.",
            ("TypingSolverTip", _) => "Automatically solves enemy phrases letter-by-letter with humanized typing delays.",

            ("AfkLoop", UiLanguage.Japanese) => "自動周回 (AFK)",
            ("AfkLoop", UiLanguage.German) => "AFK-Schleife",
            ("AfkLoop", UiLanguage.French) => "Boucle AFK",
            ("AfkLoop", UiLanguage.Korean) => "자동 반복",
            ("AfkLoop", _) => "AFK Loop",

            ("AfkLoopSub", UiLanguage.Japanese) => "受付・突入・リザルト退出の全自動化",
            ("AfkLoopSub", UiLanguage.German) => "Automatische Anmeldung, Beitritt & Abschluss",
            ("AfkLoopSub", UiLanguage.French) => "Inscription, entrée & sortie des résultats",
            ("AfkLoopSub", UiLanguage.Korean) => "참가 신청 · 입장 · 결과창 종료",
            ("AfkLoopSub", _) => "Auto-register, commence & exit results",

            ("AfkLoopTip", UiLanguage.Japanese) => "受付NPCへの話しかけ、マッチング突入、終了後のリザルト退出を自動化します。",
            ("AfkLoopTip", UiLanguage.German) => "Übernimmt die Anmeldung am Schalter, den Inhaltsbeitritt und das Verlassen des Ergebnisbildschirms.",
            ("AfkLoopTip", UiLanguage.French) => "Interagit automatiquement avec la préposée, confirme l'entrée et quitte l'écran de fin.",
            ("AfkLoopTip", UiLanguage.Korean) => "접수 NPC와 대화하고 참가를 신청한 뒤, 입장과 결과창 종료를 자동으로 진행합니다.",
            ("AfkLoopTip", _) => "Automatically interacts with Registrar, confirms entry, commences duty, and exits results.",

            ("AutoOpenReg", UiLanguage.Japanese) => "受付接近時に自動表示",
            ("AutoOpenReg", UiLanguage.German) => "Autom. Öffnen am Schalter",
            ("AutoOpenReg", UiLanguage.French) => "Ouverture auto préposée",
            ("AutoOpenReg", UiLanguage.Korean) => "접수처 근처에서 자동 표시",
            ("AutoOpenReg", _) => "Auto-Open Near Registrar",

            ("AutoOpenRegSub", UiLanguage.Japanese) => "参加受付NPCの近くでウィンドウを自動で開く",
            ("AutoOpenRegSub", UiLanguage.German) => "Öffnet Fenster in Interaktionsreichweite",
            ("AutoOpenRegSub", UiLanguage.French) => "Ouvre l'interface à portée d'interaction",
            ("AutoOpenRegSub", UiLanguage.Korean) => "접수 NPC 근처에서 설정창 표시",
            ("AutoOpenRegSub", _) => "Open UI automatically in interaction range",

            ("AutoOpenRegTip", UiLanguage.Japanese) => "キーバウンド・ブロウラーの参加受付NPCに近づいた際、自動でこの画面を開きます。",
            ("AutoOpenRegTip", UiLanguage.German) => "Öffnet dieses Menü automatisch, wenn du dich der Rezeptionistin näherst.",
            ("AutoOpenRegTip", UiLanguage.French) => "Ouvre automatiquement cette interface lorsque vous vous approchez de la préposée.",
            ("AutoOpenRegTip", UiLanguage.Korean) => "참가 접수 NPC에게 다가가 대화할 수 있는 거리에 들어오면 이 창을 자동으로 엽니다.",
            ("AutoOpenRegTip", _) => "Automatically opens this interface whenever you approach the Keybound Registrar NPC.",

            ("IgnorePunct", UiLanguage.Japanese) => "句読点・記号の省略",
            ("IgnorePunct", UiLanguage.German) => "Satzzeichen ignorieren",
            ("IgnorePunct", UiLanguage.French) => "Ignorer la ponctuation",
            ("IgnorePunct", UiLanguage.Korean) => "문장부호 생략",
            ("IgnorePunct", _) => "Ignore Punctuation",

            ("IgnorePunctSub", UiLanguage.Japanese) => "！、？、'、- などの記号を省略して入力",
            ("IgnorePunctSub", UiLanguage.German) => "Entfernt !, ?, ', - aus Phrasen",
            ("IgnorePunctSub", UiLanguage.French) => "Supprime auto !, ?, ', - des phrases",
            ("IgnorePunctSub", UiLanguage.Korean) => "!, ?, ', - 기호를 생략하여 입력",
            ("IgnorePunctSub", _) => "Auto-strip !, ?, ', - from phrases",

            ("IgnorePunctTip", UiLanguage.Japanese) => "「句読点を無視する」仕様に合わせて記号を除外し、より高速に入力します。",
            ("IgnorePunctTip", UiLanguage.German) => "Entfernt Satzzeichen automatisch für schnelleres, fehlerfreies Tippen.",
            ("IgnorePunctTip", UiLanguage.French) => "Supprime automatiquement la ponctuation pour une saisie plus rapide et sans erreur.",
            ("IgnorePunctTip", UiLanguage.Korean) => "느낌표, 물음표, 작은따옴표, 붙임표를 생략하여 입력합니다.",
            ("IgnorePunctTip", _) => "Automatically strips punctuation symbols to guarantee faster, error-free typing.",

            // --- Sliders & Sections ---
            ("Speed", UiLanguage.Japanese) => "速度設定",
            ("Speed", UiLanguage.German) => "GESCHWINDIGKEIT",
            ("Speed", UiLanguage.French) => "VITESSE",
            ("Speed", UiLanguage.Korean) => "입력 속도",
            ("Speed", _) => "SPEED",

            ("MinCharDelay", UiLanguage.Japanese) => "キー最小間隔 (ms)",
            ("MinCharDelay", UiLanguage.German) => "Min. Tastenverzögerung",
            ("MinCharDelay", UiLanguage.French) => "Délai min. entre frappes",
            ("MinCharDelay", UiLanguage.Korean) => "최소 입력 간격",
            ("MinCharDelay", _) => "Min Keystroke Delay",

            ("MaxCharDelay", UiLanguage.Japanese) => "キー最大間隔 (ms)",
            ("MaxCharDelay", UiLanguage.German) => "Max. Tastenverzögerung",
            ("MaxCharDelay", UiLanguage.French) => "Délai max. entre frappes",
            ("MaxCharDelay", UiLanguage.Korean) => "최대 입력 간격",
            ("MaxCharDelay", _) => "Max Keystroke Delay",

            ("SubDelay", UiLanguage.Japanese) => "決定 (Enter) 遅延 (ms)",
            ("SubDelay", UiLanguage.German) => "Bestätigungsverzögerung",
            ("SubDelay", UiLanguage.French) => "Délai de validation",
            ("SubDelay", UiLanguage.Korean) => "제출 대기 시간",
            ("SubDelay", _) => "Submit Delay",

            // --- Word Bar & Badges ---
            ("Standby", UiLanguage.Japanese) => "> 待機中 <",
            ("Standby", UiLanguage.German) => "> Bereit <",
            ("Standby", UiLanguage.French) => "> En attente <",
            ("Standby", UiLanguage.Korean) => "> 대기 중 <",
            ("Standby", _) => "> standby <",

            ("BadgeHalt", UiLanguage.Japanese) => "停止",
            ("BadgeHalt", UiLanguage.German) => "HALT",
            ("BadgeHalt", UiLanguage.French) => "ARRÊT",
            ("BadgeHalt", UiLanguage.Korean) => "정지",
            ("BadgeHalt", _) => "HALT",

            ("BadgeTyping", UiLanguage.Japanese) => "入力中",
            ("BadgeTyping", UiLanguage.German) => "TIPPEN",
            ("BadgeTyping", UiLanguage.French) => "SAISIE",
            ("BadgeTyping", UiLanguage.Korean) => "입력 중",
            ("BadgeTyping", _) => "TYPING",

            ("BadgeCombat", UiLanguage.Japanese) => "戦闘中",
            ("BadgeCombat", UiLanguage.German) => "KAMPF",
            ("BadgeCombat", UiLanguage.French) => "COMBAT",
            ("BadgeCombat", UiLanguage.Korean) => "전투 중",
            ("BadgeCombat", _) => "COMBAT",

            ("BadgeLoop", UiLanguage.Japanese) => "周回中",
            ("BadgeLoop", UiLanguage.German) => "SCHLEIFE",
            ("BadgeLoop", UiLanguage.French) => "BOUCLE",
            ("BadgeLoop", UiLanguage.Korean) => "반복 중",
            ("BadgeLoop", _) => "LOOP",

            ("BadgeIdle", UiLanguage.Japanese) => "待機",
            ("BadgeIdle", UiLanguage.German) => "BEREIT",
            ("BadgeIdle", UiLanguage.French) => "ATTENTE",
            ("BadgeIdle", UiLanguage.Korean) => "대기",
            ("BadgeIdle", _) => "IDLE",

            // --- Window Header Buttons & Tooltips ---
            ("Close", UiLanguage.Japanese) => "閉じる",
            ("Close", UiLanguage.German) => "Schließen",
            ("Close", UiLanguage.French) => "Fermer",
            ("Close", UiLanguage.Korean) => "닫기",
            ("Close", _) => "Close",

            ("Compact", UiLanguage.Japanese) => "最小化",
            ("Compact", UiLanguage.German) => "Kompakt",
            ("Compact", UiLanguage.French) => "Réduire",
            ("Compact", UiLanguage.Korean) => "접기",
            ("Compact", _) => "Compact",

            ("Expand", UiLanguage.Japanese) => "展開",
            ("Expand", UiLanguage.German) => "Erweitern",
            ("Expand", UiLanguage.French) => "Agrandir",
            ("Expand", UiLanguage.Korean) => "펼치기",
            ("Expand", _) => "Expand",

            ("LangTooltip", UiLanguage.Japanese) => "言語設定 (Language)",
            ("LangTooltip", UiLanguage.German) => "Spracheinstellungen (Language)",
            ("LangTooltip", UiLanguage.French) => "Paramètres de langue (Language)",
            ("LangTooltip", UiLanguage.Korean) => "언어 설정",
            ("LangTooltip", _) => "Language Settings",

            ("LangAuto", UiLanguage.Japanese) => "自動 (クライアント言語)",
            ("LangAuto", UiLanguage.German) => "Automatisch (Spiel-Sprache)",
            ("LangAuto", UiLanguage.French) => "Automatique (Langue du jeu)",
            ("LangAuto", UiLanguage.Korean) => "자동 (게임 언어)",
            ("LangAuto", _) => "Auto (Game Client)",

            _ => key
        };
    }
}

