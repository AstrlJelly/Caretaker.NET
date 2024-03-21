global using static CaretakerNET.Core.Caretaker;
global using CaretakerNET.Core;

using System;
using System.Threading;
using System.Diagnostics;

using Discord;
using Discord.WebSocket;
using Discord.Webhook;

using CaretakerNET.Games;
using CaretakerNET.Commands;
using CaretakerNET.ExternalEmojis;
using org.mariuszgromada.math.mxparser;

namespace CaretakerNET
{
    public class MainHook
    {
        // gets called when program is run; starts async loop
        public readonly static MainHook instance = new();
        static Task Main(string[] args) => instance.MainAsync(args);
        private bool keepRunning = true;
        private bool isReady;

        public readonly DiscordSocketClient Client;
        public ITextChannel? TalkingChannel;
        private Dictionary<ulong, GuildPersist> GuildData = [];
        private Dictionary<ulong, UserPersist> UserData = [];

        public readonly long StartTime;
        public bool DebugMode = false;
        public bool TestingMode = false;

        public static readonly HashSet<ulong> TrustedUsers = [
            438296397452935169, // @astrljelly
            752589264398712834, // @antoutta
        ];
        public static readonly HashSet<ulong> BannedUsers = [
            // 468933965110312980, // @lifinale, it's been long enough
            // 476021507420586014, // @vincells, thin ice
        ];

        private MainHook()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 50,
            });

            Client.Log += ClientLog;
            Client.MessageReceived += MessageReceivedAsync;
            Client.ButtonExecuted += ButtonHandler;
            Client.Ready += ClientReady;

            AppDomain.CurrentDomain.UnhandledException += async delegate {
                await Client.StopAsync();
                await Save();
            };

            StartTime = new DateTimeOffset().ToUnixTimeMilliseconds();
        }

        private static Task ClientLog(LogMessage message)
        {
            InternalLog($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}", false, message.Severity);
            return Task.CompletedTask;
        }

        private async Task MainAsync(string[] args)
        {
            CommandHandler.Init();
            DebugMode = args.Contains("debug") || args.Contains("-d");
            TestingMode = args.Contains("testing") || args.Contains("-t");

            // ChangeConsoleTitle("Starting...");

            // login and connect with token (change to config json file?)
            await Client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await Client.StartAsync();

            // i literally have no clue why but this breaks Console.ReadLine(). it even breaks BACKSPACE fsr
            // Console.TreatControlCAsInput = true;

            // keep running until Stop() is called
            while (keepRunning) {
                string? readLine = Console.ReadLine();
                if (readLine is "c" or "exit") {
                    Stop();
                    break;
                }
                if (!string.IsNullOrEmpty(readLine)) {
                    // // the channel just doesn't wanna be IIntegrationChannel?? idk
                    // Log(talkingChannel is IIntegrationChannel);
                    // if (talkingChannel is IIntegrationChannel channel)
                    // {
                    //     var webhooks = await channel.GetWebhooksAsync();
                    //     var webhook = webhooks.FirstOrDefault(x => x.Name == "AstrlJelly");
                    //     Log(webhook?.Name);
                    //     if (webhook == null) {
                    //         using Stream ajIcon = File.Open("./ajIcon.png", FileMode.Open);
                    //         webhook = await channel.CreateWebhookAsync("AstrlJelly", ajIcon);
                    //     }
                    //     await new DiscordWebhookClient(webhook).SendMessageAsync(readLine);
                    // }

                    Task<IUserMessage>? message = null;
                    if (TalkingChannel != null) message = TalkingChannel.SendMessageAsync(readLine);
                    if (readLine.StartsWith(PREFIX) && message != null) {
                        _ = MessageHandler(await message);
                        // _ = Task.Run(async () => {
                        //     var msg = await message;
                        //     (string command, string parameters) = readLine[PREFIX.Length..].SplitByFirstChar(' ');
                        //     (Command? com, parameters) = CommandHandler.ParseCommand(command, parameters);
                        //     if (com != null) {
                        //         await CommandHandler.DoCommand(msg, com, parameters);
                        //     }
                        // });
                    }
                }
            }
            // async programming is funny
            Task stop = Client.StopAsync();
            if (GuildData.Count > 0 && GuildData.Count > 0) {
                await Save();
            }
            await stop;
        }

        public async Task ClientReady()
        {
            if (!isReady) {
                await Client.DownloadUsersAsync(Client.Guilds);

                LogDebug("GUILDS : " + string.Join(", ", Client.Guilds));
                // long ass namespace
                org.mariuszgromada.math.mxparser.License.iConfirmNonCommercialUse("hmmmmm");

                TalkingChannel = Client.ParseGuild("1113913617608355992")?.ParseChannel("1113944754460315759");

                await Load();

                isReady = true;
            }
        }

        public void Stop() => keepRunning = false;

        public async Task Save()
        {
            await Persist.SaveGuilds(GuildData);
            await Persist.SaveUsers(UserData);
        }

        public async Task Load()
        {
            GuildData = await Persist.LoadGuilds();
            UserData = await Persist.LoadUsers();
            CheckGuildData();
            foreach (var key in GuildData.Keys) {
                if (key > 0) {
                    if (GuildData[key] == null) {
                        GuildData[key] = new(key);
                    }
                } else {
                    GuildData.Remove(key);
                }
                if (!GuildData.TryGetValue(key, out GuildPersist? value) && value != null) {
                    value.Init(Client);
                }
            }
        }

        public void CheckGuildData()
        {
            Parallel.ForEach(Client.Guilds, (guild) => 
            {
                if (!GuildData.TryGetValue(guild.Id, out GuildPersist? value) || value == null) {
                    GuildData[guild.Id] = new GuildPersist(guild.Id);
                    // GuildData.Add(guild.Id, new GuildPersist(guild.Id));
                }
                // if (TryGetGuildData(guild.Id, out var data) /*&& data != null*/) {
                //     data.Init(Client);
                // }
            });
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

        public UserPersist GetUserData(IUserMessage msg) => GetUserData(msg.Author.Id);
        public UserPersist GetUserData(IUser user) => GetUserData(user.Id);
        public UserPersist GetUserData(ulong id)
        {
            if (!UserData.TryGetValue(id, out UserPersist? value)) {
                value = new UserPersist();
                UserData.Add(id, value);
            }
            return value;
        }

        public async Task ButtonHandler(SocketMessageComponent component)
        {
            switch (component.Data.CustomId)
            {
                case "show-cards":
                    await component.RespondAsync($"hi {component.User.Username}", ephemeral: true);
                break;
            }
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            // make sure the message is a user sent message, and output a new msg variable
            if (message is SocketUserMessage msg) {
                _ = MessageHandler(msg);
            }

            return Task.CompletedTask;
        }

        private async Task MessageHandler(IUserMessage msg)
        {
            // also make sure it's not a bot/not banned
            if (msg.Author.IsBot) return;

            if (msg.Content.StartsWith(PREFIX)) {
                bool banned = BannedUsers.Contains(msg.Author.Id); // check if user is banned
                bool testing = TestingMode && !TrustedUsers.Contains(msg.Author.Id); // check if testing, and if user is valid
                LogDebug(msg.Author.Username + " banned? : " + banned);
                LogDebug("testing mode on? : " + TestingMode);

                (string command, string parameters) = msg.Content[PREFIX.Length..].SplitByFirstChar(' ');
                if (string.IsNullOrEmpty(command) || banned || testing) return;
                (Command? com, parameters) = CommandHandler.ParseCommand(command, parameters);

                // HasPerms returns true if GetGuild is null! make sure there's no security concerns there
                if (com == null) return;
                if (!com.HasPerms(msg) && !TrustedUsers.Contains(msg.Author.Id)) {
                    await msg.Reply("you don't have the perms to do this!");
                    return;
                }

                var u = GetUserData(msg);
                if (u.timeout > DateNow()) {
                    LogDebug("timeout : " + u.timeout);
                    LogDebug("DateNow() : " + DateNow());
                    await msg.ReactAsync("🕒");
                    u.timeout += 1000;
                    return;
                }

                // var typing = msg.Channel.EnterTypingState();
                try {
                    Stopwatch sw = new();
                    sw.Start();
                    await CommandHandler.DoCommand(msg, com, parameters);
                    u.timeout = DateNow() + com.Timeout;
                    sw.Stop();
                    LogDebug($"parsing {PREFIX}{command} command took {sw.ElapsedMilliseconds} ms");
                } catch (Exception error) {
                    LogError(error);
                    await msg.Reply(error.Message, false);
                }
                // typing.Dispose();
            } else {
                if (TryGetGuildData(msg, out GuildPersist? s) && s != null) {
                    ulong cId = msg.Channel.Id;

                    // talking channel stuff
                    if (cId == TalkingChannel?.Id) {
                        LogInfo($"{msg.Author.GlobalName} : {msg.Content}", true);
                    }

                    Func<(string, string)?>[] funcs = [
                        // count/chain stuff
                        () => {
                            if (s.count != null && cId == s.count?.Channel?.Id) { // count stuff
                                GuildPersist.CountPersist count = s.count!;
                                // duplicate check
                                if (s.count.LastCountMsg?.Author.Id == msg.Author.Id) {
                                    s.count.Reset(false);
                                    return ("💀", "you can't count twice in a row! try again");
                                }
                                // parse number from message
                                var math = new Expression(msg.Content);
                                double newNumberTemp = math.calculate();
                                if (double.IsNaN(newNumberTemp)){
                                    math = new Expression(msg.Content.Split(' ')[0]);
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
                            } else if (s.chain != null && cId == s.chain.Channel?.Id) { // chain stuff
                                // return null;
                            }
                            return null;
                        },

                        // board game stuff
                        () => {
                            BoardGame? game = s.CurrentGame;
                            if (game == null || game.Players == null) return null;

                            ulong playerId = msg.Author.Id;
                            BoardGame.Player player = game.GetWhichPlayer(msg.Author.Id);
                            if (player == BoardGame.Player.None) return null;

                            ulong otherPlayerId = game.Players[player == BoardGame.Player.Two ? 0 : 1];
                            BoardGame.Player otherPlayer = game.GetWhichPlayer(otherPlayerId);
                            (string move, string columnStr) = msg.Content.SplitByFirstChar(' ');
                            move = move.ToLower();
                            if (move is "forfeit") {
                                game.StartForfeit(playerId);
                            }
                            // forfeit stuff
                            string forfeit = "";
                            if (game.Turns >= game.EndAt) {
                                forfeit = $"wowwww looks like {UserPingFromID(game.ForfeitPlayer)} gives up... and {UserPingFromID(game.ForfeitPlayer == playerId ? otherPlayerId : playerId)}";
                            } else if (game.EndAt < int.MaxValue) {
                                forfeit = $" ({(game.EndAt - game.Turns)} turns left.)";
                            }

                            switch (s.CurrentGame)
                            {
                                case ConnectFour c4:
                                    if (game.Turns >= game.EndAt) {
                                        string board = c4.DisplayBoard();
                                        s.CurrentGame = null;
                                        return ("✅", board + forfeit);
                                    }
                                    switch (move.ToLower())
                                    {
                                        case "go": {
                                            if (!game.IsCurrentPlayer(player)) return null;
                                            int column = int.Parse(columnStr[0].ToString()) - 1;
                                            if (!c4.AddToColumn(column, player)) {
                                                return ("❌", "");
                                            }
                                            c4.SwitchPlayers();
                                            var win = c4.WinCheck(player);
                                            string board = c4.DisplayBoard(win);
                                            if (!win.Tie) {
                                                if (win.WinningPlayer == BoardGame.Player.None) {
                                                    board += $"{c4.GetEmoji(game.GetWhichPlayer(otherPlayerId))}{UserPingFromID(otherPlayerId)}, it's your turn!" + forfeit;
                                                } else {
                                                    board += $"{c4.GetEmoji(player)}{UserPingFromID(msg.Author.Id)} won!";
                                                    s.CurrentGame = null;
                                                }
                                            } else {
                                                board += $"it's a tie...";
                                                s.CurrentGame = null;
                                            }
                                            if (!string.IsNullOrEmpty(forfeit)) board += forfeit;
                                            return ("✅", board);
                                        }
                                        case "refresh": {
                                            return ("✅", c4.DisplayBoard() + $"here you go :) it's {c4.GetEmoji(player)}{UserPingFromID(msg.Author.Id)}'s turn right now");
                                        }
                                        default: return null;
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
                            await msg.ReactAsync(emojiToParse);
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