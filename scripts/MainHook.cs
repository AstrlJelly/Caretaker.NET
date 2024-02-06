using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
// using Microsoft.CodeAnalysis.CSharp.Scripting;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using CaretakerNET.Core;
using CaretakerNET.Commands;
using CaretakerNET.Games;
using CaretakerNET.ExternalEmojis;
using Discord.Webhook;

namespace CaretakerNET
{
    public class MainHook
    {
        // gets called when program is ran; starts async loop
        public readonly static MainHook instance = new();
        static Task Main(string[] args) => instance.MainAsync(args);
        private bool keepRunning = true;

        public readonly DiscordSocketClient Client;
        public SocketGuildChannel? talkingChannel;
        private Dictionary<ulong, GuildPersist> GuildData = [];
        private Dictionary<ulong, UserPersist> UserData = [];

        private readonly long startTime;
        public const string PREFIX = ">";
        public bool DebugMode = false;
        public bool TestingMode = false;

        private static readonly HashSet<ulong> TrustedUsers = [
            438296397452935169, // @astrljelly
            752589264398712834, // @antoutta
        ];
        private static readonly HashSet<ulong> BannedUsers = [
            468933965110312980, // @lifinale
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

            AppDomain.CurrentDomain.UnhandledException += async delegate { 
                await Client.StopAsync();
                await Save();
            };

            startTime = new DateTimeOffset().ToUnixTimeMilliseconds();
        }

        private static Task ClientLog(LogMessage message)
        {
            Caretaker.InternalLog($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}", false, message.Severity);
            return Task.CompletedTask;
        }

        private async Task MainAsync(string[] args)
        {
            CommandHandler.Init();
            DebugMode = args.Contains("debug") || args.Contains("-d");
            TestingMode = args.Contains("testing") || args.Contains("-t");

            // Caretaker.ChangeConsoleTitle("Starting...");

            // login and connect with token (change to config json file?)
            await Client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await Client.StartAsync();

            await Client.DownloadUsersAsync(Client.Guilds);

            await Load();

            // i literally have no clue why but this breaks Console.ReadLine(). it even breaks BACKSPACE fsr
            // Console.TreatControlCAsInput = true;

            while (true) // apparently guilds have to load, so wait for that
            {
                bool good = Client.Guilds.Count > 0;
                foreach (var guild in Client.Guilds) {
                    if (string.IsNullOrEmpty(guild.Name)) {
                        good = false;
                    }
                }
                if (good) {
                    Caretaker.LogDebug("Guilds : " + string.Join(", ", Client.Guilds.Select(x => x.Name)));
                    break;
                }
                await Task.Delay(50);
            }

            talkingChannel = (SocketGuildChannel?)Client.ParseGuild("1113913617608355992")?.ParseChannel("1113944754460315759");

            // keep running until Stop() is called
            while (keepRunning) {
                string? readLine = Console.ReadLine();
                if (readLine == "c") {
                    Stop();
                    break;
                }
                if (!string.IsNullOrEmpty(readLine)) {
                    Caretaker.Log(talkingChannel is IIntegrationChannel);
                    if (talkingChannel is IIntegrationChannel channel)
                    {
                        var webhooks = await channel.GetWebhooksAsync();
                        var webhook = webhooks.FirstOrDefault(x => x.Name == "AstrlJelly");
                        Caretaker.Log(webhook?.Name);
                        if (webhook == null) {
                            using Stream ajIcon = File.Open("./ajIcon.png", FileMode.Open);
                            webhook = await channel.CreateWebhookAsync("AstrlJelly", ajIcon);
                        }
                        await new DiscordWebhookClient(webhook).SendMessageAsync(readLine);
                    }

                    // Task<IUserMessage>? message = null;
                    // if (talkingChannel != null) message = talkingChannel.SendMessageAsync(readLine);
                    // if (readLine.StartsWith(PREFIX) && message != null) {
                    //     _ = Task.Run(async () => {
                    //         var msg = await message;
                    //         (string command, string parameters) = readLine[PREFIX.Length..].SplitByFirstChar(' ');
                    //         // Caretaker.Log(msg.Content);
                    //         await CommandHandler.ParseCommand(msg, command, parameters);
                    //     });
                    // }
                }
            }
            await Client.StopAsync();
            await Save();
            // await Task.Delay(Timeout.Infinite);
        }

        public void Stop() => keepRunning = false;

        public async Task Save()
        {
            await Persist.SaveGuilds(GuildData);
            await Persist.SaveUsers(UserData);
        }

        public async Task Load()
        {
            GuildData = await Persist.LoadGuildss();
            UserData = await Persist.LoadUsers();
        }

        public void CheckGuildData()
        {
            Parallel.ForEach(Client.Guilds, (guild) => 
            {
                if (!GuildData.TryGetValue(guild.Id, out GuildPersist? value) || value == null) {
                    GuildData.Add(guild.Id, new GuildPersist());
                }
            });
            // foreach (var guild in Client.Guilds)
            // {
            //     if (guildData.TryGetValue(guild.Id, out GuildPersist? value) && value != null) {
            //         value!.CheckClassVariables(value);
            //     } else {
            //         guildData.Add(guild.Id, new GuildPersist());
            //     }
            // }
        }

        // returns null if not in guild, like if you're in dms
        public bool TryGetGuildData(IUserMessage msg, out GuildPersist? data) {
            data = GetGuildData(msg.GetGuild()?.Id ?? 0);
            return data != null;
        }
        public GuildPersist? GetGuildData(IUserMessage msg) => GetGuildData(msg.GetGuild()?.Id ?? 0);
        public GuildPersist GetGuildData(IGuild guild) => GetGuildData(guild.Id)!;
        public GuildPersist? GetGuildData(ulong id)
        {
            if (id != 0) { 
                if (!GuildData.TryGetValue(id, out GuildPersist? value)) {
                    value = new GuildPersist();
                    GuildData.Add(id, value);
                }
                return value;
            } else {
                return null;
            }
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

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // wrap in Task.Run() so that multiple commands can be handled at the same time
            _ = Task.Run(async () => {
                if (message.Channel.Id == talkingChannel?.Id && message.Author.Id != 1182009469824139395) {
                    Caretaker.LogInfo($"{message.Author.GlobalName} : {message.Content}", true);
                }

                // make sure the message is a user sent message, and output a new msg variable
                // also make sure it's not a bot/not banned
                if ((message is not SocketUserMessage msg) || msg.Author.IsBot) return;

                if (msg.Content.StartsWith(PREFIX)) {
                    bool banned = BannedUsers.Contains(msg.Author.Id);
                    bool testing = TestingMode && !TrustedUsers.Contains(msg.Author.Id);
                    Caretaker.LogDebug(msg.Author.Username + " banned? : " + banned);
                    Caretaker.LogDebug("testing mode on? : " + TestingMode);

                    (string command, string parameters) = msg.Content[PREFIX.Length..].SplitByFirstChar(' ');
                    if (string.IsNullOrEmpty(command) || banned || testing) return;

                    var u = GetUserData(msg);
                    if (u.timeout > Caretaker.DateNow()) {
                        Caretaker.LogDebug("timeout : " + u.timeout);
                        Caretaker.LogDebug("Caretaker.DateNow() : " + Caretaker.DateNow());
                        await msg.EmojiReact("🕒");
                        u.timeout += 500;
                        return;
                    }

                    var typing = msg.Channel.EnterTypingState();
                    try {
                        Stopwatch sw = new();
                        sw.Start();
                        await CommandHandler.ParseCommand(msg, command, parameters);
                        UserData[msg.Author.Id].timeout = Caretaker.DateNow() + 1000;
                        sw.Stop();
                        Caretaker.LogDebug($"parsing {PREFIX}{command} command took {sw.ElapsedMilliseconds} ms");
                    } catch (Exception error) {
                        await msg.Reply(error.Message, false);
                    }
                    typing.Dispose();
                }
            });
            await Task.CompletedTask; 
        }
    }
}