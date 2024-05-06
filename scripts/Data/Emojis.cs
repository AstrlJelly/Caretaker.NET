using System;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace CaretakerNET.ExternalEmojis
{
    public static class Emojis
    {
        public static readonly string[] Trues = [
            "1149936632468885555",
            "1149936634079494164",
            "1149936635153235988",
            "1149936635887222806",
            "1149936637686587414",
            "1149936639460782210",
            "1149936640878456872",
            "1149936644326182922",
            "1149936647585153054",
            "1149936651221618760",
            "1149936657877970975",
            "1149936657877970975",
            "1149936802027814923",
            "1149936805626531881",
            "1149936809107791872",
            "1149936812794597417",
            "1149936816074522674",
            "1149936819497082900",
            "1149936837679398963",
            "1149936840212758539",
        ];
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
