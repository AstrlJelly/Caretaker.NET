using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Discord;
using Discord.WebSocket;

using CaretakerNET.Games;


namespace CaretakerNET
{
    public static class Persist
    {
        private const string GUILD_PATH = "./persist/guild.json";
        private const string USER_PATH  = "./persist/user.json";
        private readonly static JsonSerializerOptions serializerSettings = 
            new() {
                WriteIndented = true,
                IncludeFields = true,
            };

        public static async Task SaveGuilds(this Dictionary<ulong, GuildPersist> dictionary) => await Save(GUILD_PATH, dictionary);
        public static async Task SaveUsers(this Dictionary<ulong, UserPersist> dictionary)   => await Save(USER_PATH, dictionary);

        private static async Task Save<T>(string path, Dictionary<ulong, T> objectToSave)
        {
            LogInfo($"Start saving to {path}...", true);
            string? serializedDict = JsonSerializer.Serialize(objectToSave, serializerSettings);
            await File.WriteAllTextAsync(path, serializedDict);
            LogInfo("Saved!", true);
        }

        public static async Task<Dictionary<ulong, GuildPersist>> LoadGuilds() => await Load<Dictionary<ulong, GuildPersist>>(GUILD_PATH);
        public static async Task<Dictionary<ulong, UserPersist>>  LoadUsers()  => await Load<Dictionary<ulong, UserPersist>>(USER_PATH);

        // tells the compiler that T always implements new(), so that i can construct a default dictionary. i love C#
        private static async Task<T> Load<T>(string path) where T : new()
        {
            LogInfo($"Start loading from {path}...", true);
            if (!File.Exists(path)) File.Create(path);
            var jsonFileStr = await File.ReadAllTextAsync(path);
            if (!string.IsNullOrEmpty(jsonFileStr)) {
                try {
                    var deserializedDict = JsonSerializer.Deserialize<T>(jsonFileStr, serializerSettings);
                    if (deserializedDict != null) {
                        // LogTemp("deserializedDict : " + deserializedDict);
                        LogInfo("Loaded!", true);
                        return deserializedDict;
                    } else {
                        throw new Exception($"Load (\"{path}\") failed!");
                    }
                } catch (Exception err) {
                    LogError(err, true);
                    throw;
                }
            } else {
                return new();
            }
        }
    }

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
            // [JsonProperty] internal ulong? channelId;
            // [JsonProperty] internal ulong? lastCountMsgChannelId, lastCountMsgId;
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

        // public class SlowMode(ITextChannel? channel, int timer)
        // {
        //     public ITextChannel? channel = channel;
        //     public int timer = timer;
        // }

        // public Dictionary<string, dynamic> CommandData;
        public ulong guildId = guildId;
        public CountPersist count = new();
        public ChainPersist chain = new();
        public ConvoPersist convo = new();
        // public List<SlowMode> slowModes = [];
        public Dictionary<ulong, int> slowModes = []; // channel id and timer
        // public ConnectFour? connectFour = null;
        public BoardGame? CurrentGame = null;

        public void Init(DiscordSocketClient client)
        {
            var guild = client.GetGuild(guildId);
            count.Init(guild);
            chain.Init(guild);
            convo.Init(guild);
        }


        // public Persist() {
        //     // CommandData = [];
        //     Count = new();
        //     Chain = new();
        //     Convo = new();
        //     SlowModes = [];
        // }
    }
    public class UserPersist()
    {
        public class Item(string name, string desc, float price)
        {
            public string name = name;
            public string desc = desc;
            public float price = price;
        }

        public List<Item> inventory = [];
        public long timeout = 0;
    }
}

