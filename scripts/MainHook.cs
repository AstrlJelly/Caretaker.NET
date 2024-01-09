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

namespace CaretakerNET
{
    public class MainHook : Process
    {
        // gets called when program is ran; starts async loop
        public readonly static MainHook instance = new();
        static Task Main(string[] args) => instance.MainAsync(args);

        public readonly DiscordSocketClient _client;
        public ConnectFour? c4;
        public ulong[] players = [];
        public readonly Dictionary<ulong, ServerPersist> _s = [];
        // public Dictionary<ulong, UserPersist> _u = [];

        private readonly long startTime;
        public const string PREFIX = ">";
        public bool DebugMode = false;

        private readonly ulong[] TrustedUsers = [
            438296397452935169, // @astrljelly
            752589264398712834, // @antoutta
        ];
        private readonly ulong[] BannedUsers = [
            468933965110312980, // @lifinale
        ];

        private MainHook()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All, 
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 50,
            });

            _client.Log += ClientLog;
            _client.MessageReceived += MessageReceivedAsync;

            // Exited += async delegate {
            //     await _client.StopAsync();
            // };

            startTime = new DateTimeOffset().ToUnixTimeMilliseconds();
        }

        private static Task ClientLog(LogMessage message)
        {
            Caretaker.Log($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}", message.Severity);
            return Task.CompletedTask;
        }

        private async Task MainAsync(string[] args)
        {
            // Load();
            CommandHandler.Init();
            DebugMode = args.Contains("debug") || args.Contains("-d");

            // login and connect with token (change to config json file?)
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await _client.StartAsync();

            await _client.DownloadUsersAsync(_client.Guilds);

            // wait infinitely so the bot stays connected
            await Task.Delay(Timeout.Infinite);
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // wrap in Task.Run() so that multiple commands can be handled at the same time
            _ = Task.Run(async () => {
                // make sure the message is a user sent message, and output a new msg variable
                // also make sure it's not a bot/not banned
                bool banned = Array.Exists(BannedUsers, x => x == message.Author.Id);
                if ((message is not SocketUserMessage msg) || msg.Author.IsBot || banned) return;

                if (msg.Content.StartsWith(PREFIX)) {
                    (string command, string parameters) = msg.Content[PREFIX.Length..].SplitByFirstChar(' ');
                    if (string.IsNullOrEmpty(command)) return;

                    // var typing = msg.Channel.EnterTypingState();
                    using (msg.Channel.EnterTypingState()) {
                        try {
                            Stopwatch sw = new();
                            sw.Start();
                            await CommandHandler.ParseCommand(msg, command, parameters);
                            sw.Stop();
                            Caretaker.LogDebug($"parsing {PREFIX}{command} command took {sw.ElapsedMilliseconds} ms");
                        } catch (Exception error) {
                            await msg.Reply(error, false);
                            throw;
                        }
                    }
                    // typing.Dispose();
                }
            });
            await Task.CompletedTask; 
        }
    }
}