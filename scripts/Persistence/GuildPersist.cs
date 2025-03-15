using System.Text.Json.Serialization;
using CaretakerNET.Games;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET.Persistence
{
    public class GuildPersist(ulong guildId)
    {
        public class ChainPersist()
        {
            [JsonIgnore] public ITextChannel? Channel;
            // [JsonProperty] internal ulong ChannelId;
            internal ulong ChannelId;
            public string? Current;
            public int ChainLength;
            public string? PrevChain;
            public ulong LastChainer;
            public int AutoChain;

            public void Init(SocketGuild guild)
            {
                // Log("GUILD : " + guild);
                Channel = (ITextChannel?)guild.GetChannel(ChannelId);
            }
        }

        public class ConvoPersist
        {
            [JsonIgnore] private ITextChannel? convoChannel = null;
            [JsonIgnore] private ITextChannel? replyChannel = null;
            // [JsonProperty] internal ulong? convoChannelId;
            // [JsonProperty] internal ulong? replyChannelId;
            internal ulong? convoChannelId;
            internal ulong? replyChannelId;
            [JsonIgnore] internal ITextChannel? ConvoChannel { get => convoChannel; set {
                convoChannel = value;
                convoChannelId = value?.Id;
            }}
            [JsonIgnore] internal ITextChannel? ReplyChannel { get => replyChannel; set {
                replyChannel = value;
                replyChannelId = value?.Id;
            }}

            public void Init(SocketGuild guild)
            {
                // null checks
                if (convoChannelId is ulong id1) ConvoChannel = (ITextChannel?)guild.GetChannel(id1);
                if (replyChannelId is ulong id2) ReplyChannel = (ITextChannel?)guild.GetChannel(id2);
            }
        }

        public class CountPersist(ITextChannel? channel = null, int current = 0, int prevNumber = 0, int highestNum = 0, IUserMessage? lastCountMsg = null)
        {
            public void Reset(bool fullReset)
            {
                if (HighestNum < Current) HighestNum = Current;
                PrevNumber = Current;
                Current = 0;
                if (fullReset) LastCountMsg = null;
            }
            [JsonIgnore] internal ITextChannel? channel = channel;
            [JsonIgnore] internal IUserMessage? lastCountMsg = lastCountMsg;
            internal ulong? channelId;
            internal ulong? lastCountMsgChannelId, lastCountMsgId;
            [JsonIgnore] public ITextChannel? Channel { get => channel; set {
                channel = value;
                channelId = value?.Id;
            }}
            [JsonIgnore] public IUserMessage? LastCountMsg { get => lastCountMsg; set {
                lastCountMsg = value;
                lastCountMsgId = value?.Id;
                lastCountMsgChannelId = value?.Channel.Id;
            }}
            public int Current = current;
            public int PrevNumber = prevNumber;
            public int HighestNum = highestNum;

            public async void Init(SocketGuild guild)
            {
                if (channelId is ulong chanId) Channel = (ITextChannel?)guild.GetChannel(chanId);
                if (lastCountMsgChannelId is not ulong msgChanId) return;
                if (guild.GetChannel(msgChanId) is ITextChannel channel && channel != null) {
                    if (lastCountMsgId is ulong msgId) {
                        LastCountMsg = (IUserMessage?)await channel.GetMessageAsync(msgId);
                    }
                }
            }
        }

        public ulong GuildId = guildId;
        public string GuildName = "";
        public string Prefix = DEFAULT_PREFIX;
        public CountPersist Count = new();
        public ChainPersist Chain = new();
        public ConvoPersist Convo = new();
        public BoardGame? CurrentGame = null;

        public void Init(DiscordSocketClient client, ulong guildId)
        {
            GuildId = guildId;
            SocketGuild? guild = client.GetGuild(guildId);
            if (guild == null) {
                // LogError($"guild was null!! am i still in the guild with id \"{guildId}\"?");
                LogDebug($"guild data with id \"{guildId}\" was null.");
                return;
            }
            GuildName = guild.Name;
            Count.Init(guild);
            Chain.Init(guild);
            Convo.Init(guild);
        }
    }
}