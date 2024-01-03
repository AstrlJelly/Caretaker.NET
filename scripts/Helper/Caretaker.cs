using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET.Helper
{
    public static class Caretaker
    {
        #region String
        // don't wanna type this every time lol (and i swear it performs ever so slightly better)
        public static string ReplaceAll(this string stringToReplace, string oldStr, string newStr) => string.Join(newStr, stringToReplace.Split(oldStr));
        public static string ReplaceAll(this string stringToReplace, char oldStr, char newStr) => string.Join(newStr, stringToReplace.Split(oldStr));
        // Item1 and Item2 look kinda ugly but a tuple makes sense for this method
        public static (string, string) SplitByIndex(this string stringToSplit, int index)
        {
            if (stringToSplit.IsIndexValid(index)) {
                return (stringToSplit[..index], stringToSplit[(index + 1)..]);
            } else {
                return (stringToSplit, "");
            }
        }

        public static (string, string) SplitByChar(this string stringToSplit, char splitChar)
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

        public static T GetRandom<T>(this IEnumerable<T> list) {
            var random = new Random(); 
            return list.ElementAt(random.Next(list.Count()));
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
        public static SocketGuild GetGuild(this SocketMessage msg) 
        {
            if (msg.Channel is not SocketGuildChannel chnl) { // pattern matching is freaky but i like it
                throw new Exception($"msg with id ${msg.Id} apparently... didn't have a channel?? idk.");
            }
            return chnl.Guild;
        }

        public async static Task EmojiReact(this IMessage msg, string emojiStr)
        {
            await msg.AddReactionAsync(Emoji.Parse(emojiStr));
        }
        
        public static IUser? ParseUser(string userToParse, IGuild? guild = null)
        {
            IUser? user = null;
            (userToParse, string discriminator) = userToParse.SplitByChar('#');
            try {
                user = MainHook.instance._client.GetUser(userToParse.ToLower(), discriminator == "" ? null : discriminator);
            } catch (Exception) { try {
                user = MainHook.instance._client.GetUser(ulong.Parse(userToParse));
            } catch (Exception) { try {
                // return MainHook.instance._client.DownloadUsersAsync();
                user = (IUser?)guild?.SearchUsersAsync(userToParse);
            } catch (Exception) {}}}
            return user;
        }
        #endregion

        #region Logging
        public static void Log(string message, LogSeverity severity = LogSeverity.Info)
        {
            Console.ForegroundColor = severity switch {
                LogSeverity.Critical or LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                LogSeverity.Verbose or LogSeverity.Debug => ConsoleColor.DarkGray,
                LogSeverity.Info or _ => ConsoleColor.White,
            };
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void LogWarning(string message = "Warning!") => Log(message, LogSeverity.Warning);
        public static void LogError(string message = "Error!") => Log(message, LogSeverity.Error);
        public static void LogError(System.Exception message) => LogError(message.ToString());
        #endregion

        #region Time
        public enum Time { ms, sec, min, hr, day, week };
        
        // converts from seconds to minutes, hours to ms, minutes to days, etc.
        public static double ConvertTime(double time, Time typeFromTemp = Time.ms, Time typeToTemp = Time.ms) {
            if (typeToTemp == typeFromTemp) return time;
            var typeFrom = (int)typeFromTemp;
            var typeTo = (int)typeToTemp;
            // Console.WriteLine("typeFrom : " + typeFrom);
            // Console.WriteLine("typeTo : " + typeTo);

            int modifier = 1;
            int[] converts = [1000, 60, 60, 24, 7];

            for (var i = Math.Min(typeFrom, typeTo); i < Math.Max(typeFrom, typeTo); i++) {
                modifier *= converts[i];
            }

            return (typeFrom > typeTo) ? (time * modifier) : (time / modifier);
        }

        public static long CurrentTime() => new DateTimeOffset().ToUnixTimeMilliseconds();
        #endregion
    }
}
