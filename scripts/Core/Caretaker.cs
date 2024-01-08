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
        #endregion

        #region List
        public static bool IsIndexValid<T>(this IEnumerable<T>? enumerable, int index) => enumerable != null && index < enumerable.Count();

        // ElementAt moment
        public static T? GetFromIndexes<T>(this IEnumerable<IEnumerable<T>> enumerable, int i, int j) {
            return enumerable.IsIndexValid(i) && enumerable.ElementAt(i).IsIndexValid(j) ? enumerable.ElementAt(i).ElementAt(j) : default;
        }

        public static T? GetRandom<T>(this IEnumerable<T> list) {
            var random = new Random(); 
            int count = list.Count();
            return count > 0 ? list.ElementAt(random.Next(list.Count())) : default;
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
            return await msg.ReplyAsync(reply.ToString());
        }

        public static async Task<IUserMessage> RandomReply(this IUserMessage msg, object[] replies, bool ping = false)
        {
            return await msg.Reply(replies.GetRandom() ?? "", ping);
        }

        public static async Task<IUserMessage> EmbedReply(this IUserMessage msg, Embed embed)
        {
            return await msg.ReplyAsync(embed: embed);
        }

        public static SocketGuild GetGuild(this SocketMessage msg) 
        {
            if (msg.Channel is not SocketGuildChannel chnl) { // pattern matching is freaky but i like it
                throw new Exception($"msg with id ${msg.Id} apparently... didn't have a channel?? idk.");
            }
            return chnl.Guild;
        }
        public static SocketGuild? TryGetGuild(this SocketMessage msg) 
        {
            try {
                return GetGuild(msg);
            } catch (System.Exception) {
                return null;
            }
        }

        public async static Task EmojiReact(this IMessage msg, string emojiStr)
        {
            await msg.AddReactionAsync(Emoji.Parse(emojiStr));
        }

        private static string? IDFromReference(string reference) {
            return reference[0] == '<' && reference.Length >= 2 ? reference[2..^1] : null;
        }

        public static IUser? ParseUser(string userToParse, SocketGuild? guild = null)
        {
            IUser? user = null;
            (userToParse, string discriminator) = userToParse.SplitByFirstChar('#');
            Action[] actions = [
                delegate { user = MainHook.instance._client.GetUser(userToParse, discriminator == "" ? null : discriminator); },
                // delegate { user = MainHook.instance._client.GetUser(userToParse.ToLower()); },
                delegate { user = MainHook.instance._client.GetUser(ulong.Parse(IDFromReference(userToParse) ?? userToParse)); },
                delegate { user = guild?.Users.FirstOrDefault(x => x.Nickname == userToParse || x.GlobalName.Equals(userToParse, StringComparison.CurrentCultureIgnoreCase)); },
            ];
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

        public static ITextChannel? ParseChannel(string channelToParse, SocketGuild? guild)
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
        public static string ChannelLinkFromID(ulong id) => $"<#{id}>";
        public static string UserPingFromID(ulong id) => $"<@{id}>";
        #endregion

        #region Logging
        public static void Log(object message, LogSeverity severity = LogSeverity.Info)
        {
            Console.ForegroundColor = severity switch {
                LogSeverity.Critical or LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning                       => ConsoleColor.Yellow,
                LogSeverity.Verbose or LogSeverity.Debug  => ConsoleColor.DarkGray,
                LogSeverity.Info or _                     => ConsoleColor.White,
            };
            Console.WriteLine(message.ToString());
            Console.ResetColor();
        }

        public static void LogWarning(object? message = null) { Log(message ?? "Warning!", LogSeverity.Warning); }
        public static void LogError(object? message = null) { Log(message ?? "Error!", LogSeverity.Error); }
        public static void LogDebug(object? message = null) { if (MainHook.instance.DebugMode) Log(message ?? "null", LogSeverity.Info); }
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

        public static long CurrentTime() => new DateTimeOffset().ToUnixTimeMilliseconds();
        #endregion
    
        #region Async
        public static void Sleep()
        {
            
        } 
        #endregion
    }
}
