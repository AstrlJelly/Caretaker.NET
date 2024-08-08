using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace CaretakerCore
{
    public static class Discord
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        // private static DiscordSocketClient Client;
        public static DiscordSocketClient Client { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static void Init(DiscordSocketClient c)
        {
            if (Client == null) {
                Client = c;
            } else {
                LogWarning($"attempted to init CaretakerCore.Discord while it was already initialized!");
            }
            
        }

        public static async Task<IUserMessage> Reply(this IUserMessage msg, object rp, bool ping = false)
        {
            string? reply = rp.ToString();
            if (FlipCoin(0.05)) reply = reply?.ReplaceAll("l", "I"); // lol
            return await msg.ReplyAsync(!string.IsNullOrEmpty(reply) ? reply : "the message sent was empty.", allowedMentions: ping ? AllowedMentions.All : AllowedMentions.None);
        }

        public static async Task<IUserMessage> RandomReply(this IUserMessage msg, object[] replies, bool ping = false)
        {
            string? reply = (string?)replies.GetRandom();
            return await msg.Reply(string.IsNullOrEmpty(reply) ? " " : reply, ping);
        }

        public static async Task<IUserMessage> EmbedReply(this IUserMessage msg, Embed embed)
        {
            return await msg.ReplyAsync(embed: embed);
        }

        public static async Task OverwriteMessage(this IUserMessage msg, string newMsg = "")
        {
            var prevContent = msg.Content;
            await msg.ModifyAsync(x => x.Content = $"*{prevContent}*\n{newMsg}");
        }

        public static long TimeCreated(this IUserMessage msg)
        {
            return msg.CreatedAt.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Gets the guild of any message; returns null if not in guild.
        /// </summary>
        /// <param name="msg">The message to get the guild from.</param>
        /// <returns>The guild that the message is in, if it's in a guild. Else returns null.</returns>
        public static SocketGuild? GetGuild(this IUserMessage msg) 
        {
            if (msg.Channel is not SocketGuildChannel chnl) return null;
            return chnl.Guild;
        }

        /// <summary>
        /// Automatically parses an emoji, then reacts to a message with it.
        /// </summary>
        /// <param name="msg">The message to react to.</param>
        /// <param name="emojiStr">An Emoji in string form.</param>
        public async static Task React(this IMessage msg, string emojiStr)
        {
            // gets the emoji without the <> at the ends if it needs it (which is just kinda a guess)
            bool custom = emojiStr.Length >= 2 && emojiStr[0] == '<' && emojiStr[^1] == '>';
            await msg.AddReactionAsync(custom ? new Emoji(emojiStr[1..^1]) : Emoji.Parse(emojiStr));
        }

        public static void LogMessage(IUserMessage msg)
        {
            LogMessage(msg.Author, msg.GetGuild(), msg.Channel, msg.Content);
        }

        public static void LogMessage(IUser user, IGuild? guild, IChannel chnl, string? message = null)
        {
            string username = string.IsNullOrEmpty(user.GlobalName) ? user.Username : user.GlobalName;
            if (username.Length > 16) username = username[..13] + "...";
            LogInfo($"{guild?.Name ?? "DMs"}, #{chnl.Name}", true);
            LogInfo($"{username, 16} : {message}");
        }

        /// <summary>
        /// Gets an id from a reference. <br/>
        /// i.e #general -> 1205328637918707723 or @AstrlJelly -> 438296397452935169
        /// </summary>
        /// <param name="reference">The reference to get an ID from.</param>
        /// <returns>A ulong id of a channel/user</returns>
        private static ulong? IDFromReference(string reference)
        {
            return reference.Length > 0 && reference[0] == '<' && reference.Length >= 2 ? ulong.Parse(reference[2..^1]) : null;
        }

        public static string ChannelLinkFromID(ulong id) => $"<#{id}>";
        public static string UserPingFromID(ulong id) => $"<@{id}>";

        /// <summary>
        /// Gets the first vanity invite from a guild. <br/>
        /// Tries to get an unlimited invite first.
        /// </summary>
        /// <param name="guild">The guild to find an invite in.</param>
        /// <returns>
        /// RestInviteMetadata if invite is found, otherwise null. <br/> 
        /// The invite will be the longest available, if unlimited invite isn't found.
        /// </returns>
        public static async Task<RestInviteMetadata?> GetBestInvite(this SocketGuild guild)
        {
            var tempInvites = await guild.GetInvitesAsync();
            if (tempInvites.Count <= 0) return null;
            var invites = tempInvites.OrderBy(i => i.ExpiresAt?.ToUnixTimeMilliseconds());
            var firstInvite = invites.ElementAt(0);
            return firstInvite.ExpiresAt == null ? firstInvite : invites.Last();
        }

        /// <summary>
        /// Attempts in multiple ways to get a SocketGuild from a string. <br/>
        /// This is done by trying to use <paramref name="guildToParse"/> to grab the guild through its ID or its name.
        /// </summary>
        /// <param name="c"> The bot client to find the guild from</param>
        /// <param name="guildToParse"></param>
        /// <returns></returns>
        public static SocketGuild? ParseGuild(this DiscordSocketClient c, string guildToParse)
        {
            SocketGuild? guild = null;
            Func<string, SocketGuild?>[] actions = [
                x => ulong.TryParse(guildToParse, out ulong id) ? c.GetGuild(id) : null,
                x => c.Guilds.FirstOrDefault(g => Core.Match(guildToParse, g.Name)),
                // x => (SocketGuild?)c.Guilds.FirstOrDefault(ulong.Parse(guildToParse)),
            ];
            for (int i = 0; i < actions.Length; i++) {
                try {
                    guild = actions[i].Invoke(guildToParse);
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
                chan => (ITextChannel)guild.GetChannel(IDFromReference(chan) ?? (ulong.TryParse(chan, out ulong id) ? id : 0)),
                chan => guild.TextChannels.FirstOrDefault(c => c.GetChannelType() == ChannelType.Text && c.Name.Match(chan)),
                chan => guild.ThreadChannels.FirstOrDefault(c => c.Name.Match(chan)),
            ];
            for (int i = 0; i < actions.Length; i++) {
                try {
                    channel = actions[i].Invoke(channelToParse);
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
                delegate { user = c.GetUser(IDFromReference(userToParse) ?? ulong.Parse(userToParse)); },
                delegate { user = c.GetUser(userToParse, discriminator == "" ? null : discriminator); },
                delegate { user = guild?.Users.FirstOrDefault(x => userToParse.Match(x.Nickname, x.GlobalName)); },
            ];
            InternalLog(userToParse);
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

        public class ReactionSubscribe : IDisposable
        {
            private readonly Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task> ReactionAdded;
            public delegate Task<bool> OnReactionAdded(IUserMessage msg, SocketReaction react);
            private readonly OnReactionAdded onReact;

            public bool Destroyed = false;

            // called when "using" is exited
            public void Dispose()
            {
                if (Destroyed || Client == null) return;
                Destroyed = true;
                Client.ReactionAdded -= ReactionAdded;
                GC.SuppressFinalize(this);
            }

            public ReactionSubscribe(OnReactionAdded onReact, IUserMessage msg)
            {
                this.onReact = onReact;
                ReactionAdded = async (msgCache, chnlCache, reaction) => {
                    if (Destroyed || msg.Id != msgCache.Value.Id) return;
                    bool isLast = await onReact.Invoke(msgCache.Value, reaction);
                    if (isLast) Dispose();
                };
                if (Client == null) return;
                Client.ReactionAdded += ReactionAdded;
            }

            ~ReactionSubscribe()
            {
                Dispose();
            }
        }
        public class ComponentSubscribe : IDisposable
        {
            private readonly Func<SocketMessageComponent, Task> OnComponentInteract;
            public delegate Task<bool> OnButtonPressed(SocketMessageComponent args);
            private readonly IUserMessage message;

            public bool Destroyed = false;

            // called when "using" is exited
            public void Dispose()
            {
                if (Destroyed) return;
                if (Client == null) {
                    LogError("client was null! make sure to run the init method in your start method");
                    return;
                }
                Destroyed = true;
                Client.ButtonExecuted -= OnComponentInteract;
                Client.SelectMenuExecuted -= OnComponentInteract;
                message.ModifyAsync(m => {
                    // remove component from message; setting to null doesn't work
                    m.Components = new ComponentBuilder().Build();
                });
                GC.SuppressFinalize(this);
            }

            public ComponentSubscribe(OnButtonPressed onComponentInteract, IUserMessage message)
            {
                this.message = message;
                OnComponentInteract = async args => {
                    if (Destroyed || message.Id != args.Message.Id) return;
                    bool isLast = await onComponentInteract.Invoke(args);
                    if (!args.HasResponded) {
                        // fixes some jank with "application didn't respond"
                        _ = args.DeferAsync();
                    }
                    if (isLast) Dispose();
                };
                if (Client == null) {
                    LogError("client was null! make sure to run Init() in your start method.");
                    return;
                }
                Client.ButtonExecuted += OnComponentInteract;
                Client.SelectMenuExecuted += OnComponentInteract;
            }

            ~ComponentSubscribe()
            {
                Dispose();
            }
        }
    }

}
