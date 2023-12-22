using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Caretaker.ExternalEmojis
{
    // public enum Emojis // can't use strings with this, unlike typescript
    // {
        
    // }
    public static class Emojis
    {
        public const string Smide = "<:smide:1187013043306119231>";
        public const string True = "<:true:1108529480579948735>";
    }
}
