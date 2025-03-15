using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace CaretakerNET.Core
{
    public static class Caretaker
    {
        public const string DEFAULT_PREFIX = ">";
        public const ulong COSSETT_ID = 1337930184057290815;
        public const ulong CARETAKER_ID = 1349936111991914526;
        public const ulong CARETAKER_CENTRAL_ID = 1186486803608375346;
        // public const ulong SPACE_JAMBOREE_ID = 1230658674138157117;
        // public static string PrivatesPath = "C:/Users/AstrlJelly/Documents/GitHub/CaretakerPrivates/";

        public static void LogDebug(object? m = null, bool t = false) {
            if (MainHook.Instance.Config.DebugMode) InternalLog(m ?? "null", t, CaretakerCoreNET.CaretakerCore.LogSeverity.Info);
        }
    }
}
