using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

using CaretakerCore;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace CaretakerNET.Core
{
    public static class Caretaker
    {
        public const string DEFAULT_PREFIX = ">";
        public const ulong ASTRL_ID = 438296397452935169;
        public const ulong CARETAKER_ID = 1182009469824139395;
        public const ulong CARETAKER_CENTRAL_ID = 1186486803608375346;
        public const ulong SPACE_JAMBOREE_ID = 1230658674138157117;
        // public static string PrivatesPath = "C:/Users/AstrlJelly/Documents/GitHub/CaretakerPrivates/";

        public static void LogDebug(object? m = null, bool t = false) {
            if (MainHook.instance.config.DebugMode) InternalLog(m ?? "null", t, CaretakerCore.Core.LogSeverity.Info);
        }
    }
}
