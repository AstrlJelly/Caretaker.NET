global using static CaretakerCore.Core;
global using static CaretakerCore.Discord;
global using static CaretakerNET.Core.Caretaker;

using System;
using System.Threading;
using System.Diagnostics;

using Discord;
using Discord.WebSocket;
using Discord.Webhook;

using CaretakerNET.Games;
using CaretakerNET.Commands;
using CaretakerNET.ExternalEmojis;

using NAudio.Wave;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using org.mariuszgromada.math.mxparser;
using Z.Expressions;
using CaretakerNet.Audio;

namespace CaretakerNET
{
    public class MainHook
    {
        // gets called when program is run; starts async loop
        static Task Main(string[] args) => instance.MainAsync(args);

        public readonly static MainHook instance = new();
        private bool keepRunning = true;
        private bool isReady;

        public readonly DiscordSocketClient Client;
        public readonly EvalContext CompileContext = new();
        // public ITextChannel? TalkingChannel;
        public Dictionary<ulong, GuildPersist> GuildData { get; private set; } = [];
        public Dictionary<ulong, UserPersist> UserData { get; private set; } = [];

        public readonly long StartTime;
        public bool DebugMode = false;
        public bool TestingMode = false;

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
            Client = new DiscordSocketClient(
                new DiscordSocketConfig {
                    GatewayIntents = GatewayIntents.All,
                    LogLevel = Discord.LogSeverity.Info,
                    MessageCacheSize = 50,
                }
            );

            Client.Log += ClientLog;
            Client.MessageReceived += MessageReceivedAsync;
            Client.Ready += ClientReady;

            // Console.CancelKeyPress                     += async delegate { await OnStop(); };
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
            var af = new AudioFileReader("./sfx/startup.wav");
            var wo = new WaveOutEvent();
            wo.Init(af);
            wo.Play();
            wo.PlaybackStopped += delegate {
                wo.Dispose();
                af.Dispose();
            };
            CommandHandler.Init();
            CaretakerCore.Discord.Init(Client);
            DebugMode = args.Contains("debug") || args.Contains("-d");
            TestingMode = args.Contains("testing") || args.Contains("-t");

            ChangeConsoleTitle("Starting...");
            foreach (var directory in new string[] { "persist", "temp" }) {
                if (!Directory.Exists("./" + directory)) {
                    Directory.CreateDirectory("./" + directory);
                }
            }

            PrivatesPath = File.ReadAllText("./privates_path.txt");
            // login and connect with token (change to config json file?)
            await Client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await Client.StartAsync();

            StartReadingKeys();

            // keep running until Stop() is called
            while (keepRunning);

            await OnStop();
        }

        // private bool chattingMode = false;
        public class ConsoleState()
        {
            public enum States
            {
                Typing,
                SettingChannel,
            }
            public States currentState = States.Typing;
            public IDisposable? typingState = null;
            // public int currentTalkingChannelIndex = 0;
            public ITextChannel?[] TalkingChannels = [];
            public ITextChannel? currentTalkingChannel = null;
            public readonly List<char> consoleLine = [];

            public void ClearLine()
            {
                consoleLine.Clear();
            }
        }
        public readonly ConsoleState cs = new();

        private void StartReadingKeys()
        {
            Dictionary<ConsoleKey, Action> keyActions = new() {
                { ConsoleKey.Escape, Stop },
                { ConsoleKey.Backspace, delegate {
                    List<char> line = cs.consoleLine;
                    if (line.Count > 0) {
                        line.RemoveAt(line.Count - 1);
                        // goes back a character, clears that character with space, then goes back again. i think
                        Console.Write("\b \b");
                    }
                }},
                { ConsoleKey.Enter, async delegate {
                    if (cs.consoleLine.Count > 0) {
                        string line = string.Join("", cs.consoleLine);
                        cs.ClearLine();
                        switch (cs.currentState)
                        {
                            case ConsoleState.States.SettingChannel: {
                                (string cId, string gId) = line.SplitByFirstChar('|');

                                SocketGuild? guild = (SocketGuild?)(string.IsNullOrEmpty(gId) ? cs.currentTalkingChannel?.Guild : Client.ParseGuild(gId));
                                var ch = guild?.ParseChannel(cId);
                                if (ch != null) {
                                    cs.currentTalkingChannel = ch;
                                } else {
                                    LogError($"\nthat was null. is channel \"{cId}\" and guild \"{gId}\" correct?");
                                }

                                ClearConsoleLine();
                                cs.currentState = ConsoleState.States.Typing;
                            } break;
                            default: { // or ConsoleState.Modes.Typing
                                cs.typingState?.Dispose();
                                cs.typingState = null;

                                // with ConsoleKey.Escape, this is kinda redundant. keeping it anyways
                                if (line is "c" or "cancel" or "exit") {
                                    Stop();
                                    return;
                                }

                                var talkingChannel = cs.currentTalkingChannel;
                                if (talkingChannel != null) {
                                    ClearConsoleLine();
                                    LogInfo($"{talkingChannel.Guild.Name}, #{talkingChannel.Name}", true);
                                    LogInfo($"{Client.CurrentUser.Username:14} : {line}");
                                    _ = MessageHandler(await talkingChannel.SendMessageAsync(line));
                                } else {
                                    LogWarning("that's null. ouch");
                                }
                            } break;
                        }
                    }
                }},
            };
            while (keepRunning)
            {
                // Console.SetCursorPosition(); // this looks cool
                while (!Console.KeyAvailable);
                ConsoleKeyInfo key = Console.ReadKey(true);

                // restore when object pooling is done; seems like this lags WAY too much
                int keySfx = new Random().Next(6) + 1;
                AudioPlayer.PlayOneShot($"keyboard/keypress_{keySfx}.wav");

                if (key.Key is >= ConsoleKey.F1 and <= ConsoleKey.F12) {
                    switch (key.Key)
                    {
                        case >= ConsoleKey.F1 and <= ConsoleKey.F5: {
                            int whichChannel = key.Key - ConsoleKey.F1;
                            ITextChannel? ch = null;
                            if (whichChannel >= 0 && whichChannel < cs.TalkingChannels.Length) {
                                ch = cs.TalkingChannels[whichChannel];
                            }
                            
                            if (ch != null) {
                                cs.currentTalkingChannel = ch;
                                LogInfo($"switched to channel \"{ch.Name}\" in guild \"{ch.Guild.Name}\"");
                            } else {
                                string[] logs = [
                                    $"ahh sorry the channel at {whichChannel} is null",
                                    $"talkingChannel at {whichChannel} was null!",
                                    $"LOOOOOSERR... {whichChannel} doesn't exist.",
                                    $"you know the drill. {whichChannel}",
                                ];
                                LogWarning(logs.GetRandom());
                            }
                        } break;
                        case ConsoleKey.F6: {
                            ClearConsoleLine();
                            cs.ClearLine();
                            Console.Write("\"guildId channelId\", please : ");
                            cs.currentState = ConsoleState.States.SettingChannel;
                        } break;
                        case ConsoleKey.F12: {
                            // gwahahaha! this is where i run any kind of code i want
                        } break;
                        default: {

                        } break;
                    }
                } else if (keyActions.TryGetValue(key.Key, out var action)) {
                    action.Invoke();
                } else {
                    if (cs.currentState == ConsoleState.States.Typing) {
                        cs.typingState ??= cs.currentTalkingChannel?.EnterTypingState();
                    }
                    Console.Write(key.KeyChar);
                    cs.consoleLine.Add(key.KeyChar);
                }
            }
        }

        public async Task ClientReady()
        {
            // isReady is only used here, just to make sure it doesn't init a billion times
            if (!isReady) {
                await Client.DownloadUsersAsync(Client.Guilds);

                LogDebug("GUILDS : " + string.Join(", ", Client.Guilds));
                // org.mariuszgromada.math.mxparser
                License.iConfirmNonCommercialUse("hmmmmm");

                (ulong, ulong)[] talkingChannelIds = [
                    (SPACE_JAMBOREE_ID,   1230684176211251291), // space jamboree,    bot-central
                   (CARETAKER_CENTRAL_ID, 1189820692569538641), // caretaker central, caretaker-net
                    (1113913617608355992, 1113944754460315759), // routerheads,       bot-commands
                    (1077367474447716352, 1077385447870844971), // no icon,           bot-central
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
                cs.TalkingChannels = [ ..tempTalkingChannels ];
                cs.currentTalkingChannel = cs.TalkingChannels[0];

                await Load();

                SaveLoop();
                await Client.SetActivityAsync(new Game(
                    ">playtest",
                    ActivityType.Playing,
                    ActivityProperties.None,
                    "smiles a little bit :)"
                ));

                isReady = true;
            }
        }

        public void Stop() => keepRunning = false;
        private async Task OnStop()
        {
            Task[] toWait = [
                Client.StopAsync(),
                GuildData.Count > 0 ? Save() : new Task(()=>{}),
                Task.Delay(1000)
            ];
            cs.typingState?.Dispose();
            Console.ResetColor();

            // async programming is funny
            await Task.WhenAll(toWait);
        }

        public async Task Save()
        {
            await Voorhees.SaveGuilds(GuildData);
            await Voorhees.SaveUsers(UserData);
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

        // static void UpdatePresence()
        // {
        //     DiscordRichPresence discordPresence;
        //     memset(&discordPresence, 0, sizeof(discordPresence));
        //     discordPresence.state = "You can, too!";
        //     discordPresence.details = "Gambling & Making Money";
        //     discordPresence.largeImageKey = "caretaker_central_icon";
        //     discordPresence.largeImageText = "Caretaker Central";
        //     discordPresence.smallImageText = "Caretaker Central";
        //     discordPresence.partyId = "ae488379-351d-4a4f-ad32-2b9b01c91657";
        //     discordPresence.joinSecret = "MTI4NzM0OjFpMmhuZToxMjMxMjM= ";
        //     Discord_UpdatePresence(&discordPresence);
        // }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            // make sure the message is a user sent message, and output a new msg variable
            // also make sure it's not a bot
            if (message is IUserMessage msg && !msg.Author.IsBot) {
                _ = MessageHandler(msg);
            }

            return Task.CompletedTask;
        }

        private async Task MessageHandler(IUserMessage msg)
        {
            var u = GetUserData(msg);
            var s = GetGuildData(msg);
            string prefix = s?.Prefix ?? DEFAULT_PREFIX;

            _ = Task.Run(async () => {
                var lowerCont = msg.Content.ToLower();
                string[] stringsToFind = [
                    "it", "go"
                ];
                int[] indexes = new int[stringsToFind.Length];
                Array.Fill(indexes, -1);
                for (int x = 0; x < lowerCont.Length; x++)
                {
                    for (int y = 0; y < stringsToFind.Length; y++)
                    {
                        string strToFind = stringsToFind[y];
                        bool matches = true;
                        for (int z = 0; z < strToFind.Length; z++)
                        {
                            var contChar = lowerCont.TryGet(x + z);
                            if ((contChar == default(char) ? ' ' : contChar) != strToFind[z]) {
                                matches = false;
                                break;
                            }
                        }
                        if (matches) indexes[y] = x;
                    }
                }

                if (indexes[0] > -1 && indexes[1] > -1 && indexes[0] < indexes[1]) {
                    // randomizes from 0 - 10 full seconds
                    await Task.Delay((new Random().Next(11)) * 1000);
                    _ = msg.React(Emojis.Adofai);
                }
            });

            if (msg.Content.StartsWith(prefix)) {
                bool banned = BannedUsers.Contains(msg.Author.Id); // check if user is banned
                bool testing = TestingMode && !TrustedUsers.Contains(msg.Author.Id); // check if testing, and if user is valid
                LogDebug(msg.Author.Username + " banned? : " + banned);
                LogDebug("testing mode on? : " + TestingMode);

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

                // var typing = msg.Channel.EnterTypingState();
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
                // typing.Dispose();
            } else {
                if (s != null) {
                    ulong cId = msg.Channel.Id;

                    // talking channel stuff
                    // if (cs.currentTalkingChannel == cId && msg.Author.Id != CARETAKER_ID) {
                    if ((cs.TalkingChannels.Any(c => c?.Id == cId) || cs.currentTalkingChannel?.Id == cId) && msg.Author.Id != CARETAKER_ID) {
                        LogInfo($"{msg.GetGuild()!.Name}, #{msg.Channel.Name}", true);
                        LogInfo($"{msg.Author.GlobalName::14} : {msg.Content}");
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
                                            if (!c4.AddToColumn(column, player)) {
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