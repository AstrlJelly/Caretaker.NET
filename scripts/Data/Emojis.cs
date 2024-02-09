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
        public const string True = "<:true:1108529480579948735>";
        public const string TalkingFlower = "<a:talkingflower:1178157875248496720>";
    }

    public static class ParsedEmojis
    {
        public readonly static IEmote Smide = Emoji.Parse(Emojis.Smide);
        public readonly static IEmote Sab = Emoji.Parse(Emojis.Sab);
        public readonly static IEmote True = Emoji.Parse(Emojis.True);
        public readonly static IEmote TalkingFlower = Emoji.Parse(Emojis.TalkingFlower);
    }
}
