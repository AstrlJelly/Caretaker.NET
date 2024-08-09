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

namespace CaretakerNET
{
    public class MainHook
    {
        // gets called when program is run; starts async loop
        private static Task Main(string[] args) => Instance.MainAsync(args);

        public readonly static MainHook Instance = new();
        private bool isReady;
        private bool testingMode;

        private readonly DiscordSocketClient Client;
        private readonly StringBuilder LogBuilder = new();
        public readonly EvalContext CompileContext = new();
        public ChatGPT.Net.ChatGpt CaretakerChat { get; private set; } = new("");
        public Config @Config { get; private set; } = new();

        public readonly long StartTime;

        public bool KeepRunning { get; private set; } = true;

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
            Config = await ConfigHandler.Load();
            SoundDeck.PlayOneShotClip("startup");
            CommandHandler.Init();
            CaretakerCore.Discord.Init(Client);

            CaretakerChat = new(Config.CaretakerChatApiToken, new() {
                BaseUrl = "https://api.pawan.krd/cosmosrp/v1/",
                Model = "cosmosrp"
            });

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

            await Client.LoginAsync(TokenType.Bot, Config.Token);
            await Client.StartAsync();

            // keep running until Stop() is called
            while (KeepRunning) await Task.Delay(100);

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
                CaretakerConsole.Init(tempTalkingChannels);

                await PersistenceHandler.Instance.Load();

                SaveLoop();
                await Client.SetActivityAsync(new Game(
                    ">playtest",
                    ActivityType.Playing,
                    ActivityProperties.None,
                    "smiles a little bit :)"
                ));

                // ts.UpdateTitle("Ready!");
                CaretakerConsole.Instance.CurrentTitleState.Status = "Ready!";
            }
        }


        public void Stop() => KeepRunning = false;
        private async Task OnStop()
        {
            List<Task> toWait = [
                Client.StopAsync(),
                SaveLogFile(),
                Task.Delay(1000)
            ];
            if (PersistenceHandler.Instance.GuildData.Count > 0) {
                toWait.Add(PersistenceHandler.Instance.Save());
            }
            CaretakerConsole.Instance.TypingState?.Dispose();
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


        private async void SaveLoop()
        {
            await Task.Delay(60000);
            _ = PersistenceHandler.Instance.Save();
            _ = SaveLogFile();
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

        private (string, string)? HandleCountAndChain(IUserMessage msg)
        {
            // var u = PersistenceHandler.Instance.GetUserData(msg);
            var s = PersistenceHandler.Instance.GetGuildData(msg);
            if (s == null) return null;
            if (s.Count != null && msg.Channel.Id == s.Count?.Channel?.Id) { // count stuff
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
            } else if (s.Chain != null && msg.Channel.Id == s.Chain.Channel?.Id) { // chain stuff
                // return null;
            }
            return null;
        }

        private (string, string)? HandleBoardGame(IUserMessage msg)
        {
            var u = PersistenceHandler.Instance.GetUserData(msg);
            var s = PersistenceHandler.Instance.GetGuildData(msg);
            if (s == null) return null;
            BoardGame? game = s.CurrentGame;
            if (game == null || game.Players == null || msg.Channel.Id != game.PlayingChannelId) return null;

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
                            if (c4.CaretakerPlayer == BoardGame.Player.None) {
                                c4.SwitchPlayers();
                            }
                            string board = c4.DisplayBoard(out var win);
                            if (win.Tie) {
                                board += $"it's a tie...";
                                s.CurrentGame = null;
                            } else {
                                if (win.WinningPlayer == BoardGame.Player.None) {
                                    board += $"{c4.GetEmoji(otherPlayerId)}{UserPingFromID(otherPlayerId)}, it's your turn!" + forfeit;
                                } else {
                                    u.AddWin(typeof(ConnectFour));
                                    PersistenceHandler.Instance.GetUserData(otherPlayerId).AddLoss(typeof(ConnectFour));
                                    board += $"{c4.GetEmoji(player)}{UserPingFromID(playerId)} won!";
                                    s.CurrentGame = null;
                                }
                            }
                            if (c4.CaretakerPlayer != BoardGame.Player.None) {
                                c4.DoCaretakerMove(msg.Channel);
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

        public async Task MessageHandler(IUserMessage msg)
        {
            var u = PersistenceHandler.Instance.GetUserData(msg);
            var s = PersistenceHandler.Instance.GetGuildData(msg);
            string prefix = s?.Prefix ?? DEFAULT_PREFIX;

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
                    u.Timeout += 1500;
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

                    // make sure the message is only logged if it's in any of the talking channels, or is the current talking channel.
                    // also don't log it if it's caretaker, i handle that specially.
                    if (msg.Author.Id != CARETAKER_ID &&
                        (CaretakerConsole.Instance.CurrentTalkingChannel?.Id == cId || CaretakerConsole.Instance.IsChannelTalkingChannel(cId)))
                    {
                        LogMessage(msg);
                    }

                    Func<(string, string)?>[] funcs = [
                        () => HandleCountAndChain(msg),
                        () => HandleBoardGame(msg),
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