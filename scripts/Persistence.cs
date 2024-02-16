using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

using CaretakerNET.Games;
using CaretakerNET.Core;

namespace CaretakerNET
{
    public static class Persist
    {
        const string GUILD_PATH = "./persist/guild.json";
        const string USER_PATH  = "./persist/user.json";

        public static async Task SaveGuilds(this Dictionary<ulong, GuildPersist> dictionary) => await Save(GUILD_PATH, dictionary);
        public static async Task SaveUsers(this Dictionary<ulong, UserPersist> dictionary)   => await Save(USER_PATH, dictionary);

        public static async Task<Dictionary<ulong, GuildPersist>> LoadGuilds() => await Load<Dictionary<ulong, GuildPersist>>(GUILD_PATH);
        public static async Task<Dictionary<ulong, UserPersist>>  LoadUsers()  => await Load<Dictionary<ulong, UserPersist>>(USER_PATH);

        private static async Task Save<T>(string path, Dictionary<ulong, T> objectToSave)
        {
            Caretaker.LogInfo($"Start saving to {path}...", true);
            string? serializedDict = JsonConvert.SerializeObject(objectToSave, Formatting.Indented
            , new JsonSerializerSettings 
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }
            );
            await File.WriteAllTextAsync(path, serializedDict);
            Caretaker.LogInfo("Saved!", true);
        }

        // tells the compiler that T always implements new(), so that i can construct a default dictionary. i love C#
        private static async Task<T> Load<T>(string path) where T : new()
        {
            Caretaker.LogInfo($"Start loading from {path}...", true);
            var jsonFileStr = await File.ReadAllTextAsync(path);
            // Caretaker.LogDebug("jsonFileStr : " + jsonFileStr);
            if (!string.IsNullOrEmpty(jsonFileStr)) {
                try {
                    var deserializedDict = JsonConvert.DeserializeObject<T>(jsonFileStr);
                    if (deserializedDict != null) {
                        // Caretaker.LogTemp("deserializedDict : " + deserializedDict);
                        Caretaker.LogInfo("Loaded!", true);
                        return deserializedDict;
                    } else {
                        throw new Exception($"Load (\"{path}\") failed!");
                    }
                } catch (System.Exception err) {
                    Caretaker.LogError(err, true);
                    throw;
                }
            } else {
                return new();
            }
        }
    }

    public class GuildPersist(ulong guildId)
    {
        public class Chain()
        {
            [JsonIgnore] public ITextChannel? Channel;
            internal ulong ChannelId;
            public string? Current;
            public int ChainLength;
            public string? PrevChain;
            public ulong LastChainer;
            public int AutoChain;

            public void Init(SocketGuild guild)
            {
                Channel = (ITextChannel?)guild.GetChannel(ChannelId);
            }
        }

        public class Convo
        {
            [JsonIgnore] private ITextChannel? convoChannel;
            [JsonIgnore] private ITextChannel? replyChannel;
            [JsonProperty] internal ulong convoChannelId;
            [JsonProperty] internal ulong replyChannelId;
            [JsonIgnore] internal ITextChannel? ConvoChannel { get => convoChannel; set {
                convoChannel = value;
                if (value != null) {
                    convoChannelId = value.Id;
                }
            }}
            [JsonIgnore] internal ITextChannel? ReplyChannel { get => replyChannel; set {
                replyChannel = value;
                if (value != null) {
                    replyChannelId = value.Id;
                }
            }}

            public void Init(SocketGuild guild)
            {
                ConvoChannel = (ITextChannel?)guild.GetChannel(convoChannelId);
                ReplyChannel = (ITextChannel?)guild.GetChannel(replyChannelId);
            }
        }

        public class Count(ITextChannel? channel = null, int current = 0, int prevNumber = 0, int highestNum = 0, IUserMessage? lastCountMsg = null)
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
            [JsonProperty] internal ulong channelId;
            [JsonProperty] internal ulong lastCountMsgChannelId, lastCountMsgId;
            [JsonIgnore] public ITextChannel? Channel { get => channel; set {
                channel = value;
                if (value != null) {
                    channelId = value.Id;
                }
            }}
            [JsonIgnore] public IUserMessage? LastCountMsg { get => lastCountMsg; set {
                lastCountMsg = value;
                if (value != null) {
                    lastCountMsgChannelId = value.Channel.Id;
                    lastCountMsgId = value.Id;
                }
            }}
            public int Current = current;
            public int PrevNumber = prevNumber;
            public int HighestNum = highestNum;

            public async void Init(SocketGuild guild)
            {
                Channel = (ITextChannel?)guild.GetChannel(channelId);
                if (guild.GetChannel(lastCountMsgChannelId) is ITextChannel channel && channel != null) {
                    LastCountMsg = (IUserMessage?)await channel.GetMessageAsync(lastCountMsgId);
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
        public Count count = new();
        public Chain chain = new();
        public Convo convo = new();
        // public List<SlowMode> slowModes = [];
        public Dictionary<ulong, int> slowModes = []; // channel id and timer
        public ConnectFour? connectFour = null;

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

