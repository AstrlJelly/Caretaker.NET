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
using org.mariuszgromada.math.mxparser;
using System.Data;

namespace CaretakerNET
{
    public class MainHook
    {
        // gets called when program is run; starts async loop
        public readonly static MainHook instance = new();
        static Task Main(string[] args) => instance.MainAsync(args);
        private bool keepRunning = true;

        public readonly DiscordSocketClient Client;
        public ITextChannel? talkingChannel;
        private Dictionary<ulong, GuildPersist> GuildData = [];
        private Dictionary<ulong, UserPersist> UserData = [];

        private readonly long startTime;
        public const string PREFIX = ">";
        public bool DebugMode = false;
        public bool TestingMode = false;

        public static readonly ulong[] TrustedUsers = [
            438296397452935169, // @astrljelly
            752589264398712834, // @antoutta
        ];
        public static readonly ulong[] BannedUsers = [
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
            Client.Ready += ClientReady;

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

            // i literally have no clue why but this breaks Console.ReadLine(). it even breaks BACKSPACE fsr
            // Console.TreatControlCAsInput = true;

            // while (!keepRunning) { }

            // keep running until Stop() is called
            while (keepRunning) {
                string? readLine = Console.ReadLine();
                if (readLine == "c") {
                    Stop();
                    break;
                }
                if (!string.IsNullOrEmpty(readLine)) {
                    // // the channel just doesn't wanna be IIntegrationChannel?? idk
                    // Caretaker.Log(talkingChannel is IIntegrationChannel);
                    // if (talkingChannel is IIntegrationChannel channel)
                    // {
                    //     var webhooks = await channel.GetWebhooksAsync();
                    //     var webhook = webhooks.FirstOrDefault(x => x.Name == "AstrlJelly");
                    //     Caretaker.Log(webhook?.Name);
                    //     if (webhook == null) {
                    //         using Stream ajIcon = File.Open("./ajIcon.png", FileMode.Open);
                    //         webhook = await channel.CreateWebhookAsync("AstrlJelly", ajIcon);
                    //     }
                    //     await new DiscordWebhookClient(webhook).SendMessageAsync(readLine);
                    // }

                    Task<IUserMessage>? message = null;
                    if (talkingChannel != null) message = talkingChannel.SendMessageAsync(readLine);
                    if (readLine.StartsWith(PREFIX) && message != null) {
                        _ = Task.Run(async () => {
                            var msg = await message;
                            (string command, string parameters) = readLine[PREFIX.Length..].SplitByFirstChar(' ');
                            // Caretaker.Log(msg.Content);
                            await CommandHandler.ParseCommand(msg, command, parameters);
                        });
                    }
                }
            }
            await Client.StopAsync();
            await Save();
            // await Task.Delay(Timeout.Infinite);
        }

        public async Task ClientReady()
        {
            await Client.DownloadUsersAsync(Client.Guilds);

            Caretaker.LogDebug("GUILDS : " + string.Join(", ", Client.Guilds));
            await Load();


            talkingChannel = Client.ParseGuild("1113913617608355992")?.ParseChannel("1113944754460315759");
            keepRunning = true;
            // return Task.CompletedTask;
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
            // int i = 0;
            foreach (var key in GuildData.Keys) {
                if (key <= 0) {
                    GuildData.Remove(key);
                } else {
                    try {
                        GuildData[key].Init(Client);
                    } catch (System.Exception error) {
                        GuildData[key] = new(key);
                        Caretaker.LogError(error);
                        throw;
                    }
                }
                
                // i++;
            }
        }

        public void CheckGuildData()
        {
            Parallel.ForEach(Client.Guilds, (guild) => 
            {
                if (!GuildData.TryGetValue(guild.Id, out GuildPersist? value) || value == null) {
                    GuildData.Add(guild.Id, new GuildPersist(guild.Id));
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
        public bool TryGetGuildData(IUserMessage msg, out GuildPersist? data) 
        {
            data = GetGuildData(msg.GetGuild()?.Id ?? 0);
            return data != null;
        }
        public GuildPersist? GetGuildData(IUserMessage msg) => GetGuildData(msg.GetGuild()?.Id ?? 0);
        public GuildPersist GetGuildData(IGuild guild) => GetGuildData(guild.Id)!;
        public GuildPersist? GetGuildData(ulong id)
        {
            if (id != 0) { 
                if (!GuildData.TryGetValue(id, out GuildPersist? value)) {
                    value = new GuildPersist(id);
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
                if (message is not SocketUserMessage msg) return;

                // make sure the message is a user sent message, and output a new msg variable
                // also make sure it's not a bot/not banned
                if (msg.Author.IsBot) return;

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
                } else {
                    if (TryGetGuildData(msg, out GuildPersist? s) && s != null) {
                        Dictionary<ulong, Func<SocketUser, (string, string)?>> actions = new() {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            { talkingChannel?.Id ?? 0, _ => {
                                Caretaker.LogInfo($"{msg.Author.GlobalName} : {msg.Content}", true);
                                return null;
                            } },
                            { s.count?.Channel?.Id ?? 0, author => {
                                // s.count will not be null here but the compiler doesn't know that 😢
                                var count = s.count;
                                // duplicate check
                                if (s.count?.LastCountMsg?.Author.Id == author.Id && !author.IsTrusted()) {
                                    s.count.Reset(false);
                                    return ("💀", "you can't count twice in a row! try again");
                                }
                                
                                // parse number from message
                                DataTable dt = new();
                                int? newNumberTemp = (int?)dt.Compute(msg.Content, null) ?? (int?)dt.Compute(msg.Content.Split(' ')[0], null);
                                int newNumber = 0;

                                if (newNumberTemp == null) {
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
                                        newNumber = int.Parse(string.Join("", numbers));
                                    } /* else { // maybe? would need to be reworked.
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
                                } else {
                                    newNumber = (int)newNumberTemp;
                                }

                                // is the new number one more than the last?
                                if (newNumber == count.Current + 1) {
                                    count.Current++;
                                    return ("✅", "");
                                } else {
                                    // if the messages are close together, point that out. happens pretty often
                                    var lastMsg = s.count?.LastCountMsg;
                                    long difference = msg.TimeCreated() - (s.count?.LastCountMsg?.TimeCreated() ?? 0);
                                    // currently 500 millseconds; tweak if it's too little or too much
                                    return ("❌", (difference > 500 || lastMsg == null) ?
                                        "aw you're not very good at counting, are you?" : 
                                        "too many cooks in the kitchen!!"
                                    );
                                }
                            } },
                            { s.chain?.Channel?.Id ?? 0, delegate {
                                return null;
                            } },
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        };
                        // epic tuples 😄😄😄
                        (string emojiToParse, string reply) = actions[msg.Channel.Id].Invoke(msg.Author) ?? ("", "");
                        if (!string.IsNullOrEmpty(emojiToParse)) {
                            await msg.EmojiReact(emojiToParse);
                        }
                        if (!string.IsNullOrEmpty(reply)) {
                            await msg.Reply(reply);
                        }
                    }
                }
            });
            await Task.CompletedTask; 
        }
    }
}