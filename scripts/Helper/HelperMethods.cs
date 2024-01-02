using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Caretaker.Helper
{
    public static class Helpers
    {
        public static bool IsIndexValid<T>(this List<T>? list, int index) => list != null && index < list.Count;
        public static bool IsIndexValid(this Array? array, int index) => array != null && index < array.Length;

        // public static T TryGet<T>(this List<T>? list, int index) => list.IsIndexValid(index) ? list?[index] : default;
        public static SocketGuild GetGuild(this SocketMessage msg) {
            if (msg.Channel is not SocketGuildChannel chnl) { // pattern matching is freaky but i like it
                throw new Exception($"msg with id ${msg.Id} apparently... didn't have a channel?? idk.");
            }
            return chnl.Guild;
        }

        public static string EnumName(this Enum whichEnum, int enumElement) {
            Type enumType = whichEnum.GetType();
            int max = Enum.GetNames(enumType).Length;
            return Enum.GetName(enumType, Math.Clamp(enumElement, 0, max)) ?? "";
        }

        enum Time { ms, sec, min, hr, day, week };
        
        // converts from seconds to minutes, hours to ms, minutes to days, etc.
        private static double ConvertTime(double time, Time typeFromTemp = Time.ms, Time typeToTemp = Time.ms) {
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

            Console.WriteLine(modifier);

            return (typeFrom > typeTo) ? (time * modifier) : (time / modifier);
        }

        // don't wanna type this every time lol (and i swear it performs ever so slightly better)
        private static string ReplaceAll(this string stringToReplace, string oldStr, string newStr) {
            return string.Join(newStr, stringToReplace.Split(oldStr));
        }

        // for loops with callbacks inside of them, just so i never mess up my backwards looping
        private static void IterateForward()
        {
            
        }

        private static void IterateBackward()
        {
            
        }
    }
}
