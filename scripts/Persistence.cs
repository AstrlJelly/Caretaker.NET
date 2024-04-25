using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Discord;
using Discord.WebSocket;

using CaretakerNET.Games;
using CaretakerNET.Economy;

namespace CaretakerNET
{
    public static class Voorhees
    {
        private const string GUILD_PATH = "./persist/guild.json";
        private const string USER_PATH  = "./persist/user.json";
        private readonly static JsonSerializerOptions serializerSettings = 
            new JsonSerializerOptions {
                WriteIndented = true,
                IncludeFields = true,
            };

        public static async Task SaveGuilds(Dictionary<ulong, GuildPersist> dictionary) => await Save(GUILD_PATH, dictionary);
        public static async Task SaveUsers(Dictionary<ulong, UserPersist> dictionary)   => await Save(USER_PATH, dictionary);

        private static async Task Save<T>(string path, Dictionary<ulong, T> objectToSave)
        {
            LogInfo($"Start saving to {path}...", true);
            if (!Directory.Exists("./persist")) Directory.CreateDirectory("./persist");
            string? serializedDict = JsonSerializer.Serialize(objectToSave, serializerSettings);
            await File.WriteAllTextAsync(path, serializedDict); // creates file if it doesn't exist
            LogInfo("Saved!", true);
        }

        public static async Task<Dictionary<ulong, GuildPersist>> LoadGuilds() => await Load<Dictionary<ulong, GuildPersist>>(GUILD_PATH);
        public static async Task<Dictionary<ulong, UserPersist>>  LoadUsers()  => await Load<Dictionary<ulong, UserPersist>>(USER_PATH);

        // tells the compiler that T should always implement new(), so that i can construct a default dictionary. i love C#
        private static async Task<T> Load<T>(string path) where T : new()
        {
            LogInfo($"Start loading from {path}...", true);
            if (!Directory.Exists("./persist")) Directory.CreateDirectory("./persist");
            if (!File.Exists(path)) File.Create(path);
            string jsonFileStr = await File.ReadAllTextAsync(path);

            if (string.IsNullOrEmpty(jsonFileStr)) return new();

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
    public class UserPersist
    {
        public ulong UserId;
        [JsonInclude] bool IsInServer = true;
        public string Username = "";
        // null when starting. much easier to check and smarter than making another bool
        public long? Balance = null;
        [JsonIgnore] public bool HasStartedEconomy => Balance != null;
        public const long START_BAL = 500;
        public List<EconomyHandler.Item> Inventory = [];
        public long Timeout = 0;

        // the name of the game won or lost
        public List<string> Wins = [];
        public List<string> Losses = [];

        public void Init(DiscordSocketClient client, ulong userId)
        {
            UserId = userId;
            var user = client.GetUser(userId);
            if (user != null) {
                Username = user.Username;
                IsInServer = true;
            } else {
                IsInServer = false;
            }
        }
 
        // public bool HasStartedEconomy()
        // {
        //     if (Balance != null) return false;
        //     Balance = START_BAL;
        //     return true;
        // }

        public bool StartEconomy(IUserMessage msg, bool fromCom = true)
        {
            if (Balance != null) return false;
            string[] startReplies = [
                "ohhhh you haven't used the economy before, have you?",
                "it's time for you to start gambling! :D",
                "ur CRAZY poor right now",
                "wow you somehow have no money. that's crazy.",
            ];
            string reply = startReplies.GetRandom()! + $"\nhere's {START_BAL} jells, you need it.";
            if (fromCom) reply += " (also, try that command again.)";

            _ = msg.Reply(reply);
            Balance = START_BAL;
            return true;
        }

        public void AddWin(Type whichGame) => Wins.Add(whichGame.Name);
        public void AddLoss(Type whichGame) => Losses.Add(whichGame.Name);
        public float WinLossRatio()
        {
            return Wins.Count / Losses.Count;
        }
    }
}

