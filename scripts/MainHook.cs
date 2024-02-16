global using static CaretakerNET.Core.Caretaker;
global using CaretakerNET.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;

using Discord;
using Discord.WebSocket;
using Discord.Webhook;

using CaretakerNET.Commands;
using CaretakerNET.Games;
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

        public readonly DiscordSocketClient Client;
        public ITextChannel? talkingChannel;
        private Dictionary<ulong, GuildPersist> GuildData = [];
        private Dictionary<ulong, UserPersist> UserData = [];

        private readonly long startTime;
        // public const char PREFIX = '>';
        public const char PREFIX_CHAR = '>';
        public const string PREFIX = ">";
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
            Client.Ready += ClientReady;

            AppDomain.CurrentDomain.UnhandledException += async delegate { 
                await Client.StopAsync();
                await Save();
            };

            startTime = new DateTimeOffset().ToUnixTimeMilliseconds();
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

            // const int EXLCUDE = 5;
            // const int MAX = 10;
            // var rd = new Random();
            // for (int i = 0; i < 100; i++)
            // {
            //     int randomInt = rd.Next(0, MAX - 1);
            //     if (randomInt == EXLCUDE) {
            //         randomInt++;
            //     }
            //     Log(randomInt);
            // }

            // keep running until Stop() is called
            while (keepRunning) {
                string? readLine = Console.ReadLine();
                if (readLine == "c") {
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
                    if (talkingChannel != null) message = talkingChannel.SendMessageAsync(readLine);
                    if (readLine.StartsWith(PREFIX) && message != null) {
                        _ = Task.Run(async () => {
                            var msg = await message;
                            (string command, string parameters) = readLine[PREFIX.Length..].SplitByFirstChar(' ');
                            (Command? com, parameters) = CommandHandler.ParseCommand(command, parameters);
                            if (com != null) {
                                await CommandHandler.DoCommand(msg, com, parameters);
                            }
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
            if (!keepRunning) {
                await Client.DownloadUsersAsync(Client.Guilds);

                LogDebug("GUILDS : " + string.Join(", ", Client.Guilds));
                await Load();
                License.iConfirmNonCommercialUse("hmmmmm");

                talkingChannel = Client.ParseGuild("1113913617608355992")?.ParseChannel("1113944754460315759");
                keepRunning = true;
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
            // int i = 0;
            foreach (var key in GuildData.Keys) {
                if (key <= 0) {
                    GuildData.Remove(key);
                } else {
                    try {
                        GuildData[key].Init(Client);
                    } catch (System.Exception error) {
                        GuildData[key] = new(key);
                        LogError(error);
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


        public bool TryGetGuildData(IUserMessage msg, out GuildPersist data) 
        {
            data = GetGuildData(msg.GetGuild()?.Id ?? 0)!;
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

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // wrap in Task.Run() so that multiple commands can be handled at the same time
            _ = Task.Run(async () => {
                if (message is not SocketUserMessage msg) return;

                // make sure the message is a user sent message, and output a new msg variable
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

                    var typing = msg.Channel.EnterTypingState();
                    try {
                        Stopwatch sw = new();
                        sw.Start();
                        await CommandHandler.DoCommand(msg, com, parameters);
                        u.timeout = DateNow() + 1000;
                        sw.Stop();
                        LogDebug($"parsing {PREFIX}{command} command took {sw.ElapsedMilliseconds} ms");
                    } catch (Exception error) {
                        await msg.Reply(error.Message, false);
                    }
                    typing.Dispose();
                } else {
                    if (TryGetGuildData(msg, out GuildPersist? s) && s != null) {
                        Dictionary<ulong, Func<SocketUser, (string, string)?>> actions = new() {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            { talkingChannel?.Id ?? 0, _ => {
                                LogInfo($"{msg.Author.GlobalName} : {msg.Content}", true);
                                return null;
                            } },
                            { s.count?.Channel?.Id ?? 1, author => {
                                // s.count will not be null here but the compiler doesn't know that 😢
                                var count = s.count;
                                // duplicate check
                                if (s.count?.LastCountMsg?.Author.Id == author.Id) {
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
                                    s.count.LastCountMsg = msg;
                                    return ("✅", "");
                                } else {
                                    // if the messages are close together, point that out. happens pretty often
                                    var lastMsg = s.count?.LastCountMsg;
                                    long difference = msg.TimeCreated() - (s.count?.LastCountMsg?.TimeCreated() ?? 0);
                                    count.Reset(false);
                                    // currently 800 millseconds; tweak if it's too little or too much
                                    return ("❌", (difference > 800 || lastMsg == null) ?
                                        "aw you're not very good at counting, are you?" : 
                                        "too many cooks in the kitchen!!"
                                    );
                                }
                            } },
                            { s.chain?.Channel?.Id ?? 2, delegate {
                                return null;
                            } },
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        };
                        // epic tuples 😄😄😄
                        (string emojiToParse, string reply) = actions[msg.Channel.Id]?.Invoke(msg.Author) ?? ("", "");
                        if (!string.IsNullOrEmpty(emojiToParse)) {
                            await msg.ReactAsync(emojiToParse);
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