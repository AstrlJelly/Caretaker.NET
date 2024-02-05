using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
// using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET.Core
{
    public static class Caretaker
    {
        #region String
        // don't wanna type this every time lol (and i swear it performs ever so slightly better)
        public static string ReplaceAll(this string stringToReplace, string oldStr, string newStr) => string.Join(newStr, stringToReplace.Split(oldStr));
        public static string ReplaceAll(this string stringToReplace, char oldStr, char newStr) => string.Join(newStr, stringToReplace.Split(oldStr));

        public static T? TryGet<T>(this IEnumerable<T> enumerable, int index) => enumerable.IsIndexValid(index) ? enumerable!.ElementAt(index) : default;

        // Item1 and Item2 look kinda ugly but a tuple makes sense for this method
        public static (string, string) SplitByIndex(this string stringToSplit, int index)
        {
            if (stringToSplit.IsIndexValid(index)) {
                return (stringToSplit[..index], stringToSplit[(index + 1)..]);
            } else {
                return (stringToSplit, "");
            }
        }

        public static (string, string) SplitByFirstChar(this string stringToSplit, char splitChar)
        {
            int index = stringToSplit.IndexOf(splitChar);
            if (index == -1) index = stringToSplit.Length;

            return SplitByIndex(stringToSplit, index);
        }
        // not used currently but i thought it would be interesting to make
        public static List<string> SplitByIndexes(this string stringToSplit, params int[] indexes)
        {
            indexes = [0, ..indexes, stringToSplit.Length];
            List<string> newStrings = [];
            for (int i = 0; i < indexes.Length - 1; i++) {
                newStrings.Add(stringToSplit.Substring(indexes[i], indexes[i + 1]));
            }
            return newStrings;
        }

        public static T? Parse<T>(this string stringToParse)
        {
            dynamic? parsed = typeof(T).ToString() switch
            {
                "Int64" => int.Parse(stringToParse),
                "UInt64" => uint.Parse(stringToParse),
                "Double" => double.Parse(stringToParse),
                _ => null,
            };
            return (T?)parsed; // this cast gets around the silly compiler
        }

        // public static bool TryParse<T>(this string stringToParse, out T parsed)
        // {
        //     switch (typeof(T).ToString().ToLower())
        //     {
        //         case "":
        //         break;
        //         default:
        //     }
        //     // parsed = typeof(T) switch
        //     // {

        //     //     _ => default,
        //     // };
        // }
        #endregion

        #region List
        public static bool IsIndexValid<T>(this IEnumerable<T>? enumerable, int index) => enumerable != null && index >= 0 && index < enumerable.Count();

        // ElementAt moment
        public static T? GetFromIndexes<T>(this IEnumerable<IEnumerable<T>> enumerable, int i, int j) {
            return enumerable.IsIndexValid(i) && enumerable.ElementAt(i).IsIndexValid(j) ? enumerable.ElementAt(i).ElementAt(j) : default;
        }

        // Item1 and Item2 look kinda ugly but a tuple makes sense for this method
        public static (List<T>, List<T>?) SplitByIndex<T>(this List<T> listToSplit, int index)
        {
            if (listToSplit.IsIndexValid(index)) {
                return (listToSplit[..index], listToSplit[(index + 1)..]);
            } else {
                return (listToSplit, default);
            }
        }

        public static T? GetRandom<T>(this IEnumerable<T> enumerable) {
            var random = new Random();
            int count = enumerable.Count();
            return count > 0 ? enumerable.ElementAt(random.Next(count)) : default;
        }

        public static bool TryFindIndex<T>(this IEnumerable<T> enumerable, Predicate<T> match, out int index) {
            index = -1;
            foreach (T item in enumerable)
            {
                index++;
                if (match.Invoke(item)) return true;
            }
            return false;
        }
        #endregion

        #region Enum
        public static string EnumName(this Enum whichEnum, int index) {
            Type enumType = whichEnum.GetType();
            int max = Enum.GetNames(enumType).Length - 1;
            int newIndex = Math.Clamp(index, 0, max);
            if (newIndex != index) {
                Caretaker.LogWarning($"EnumName() had {index} put into it; defaulted to {newIndex} instead.");
            }
            return Enum.GetName(enumType, newIndex)!;
        }
        #endregion

        #region Discord
        public static async Task<IUserMessage> Reply(this IUserMessage msg, object reply, bool ping = false)
        {
            return await msg.ReplyAsync(reply.ToString(), allowedMentions: ping ? AllowedMentions.All : AllowedMentions.None);
        }

        public static async Task<IUserMessage> RandomReply(this IUserMessage msg, object[] replies, bool ping = false)
        {
            string? reply = (string?)replies.GetRandom();
            return await msg.Reply(string.IsNullOrEmpty(reply) ? "." : reply, ping);
        }

        public static async Task<IUserMessage> EmbedReply(this IUserMessage msg, Embed embed)
        {
            return await msg.ReplyAsync(embed: embed);
        }

        public static SocketGuild? GetGuild(this IUserMessage msg) 
        {
            if (msg.Channel is not SocketGuildChannel chnl) return null;
            return chnl.Guild;
        }

        public async static Task EmojiReact(this IMessage msg, string emojiStr)
        {
            await msg.AddReactionAsync(Emoji.Parse(emojiStr));
        }

        private static string? IDFromReference(string reference) {
            return reference[0] == '<' && reference.Length >= 2 ? reference[2..^1] : null;
        }

        public static SocketGuild? ParseGuild(this DiscordSocketClient c, string guildToParse)
        {
            SocketGuild? guild = null;
            Func<string, SocketGuild?>[] actions = [
                x => c.Guilds.FirstOrDefault(g => g.Name.Equals(guildToParse, StringComparison.CurrentCultureIgnoreCase) || g.Id == ulong.Parse(guildToParse)),
                // x => (SocketGuild?)c.Guilds.FirstOrDefault(ulong.Parse(guildToParse)),
            ];
            for (int i = 0; i < actions.Length; i++) {
                try {
                    guild = actions[i](guildToParse);
                    if (guild != null) break;
                } catch {
                    continue;
                }
            }
            return guild;
        }

        public static ITextChannel? ParseChannel(this SocketGuild guild, string channelToParse)
        {
            if (guild == null) return null;
            ITextChannel? channel = null;
            Func<string, ITextChannel?>[] actions = [
                x => guild.TextChannels.FirstOrDefault(chan => chan.Name.Equals(channelToParse, StringComparison.CurrentCultureIgnoreCase)),
                x => (ITextChannel)guild.GetChannel(ulong.Parse(IDFromReference(channelToParse) ?? channelToParse)),
            ];
            for (int i = 0; i < actions.Length; i++) {
                try {
                    channel = actions[i](channelToParse);
                    if (channel != null) break;
                } catch {
                    continue;
                }
            }
            return channel;
        }

        public static IUser? ParseUser(this DiscordSocketClient c, string userToParse, SocketGuild? guild = null)
        {
            IUser? user = null;
            (userToParse, string discriminator) = userToParse.SplitByFirstChar('#');
            Action[] actions = [
                delegate { user = c.GetUser(userToParse, discriminator == "" ? null : discriminator); },
                // delegate { user = c.GetUser(userToParse.ToLower()); },
                delegate { user = c.GetUser(ulong.Parse(IDFromReference(userToParse) ?? userToParse)); },
                delegate { user = guild?.Users.FirstOrDefault(x => x.Nickname == userToParse || x.GlobalName.Equals(userToParse, StringComparison.CurrentCultureIgnoreCase)); },
            ];
            Caretaker.LogTemp(userToParse);
            for (int i = 0; i < actions.Length; i++) {
                try {
                    actions[i].Invoke();
                    if (user != null) break;
                } catch {
                    continue;
                }
            }
            return user;
        }

        public static string ChannelLinkFromID(ulong id) => $"<#{id}>";
        public static string UserPingFromID(ulong id) => $"<@{id}>";
        #endregion

        #region Console
        public static void Log(object message, bool time = false, LogSeverity severity = LogSeverity.Info)
        {
            Console.ForegroundColor = severity switch {
                LogSeverity.Critical or LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning                       => ConsoleColor.Yellow,
                LogSeverity.Verbose or LogSeverity.Debug  => ConsoleColor.DarkGray,
                LogSeverity.Info or _                     => ConsoleColor.White,
            };
            if (time) message = $"[{CurrentTime()}] " + message;
            Console.WriteLine(message?.ToString());
            Console.ResetColor();
        }

        [Obsolete("THIS SHOULD BE TEMPORARY!!")] // a LOT of logging.
        public static void LogTemp(object? message = null, bool time = false) { Log(message ?? "null", time, LogSeverity.Info); }
        public static void LogInfo(object? message = null, bool time = false) { Log(message ?? "null", time, LogSeverity.Info); }
        public static void LogWarning(object? message = null, bool time = false) { Log(message ?? "Warning!", time, LogSeverity.Warning); }
        public static void LogError(object? message = null, bool time = false) { Log(message ?? "Error!", time, LogSeverity.Error); }
        public static void LogDebug(object? message = null, bool time = false) { if (MainHook.instance.DebugMode) Log(message ?? "null", time, LogSeverity.Info); }

        public static void ChangeConsoleTitle(string status)
        {
            Console.Title = "CaretakerNET : ";
        }
        #endregion

        #region Time
        public enum Time { ms, sec, min, hr, day, week };
        
        // converts from seconds to minutes, hours to ms, minutes to days, etc.
        public static double ConvertTime(double time, Time typeFromTemp = Time.ms, Time typeToTemp = Time.ms) {
            if (typeToTemp == typeFromTemp) return time;
            var typeFrom = (int)typeFromTemp;
            var typeTo = (int)typeToTemp;

            int modifier = 1;
            int[] converts = [1000, 60, 60, 24, 7];

            for (int i = Math.Min(typeFrom, typeTo); i < Math.Max(typeFrom, typeTo); i++) {
                modifier *= converts[i];
            }

            return (typeFrom > typeTo) ? (time * modifier) : (time / modifier);
        }

        public static string CurrentTime() => DateTime.Now.ToString("HH:mm:ss tt");

        public static long DateNow() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        #endregion
    
        #region Async
        public static void Sleep()
        {
            
        } 
        #endregion
    }
}
