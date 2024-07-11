global using static CaretakerCore.Core;
global using static CaretakerCore.Discord;
global using static CaretakerNET.Core.Caretaker;

using System;
using System.Text;
using System.Diagnostics;

using Discord;
using Discord.WebSocket;

using CaretakerNET.Games;
using CaretakerNET.Commands;
using CaretakerNET.ExternalEmojis;

using org.mariuszgromada.math.mxparser;
using Z.Expressions;
using CaretakerNET.Audio;
using CaretakerNET.Persistence;
using CaretakerNET.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CaretakerNET
{
    public class MainHook
    {
        // gets called when program is run; starts async loop
        private static Task Main(string[] args) => instance.MainAsync(args);

        public readonly static MainHook instance = new();
        private bool isReady;
        private bool testingMode;

        public readonly DiscordSocketClient Client;
        public readonly CaretakerConsole ConsoleHandler = new();
        public readonly EvalContext CompileContext = new();
        public readonly StringBuilder LogBuilder = new();
        public Config config = new();
        public Dictionary<ulong, GuildPersist> GuildData { get; private set; } = [];
        public Dictionary<ulong, UserPersist> UserData { get; private set; } = [];

        public readonly long StartTime;

        public bool KeepRunning = true;

        public static readonly HashSet<ulong> TrustedUsers = [
            CARETAKER_ID,       // should be obvious
            438296397452935169, // @astrljelly
            752589264398712834, // @antoutta
        ];
        public static readonly HashSet<ulong> BannedUsers = [
            // 391459218034786304, // @untitled.com, https://discord.com/channels/1171893658149191750/1229197791784468550/1229197801301479495
            // 468933965110312980, // @lifinale, it's been long enough
            // 476021507420586014, // @vincells, thin ice
        ];

        private MainHook()
        {
            Client = (new DiscordSocketClient(
                new DiscordSocketConfig {
                    GatewayIntents = GatewayIntents.All,
                    LogLevel = Discord.LogSeverity.Info,
                    MessageCacheSize = 50,
                }
            ));

            Client.Log += ClientLog;
            Client.MessageReceived += MessageReceivedAsync;
            Client.Ready += ClientReady;

            OnLog += log => {
                LogBuilder.AppendLine(log);
            };

            Console.CancelKeyPress += delegate { Client.StopAsync(); };
            AppDomain.CurrentDomain.UnhandledException += async delegate { await OnStop(); };

            StartTime = DateNow();
        }

        private static Task ClientLog(LogMessage message)
        {
            InternalLog($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}", false, (CaretakerCore.Core.LogSeverity)message.Severity);
            return Task.CompletedTask;
        }

        private async Task MainAsync(string[] args)
        {
            config = await ConfigHandler.Load();
            SoundDeck.PlayOneShotClip("startup");
            CommandHandler.Init();
            CaretakerCore.Discord.Init(Client);

            ts.UpdateTitle();

            testingMode = args.Contains("testing") || args.Contains("-t");
            // string name = nameof(CaretakerNET);

            foreach (var directory in new string[] { "persist", "temp", "logs" }) {
                if (!Directory.Exists("./" + directory)) {
                    Directory.CreateDirectory("./" + directory);
                }
            }
            
            // might wanna make this async?
            foreach (var filePath in Directory.GetFiles("./logs"))
            {
                var createTime = new DateTimeOffset(File.GetCreationTime(filePath)).ToUnixTimeMilliseconds();
                var discardTime = DateNow() - ConvertTime(1, Time.Day);
                if (createTime < discardTime) {
                    _ = Task.Run(() => File.Delete(filePath));
                }
            }
            // PrivatesPath = File.ReadAllText("./privates_path.txt");
            // login and connect with token (change to config json file?)
            await Client.LoginAsync(TokenType.Bot, config.Token);
            await Client.StartAsync();

            ConsoleHandler.StartReadingKeys();

            // keep running until Stop() is called
            while (KeepRunning);

            await OnStop();
        }

        public async Task ClientReady()
        {
            // isReady is only used here, just to make sure it doesn't init a billion times
            if (!isReady) {
                isReady = true;

                await Client.DownloadUsersAsync(Client.Guilds);

                LogDebug("GUILDS : " + string.Join(", ", Client.Guilds));
                // org.mariuszgromada.math.mxparser
                License.iConfirmNonCommercialUse("hmmmmm");

                (ulong, ulong)[] talkingChannelIds = [
                   (CARETAKER_CENTRAL_ID, 1189820692569538641), // caretaker central, caretaker-net
                    (SPACE_JAMBOREE_ID,   1230684176211251291), // space jamboree,    caretaker-central-lite
                    (1113913617608355992, 1113944754460315759), // routerheads,       bot-commands
                    (1077367474447716352, 1077385447870844971), // no icon,           bot-central
                    (1091542281242279979, 1182368930275274792), // korboy's,          astrl-posting thread
                ];
                List<ITextChannel?> tempTalkingChannels = [];
                foreach (var ids in talkingChannelIds)
                {
                    (ulong guildId, ulong channelId) = ids;
                    var channel = (ITextChannel?)(Client.GetGuild(guildId)?.GetChannel(channelId));
                    if (channel != null) {
                        tempTalkingChannels.Add(channel);
                    } else {
                        LogError($"channel with id {channelId} in guild with id {guildId} was null!");
                    }
                }
                ConsoleHandler.TalkingChannels = [ ..tempTalkingChannels ];
                ConsoleHandler.CurrentTalkingChannel = ConsoleHandler.TalkingChannels[0];

                await Load();

                SaveLoop();
                await Client.SetActivityAsync(new Game(
                    ">playtest",
                    ActivityType.Playing,
                    ActivityProperties.None,
                    "smiles a little bit :)"
                ));

                // ts.UpdateTitle("Ready!");
                ts.Status = "Ready!";
            }
        }

        public class TitleState
        {
            private string status = "Starting...";
            public string Status { get => status; set {
                status = value;
                UpdateTitle();
            }}

            // public void UpdateTitle(string status)
            // {
            //     Status = status;
            //     // UpdateTitle();
            // }

            public void UpdateTitle()
            {
                var ch = instance.ConsoleHandler.CurrentTalkingChannel;
                Console.Title = $"CaretakerNET : {Status} | {ch?.Guild.Name}, {ch?.Name}";
            }
        }
        public readonly TitleState ts = new();

        public void Stop() => KeepRunning = false;
        private async Task OnStop()
        {
            List<Task> toWait = [
                Client.StopAsync(),
                SaveLogFile(),
                Task.Delay(1000)
            ];
            if (GuildData.Count > 0) {
                toWait.Add(Save());
            }
            ConsoleHandler.TypingState?.Dispose();
            Console.ResetColor();

            // async programming is funny
            await Task.WhenAll(toWait);
        }

        // might wanna clear the log builder and only save to one file every session
        // would save building thousands of lines if it ever gets that big
        public async Task SaveLogFile()
        {
            if (!Directory.Exists("./logs")) Directory.CreateDirectory("./logs");
            await File.WriteAllTextAsync($"./logs/log_{DateTime.Now:yy-MM-dd_HH-mm-ss}.txt", LogBuilder.ToString()); // creates file if it doesn't exist
        }

        public async Task Save()
        {
            await Task.WhenAll(
                Voorhees.SaveGuilds(GuildData),
                Voorhees.SaveUsers(UserData)
            );
        }

        public async Task Load()
        {
            var loadGuilds = Task.Run(async () => {
                GuildData = await Voorhees.LoadGuilds();
                // not necessary, takes 5-10 ms to complete at 15 guilds. yeeeouch
                // also yes, i probably need to do that GetGuild().GetUser(), so that i can get caretaker as IGuildUser :(
                GuildData = GuildData.OrderBy(x => Client.GetGuild(x.Key)?.GetUser(CARETAKER_ID)?.JoinedAt?.UtcTicks).ToDictionary();
                LogDebug("GuildData.Count : " + GuildData.Count);
                foreach (var key in GuildData.Keys) {
                    if (key > 0) {
                        if (GuildData[key] == null) {
                            GuildData[key] = new(key);
                        }
                    } else {
                        GuildData.Remove(key);
                    }
                    GuildData[key].Init(Client, key);
                }

            });
            var loadUsers = Task.Run(async () => {
                UserData = await Voorhees.LoadUsers();
                // also not necessary, and also takes 5-10 ms to complete at 76 users
                UserData = UserData.OrderBy(x => x.Value.Username).ToDictionary();
                LogDebug("UserData.Count : " + UserData.Count);
                foreach (var key in UserData.Keys) {
                    if (key > 0) {
                        if (UserData[key] == null) {
                            UserData[key] = new();
                        }
                    } else {
                        UserData.Remove(key);
                    }
                    UserData[key].Init(Client, key);
                }
            });
            await Task.WhenAll(loadGuilds, loadUsers);
        }

        private async void SaveLoop()
        {
            await Task.Delay(60000);
            _ = Save();
            _ = SaveLogFile();
        }

        // data can very much so be null, but i trust any user (basically just me) to never use data if it's null.
        public bool TryGetGuildData(ulong id, out GuildPersist data) 
        {
            data = GetGuildData(id)!;
            return data != null;
        }
        public bool TryGetGuildData(IUserMessage msg, out GuildPersist data) 
        {
            data = GetGuildData(msg)!;
            return data != null;
        }
        // returns null if not in guild, like if you're in dms
        public GuildPersist? GetGuildData(IUserMessage msg) => GetGuildData(msg.GetGuild()?.Id ?? 0);
        public GuildPersist GetGuildData(IGuild guild) => GetGuildData(guild.Id)!;
        public GuildPersist? GetGuildData(ulong id)
        {
            if (id == 0) return null; // return null here, don't wanna try creating a GuildPersist for a null guild

            if (!GuildData.TryGetValue(id, out GuildPersist? value)) {
                value = new GuildPersist(id);
                GuildData.Add(id, value);
            }
            return value;
        }

        public bool TryGetUserData(ulong id, out UserPersist data) 
        {
            data = GetUserData(id);
            return data != null;
        }
        public UserPersist GetUserData(IUserMessage msg) => GetUserData(msg.Author.Id);
        public UserPersist GetUserData(IUser user) => GetUserData(user.Id);
        public UserPersist GetUserData(ulong id)
        {
            if (!UserData.TryGetValue(id, out UserPersist? value)) {
                value = new UserPersist();
                value.Init(Client, id);
                UserData.Add(id, value);
            }
            return value;
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            // make sure the message is a user sent message, and output a new msg variable
            // also make sure it's not a bot
            if (message is IUserMessage msg && !msg.Author.IsBot) {
                _ = MessageHandler(msg);
            }

            return Task.CompletedTask;
        }

        public async Task MessageHandler(IUserMessage msg)
        {
            var u = GetUserData(msg);
            var s = GetGuildData(msg);
            string prefix = s?.Prefix ?? DEFAULT_PREFIX;

            // _ = Task.Run(async () => {
            //     var lowerCont = msg.Content.ToLower();
            //     (string, int)[] findingStrs = [
            //         ("it", -1), ("go", -1),
            //     ];
            //     for (int x = 0; x < lowerCont.Length; x++) // go through every character in msg content
            //     {
            //         for (int y = 0; y < findingStrs.Length; y++) // for every character, go through each string we want to find
            //         {
            //             if (findingStrs[y].Item2 > -1) continue;
            //             string strToFind = findingStrs[y].Item1;
            //             bool matches = true;
            //             for (int z = 0; z < strToFind.Length; z++) // go through each character of the string we want to find
            //             {
            //                 var contChar = lowerCont.TryGet(x + z);
            //                 if ((contChar == default(char) ? ' ' : contChar) != strToFind[z]) {
            //                     matches = false;
            //                     break;
            //                 }
            //             }
            //             if (matches) findingStrs[y].Item2 = x;
            //         }
            //     }

            //     if (
            //         findingStrs[0].Item2 > -1 && findingStrs[1].Item2 > -1 &&
            //         findingStrs[0].Item2 < findingStrs[1].Item2
            //     ) {
            //         // randomizes from 0 - 10 full seconds
            //         await Task.Delay((new Random().Next(11)) * 1000);
            //         _ = msg.React(Emojis.Adofai);
            //     }
            // });

            if (msg.Content.StartsWith(prefix)) {
                bool banned = BannedUsers.Contains(msg.Author.Id); // check if user is banned
                bool testing = testingMode && !TrustedUsers.Contains(msg.Author.Id); // check if testing, and if user is valid
                LogDebug(msg.Author.Username + " banned? : " + banned);
                LogDebug("testing mode on? : " + testingMode);

                (string command, string parameters) = msg.Content[prefix.Length..].SplitByFirstChar(' ');
                if (string.IsNullOrEmpty(command) || banned || testing) return;
                (Command? com, parameters) = CommandHandler.ParseCommand(command, parameters, msg.Author.Id);

                if (com == null) return;

                // HasPerms returns true if GetGuild is null! make sure there's no security concerns there
                if (!com.HasPerms(msg) && !TrustedUsers.Contains(msg.Author.Id)) {
                    await msg.Reply("you don't have the perms to do this!");
                    return;
                }

                if (u.Timeout > DateNow()) {
                    LogDebug("timeout : " + u.Timeout);
                    LogDebug("DateNow() : " + DateNow());
                    u.Timeout += 1000;
                    _ = msg.React("🕒");
                    return;
                }

                // var typing = msg.Channel.EnterTypingState(
                //     new RequestOptions {
                //         Timeout = 1000,
                //     }
                // );
                    try {
                        Stopwatch sw = new();
                        sw.Start();
                        await CommandHandler.DoCommand(msg, com, parameters, command);
                        u.Timeout = DateNow() + com.Timeout;
                        sw.Stop();
                        LogDebug($"parsing {prefix}{command} command took {sw.Elapsed.TotalMilliseconds} ms");
                    } catch (Exception error) {
                        await msg.Reply(error.Message, false);
                        LogError(error);
                    }
                // // might be not enough? or it might just not work.
                // await Task.Delay(1100);
                // typing.Dispose();
            } else {
                if (s != null) {
                    ulong cId = msg.Channel.Id;

                    // talking channel stuff
                    // if (cs.currentTalkingChannel == cId && msg.Author.Id != CARETAKER_ID) {
                    if ((ConsoleHandler.TalkingChannels.Any(c => c?.Id == cId) || ConsoleHandler.CurrentTalkingChannel?.Id == cId) && msg.Author.Id != CARETAKER_ID) {
                        LogMessage(msg);
                    }

                    Func<(string, string)?>[] funcs = [
                        // count/chain stuff
                        () => {
                            if (s.Count != null && cId == s.Count?.Channel?.Id) { // count stuff
                                GuildPersist.CountPersist count = s.Count!;
                                // duplicate check
                                if (s.Count.LastCountMsg?.Author.Id == msg.Author.Id) {
                                    s.Count.Reset(false);
                                    return ("💀", "you can't count twice in a row! try again");
                                }
                                // parse number from message
                                var math = new Expression(msg.Content);
                                double newNumberTemp = math.calculate();
                                if (double.IsNaN(newNumberTemp)){
                                    math.setExpressionString(msg.Content.Split(' ')[0]);
                                    newNumberTemp = math.calculate();
                                }

                                if (double.IsNaN(newNumberTemp)) {
                                    List<char> numbers = [];
                                    bool numberStarted = false;
                                    for (int i = 0; i < msg.Content.Length; i++)
                                    {
                                        if (char.IsNumber(msg.Content[i])) {
                                            numberStarted = true;
                                            numbers.Add(msg.Content[i]);
                                        } else {
                                            if (numberStarted) break;
                                        }
                                    }
                                    if (numbers.Count > 0) {
                                        newNumberTemp = double.Parse(string.Join("", numbers));
                                    } /* else { // maybe? would need to be reworked. i probably won't.
                                        List<int?> computes = [];
                                        int lastWorkingIndex = 0;
                                        for (int i = 0; i < msg.Content.Length; i++)
                                        {
                                            int? tempNumber = (int?)dt.Compute(msg.Content[..i], null);
                                            if (tempNumber != null) lastWorkingIndex = i;
                                            computes.Add(tempNumber);
                                        }
                                        newNumberTemp = computes[lastWorkingIndex];
                                        if (newNumberTemp != null) {
                                            newNumber = (int)newNumberTemp;
                                        }
                                    } */
                                }

                                // if (double.IsNaN(newNumberTemp)) return ("", "hmm");

                                int newNumber = (int)newNumberTemp;

                                // is the new number one more than the last?
                                if (newNumber == count.Current + 1) {
                                    count.Current++;
                                    count.LastCountMsg = msg;
                                    return ("✅", "");
                                } else {
                                    // if the messages are close together, point that out. happens pretty often
                                    var lastMsg = count.LastCountMsg;
                                    long difference = msg.TimeCreated() - (count.LastCountMsg?.TimeCreated() ?? 0);
                                    count.Reset(false);
                                    // currently 800 millseconds; tweak if it's too little or too much
                                    return ("❌", (difference > 800 || lastMsg == null) ?
                                        "aw you're not very good at counting, are you?" : 
                                        "too many cooks in the kitchen!!"
                                    );
                                }
                            } else if (s.Chain != null && cId == s.Chain.Channel?.Id) { // chain stuff
                                // return null;
                            }
                            return null;
                        },

                        // board game stuff
                        () => {
                            BoardGame? game = s.CurrentGame;
                            if (game == null || game.Players == null || cId != game.PlayingChannelId) return null;

                            BoardGame.Player player = game.GetWhichPlayer(msg.Author.Id);
                            if (player == BoardGame.Player.None) return null;

                            (ulong playerId, ulong otherPlayerId) = game.GetPlayerIds(msg.Author.Id);

                            (string move, string columnStr) = msg.Content.SplitByFirstChar(' ');
                            move = move.ToLower();
                            if (move is "forfeit") {
                                game.StartForfeit(playerId);
                            }
                            // forfeit stuff
                            string forfeit = "";
                            if (game.Turns >= game.EndAt) {
                                forfeit = $"wowwww looks like {UserPingFromID(game.ForfeitPlayer)} gives up... and {UserPingFromID(game.ForfeitPlayer == playerId ? otherPlayerId : playerId)} wins!";
                            } else if (game.EndAt < int.MaxValue && game.EndAt > game.EndAt - game.Turns) {
                                forfeit = $" ({(game.EndAt - game.Turns)} turns left.)";
                            }

                            switch (s.CurrentGame)
                            {
                                case ConnectFour c4: {
                                    if (game.Turns >= game.EndAt) {
                                        string board = c4.DisplayBoard();
                                        s.CurrentGame = null;
                                        return ("✅", board + forfeit);
                                    }
                                    switch (move)
                                    {
                                        case "go": {
                                            if (playerId != msg.Author.Id) return null;
                                            int column = int.Parse(columnStr[0].ToString()) - 1;
                                            if (!c4.TryAddToColumn(column, player)) {
                                                return ("❌", "");
                                            }
                                            c4.SwitchPlayers();
                                            string board = c4.DisplayBoard(out var win);
                                            if (win.Tie) {
                                                board += $"it's a tie...";
                                                s.CurrentGame = null;
                                            } else {
                                                if (win.WinningPlayer == BoardGame.Player.None) {
                                                    board += $"{c4.GetEmoji(otherPlayerId)}{UserPingFromID(otherPlayerId)}, it's your turn!" + forfeit;
                                                } else {
                                                    u.AddWin(typeof(ConnectFour));
                                                    GetUserData(otherPlayerId).AddLoss(typeof(ConnectFour));
                                                    board += $"{c4.GetEmoji(player)}{UserPingFromID(playerId)} won!";
                                                    s.CurrentGame = null;
                                                }
                                            }
                                            return ("✅", board);
                                        }
                                        case "refresh": {
                                            return ("✅", c4.DisplayBoard() + $"here you go :) it's {c4.GetEmoji(player)}{UserPingFromID(playerId)}'s turn right now");
                                        }
                                        default: return null;
                                    }
                                }
                                case Checkers checkers: {
                                    return ("", checkers.DisplayBoard());
                                }
                                default: return null;
                            }
                        } 
                    ];
                    foreach (var func in funcs)
                    {
                        // epic tuples 😄😄😄
                        (string emojiToParse, string reply) = func.Invoke() ?? ("", "");
                        if (!string.IsNullOrEmpty(emojiToParse)) {
                            await msg.React(emojiToParse);
                        }
                        if (!string.IsNullOrEmpty(reply)) {
                            await msg.Reply(reply);
                        }
                    }
                }
            }
        }
    }
}