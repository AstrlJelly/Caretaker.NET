using System;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace CaretakerNET.ExternalEmojis
{
    public static class Emojis
    {
        public const string Smide = "<:smide:1187013043306119231>";
        public const string Sab = "<:sab:1136749005913739424>";
        public const string Cride = "<:cride:1230681547716296725>";
        public const string True = "<:true:1108529480579948735>";
        public const string TalkingFlower = "<a:talkingflower:1178157875248496720>";
        // public const string Adofai = "<a:adofaiofai:1232003215592259676>";
        public const string Adofai = "<a:adofaiofai:1232003215592259676>";
    }

    public static class ParsedEmojis
    {
        public static IEmote Smide { get; private set; } = Emoji.Parse(Emojis.Smide);
        public static IEmote Sab { get; private set; } = Emoji.Parse(Emojis.Sab);
        public static IEmote True { get; private set; } = Emoji.Parse(Emojis.True);
        public static IEmote TalkingFlower { get; private set; } = Emoji.Parse(Emojis.TalkingFlower);
        public static IEmote Adofai { get; private set; } = Emoji.Parse(Emojis.TalkingFlower);
    }
}
