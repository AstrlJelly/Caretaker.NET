// using System;
// using System.Collections;
// using System.Diagnostics;
// using System.Text;

using System;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace CaretakerCore
{
    public static class Core
    {
        #region String
        // don't wanna type this every time lol (and i swear it performs ever so slightly better)
        public static string ReplaceAll(this string stringToReplace, string oldStr, string newStr) => string.Join(newStr, stringToReplace.Split(oldStr));
        public static string ReplaceAll(this string stringToReplace, char oldStr, char newStr) => string.Join(newStr, stringToReplace.Split(oldStr));

        /// <summary>
        /// A safer "this[]", which simply returns default if out of bounds.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The IEnumerable to use to get at <paramref name="index"/>.</param>
        /// <param name="index"></param>
        /// <returns>The item at <paramref name="index"/>, if <paramref name="enumerable"/> isn't null, and there is a value at that index.</returns>
        public static T? TryGet<T>(this IEnumerable<T> enumerable, int index)
        {
            return enumerable != null && enumerable.IsIndexValid(index) ? enumerable.ElementAt(index) : default;
        }

        /// <summary>
        /// Splits a string into two parts using an index.
        /// </summary>
        /// <param name="stringToSplit">The string to split into two.</param>
        /// <param name="index">The index to split at, i.e "Split" at index 2 would become ("Spl", "it")</param>
        /// <returns>The split string in a tuple form.</returns>
        public static (string, string) SplitByIndex(this string stringToSplit, int index)
        {
            if (stringToSplit.IsIndexValid(index) || index > -1) {
                return (stringToSplit[..index], stringToSplit[(index + 1)..]);
            } else {
                return (stringToSplit, "");
            }
        }

        /// <summary>
        /// Splits a string into two parts using the first instance of a character. 
        /// </summary>
        /// <param name="stringToSplit">The string to split into two.</param>
        /// <param name="splitChar">The char to split at, i.e "Split" with 'l' would become ("Sp", "it")</param>
        /// <returns>The split string in a tuple form.</returns>
        public static (string, string) SplitByFirstChar(this string stringToSplit, char splitChar)
        {
            int index = stringToSplit.IndexOf(splitChar);

            return SplitByIndex(stringToSplit, index);
        }

        /// <summary>
        /// Splits a string into two parts using the last instance of a character.
        /// </summary>
        /// <param name="stringToSplit">The string to split into two.</param>
        /// <param name="splitChar">The char to split at, i.e "Split This" with 'i' would become ("Split thi", "s")</param>
        /// <returns>The split string in a tuple form.</returns>
        public static (string, string) SplitByLastChar(this string stringToSplit, char splitChar)
        {
            int index = stringToSplit.LastIndexOf(splitChar);

            return SplitByIndex(stringToSplit, index);
        }

        /// <summary>
        /// Splits a string into mutiple parts using an array of indexes.
        /// </summary>
        /// <param name="stringToSplit">The string to split into two.</param>
        /// <param name="indexes">The indexes to split at. <para/> i.e "Split This" at index 5, and 7 would become ["Split", " T", "his"]</param>
        /// <returns>A List of the split strings, with Count <paramref name="indexes"/>.Length + 1.</returns>
        public static List<string> SplitByIndexes(this string stringToSplit, params int[] indexes)
        {
            int maxLength = stringToSplit.Length;
            List<int> newIndexes = [0, ..indexes];
            newIndexes.RemoveAll(x => x >= maxLength);
            newIndexes.Add(maxLength);
            List<string> newStrings = [];
            for (int i = 0; i < newIndexes.Count - 1; i++) {
                newStrings.Add(stringToSplit[newIndexes[i]..newIndexes[i + 1]]);
            }
            return newStrings;
        }

        /// <summary>
        /// Checks if a string is equal to any in an array of strings.
        /// </summary>
        /// <param name="stringToMatch">The string to check against <paramref name="stringsToMatch"/></param>
        /// <param name="stringsToMatch"> The strings to check against <paramref name="stringToMatch"/></param>
        /// <returns><i>True</i> if any strings match <paramref name="stringToMatch"/>. <i>False</i> otherwise.</returns>
        public static bool Match(this string stringToMatch, params string[] stringsToMatch)
        {
            for (int i = 0; i < stringsToMatch.Length; i++)
            {
                if (stringToMatch.Equals(stringsToMatch[i], StringComparison.CurrentCultureIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Boolean
        public static bool FlipCoin(double chance = 0.5)
        {
            return new Random().NextDouble() < chance;
        }
        #endregion 

        #region List
        /// <summary>
        /// Checks if an index is valid.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="index"></param>
        /// <returns><i>True</i> if index is valid. <i>False</i> otherwise.</returns>
        public static bool IsIndexValid<T>(this IEnumerable<T>? enumerable, int index) => enumerable != null && index >= 0 && index < enumerable.Count();

        /// <summary>
        /// Gets an element in a 2D IEnumerable using two indexes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="i">Index 1</param>
        /// <param name="j">Index 2</param>
        /// <returns>An element of type <typeparamref name="T"/> from the 2D <paramref name="enumerable"/></returns>
        public static T? GetFromIndexes<T>(this IEnumerable<IEnumerable<T>> enumerable, int i, int j)
        {
            return enumerable.IsIndexValid(i) && enumerable.ElementAt(i).IsIndexValid(j) ? enumerable.ElementAt(i).ElementAt(j) : default;
        }

        /// <summary>
        /// Splits a list using an index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listToSplit"></param>
        /// <param name="index"></param>
        /// <returns>Two lists, split from <paramref name="listToSplit"/> using <paramref name="index"/>.</returns>
        public static (List<T>, List<T>?) SplitByIndex<T>(this List<T> listToSplit, int index)
        {
            if (listToSplit.IsIndexValid(index)) {
                return (listToSplit[..index], listToSplit[(index + 1)..]);
            } else {
                return (listToSplit, default);
            }
        }

        /// <summary>
        /// Gets a random element from <paramref name="enumerable"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns>An element from <paramref name="enumerable"/>.</returns>
        public static T? GetRandom<T>(this IEnumerable<T> enumerable)
        {
            var random = new Random();
            int count = enumerable.Count();
            return count > 0 ? enumerable.ElementAt(random.Next(count)) : default;
        }

        /// <summary>
        /// Inline FindIndex(), with the result being an "out" argument.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="match"></param>
        /// <param name="index"></param>
        /// <returns><i>True</i> if the index was found. <i>False</i> otherwise.</returns>
        public static bool TryFindIndex<T>(this IEnumerable<T> enumerable, Func<T, int, bool> match, out int index) 
        {
            index = -1;
            foreach (T item in enumerable)
            {
                index++;
                if (match.Invoke(item, index)) return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="match"></param>
        /// <param name="index"></param>
        /// <returns><i>True</i> if the index was found. <i>False</i> otherwise.</returns>
        public static IEnumerable<T> GetEnumerableFromDimension<T>(this T[,] array, int dimension) 
        {
            return Enumerable.Range(dimension, array.GetLength(1)).Select(i => array[dimension, i]);
        }
        #endregion

        #region Enum
        public static string EnumName(this Type enumType, int index)
        {
            // Type enumType = whichEnum.GetType();
            int max = Enum.GetNames(enumType).Length - 1;
            int newIndex = Math.Clamp(index, 0, max);
            if (newIndex != index) {
                LogWarning($"EnumName() had {index} put into it; defaulted to {newIndex} instead.");
            }
            return Enum.GetName(enumType, newIndex)!;
        }
        #endregion

        #region Console
        public enum LogSeverity
        {
            Critical,
            Error,
            Warning,
            Info,
            Verbose,
            Debug,
        }

        public static Action<string>? OnLog;
        public static void InternalLog(object? message, bool time = false, LogSeverity severity = LogSeverity.Info)
        {
            Console.ForegroundColor = severity switch {
                LogSeverity.Critical or LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning                       => ConsoleColor.Yellow,
                LogSeverity.Verbose or LogSeverity.Debug  => ConsoleColor.DarkGray,
                LogSeverity.Info or _                     => ConsoleColor.White,
            };
            string log = message?.ToString() ?? "null";
            if (time) log = $"[{CurrentTime()}] " + log;
            Console.WriteLine(log);
            Console.ResetColor();
            OnLog?.Invoke(log);
        }

        // m : message, t : add timestamp?
        public static void LogInfo(object? m = null, bool t = false) { InternalLog(m, t, LogSeverity.Info); }
        public static void LogWarning(object? m = null, bool t = false) { InternalLog(m ?? "Warning!", t, LogSeverity.Warning); }
        public static void LogError(object? m = null, bool t = false) { InternalLog(m ?? "Error!", t, LogSeverity.Error); }
        [Obsolete("THIS SHOULD BE TEMPORARY!!")]
        public static void Log(object? m = null, bool t = false) { InternalLog(m, t, LogSeverity.Info); }

        // doesn't quite work? still testing
        public static void ClearConsoleLine()
        {
            // set cursor to beginning to actually overwrite stuff
            Console.SetCursorPosition(0, Console.CursorTop);
            // clears current line by just spamming space
            Console.Write(new string(' ', Console.BufferWidth));
            // puts cursor back to where it should be
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            // int currentLineCursor = Console.CursorTop;
            // Console.SetCursorPosition(0, currentLineCursor);
            // Console.Write(new string(' ', Console.WindowWidth)); 
            // Console.SetCursorPosition(0, currentLineCursor);
        }
        #endregion

        #region Time
        public enum Time { Ms, Sec, Min, Hour, Day, Week };
        
        // converts from seconds to minutes, hours to ms, minutes to days, etc.
        public static double ConvertTime(double time, Time typeFromTemp = Time.Ms, Time typeToTemp = Time.Ms)
        {
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

        public static string CurrentTime() => DateTime.Now.ToString("HH:mm:ss.fff tt");

        public static long DateNow() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        #endregion

        #region Async
        [Obsolete("not really anything rn and might forever be nothing")]
        public static void Sleep()
        {
            
        } 
        #endregion

        #region Misc
        public static bool IsNull<T>(T check, out T result)
        {
            result = check;
            return result == null;
        }
        #endregion
    }
}
