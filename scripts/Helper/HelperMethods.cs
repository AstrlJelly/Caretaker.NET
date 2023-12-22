using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Caretaker.Helper
{
    public static class Helpers
    {
        public static bool IsIndexValid<T>(this List<T> list, int index) => index < list.Count;
        public static bool IsIndexValid(this Array array, int index) => index < array.Length;

        public static SocketGuild GetGuild(this SocketMessage msg) {
            if (msg.Channel is not SocketGuildChannel chnl) { // pattern matching is freaky but i like it
                throw new Exception($"msg with id ${msg.Id} apparently... didn't have a channel?? idk.");
            }
            return chnl.Guild;
        }
    }
}
