using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
// using Microsoft.CodeAnalysis.CSharp.Scripting;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using CaretakerNET.Helper;
using CaretakerNET.Commands;
using CaretakerNET.Games;

namespace CaretakerNET
{
    public class MainHook
    {
        // gets called when program is ran; starts async loop
        public readonly static MainHook instance = new();
        static Task Main(string[] args) => instance.MainAsync(args);

        public readonly DiscordSocketClient _client;
        public ConnectFour? c4;
        public readonly Dictionary<ulong, ServerPersist> _s = [];
        // public Dictionary<ulong, UserPersist> _u = [];

        private readonly long startTime;
        public const string prefix = ">";
        public bool DebugMode = false;

        private readonly ulong[] TrustedUsers = [
            438296397452935169, // @astrljelly
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

            // Subscribe the logging handler to both the client and the CommandService.
            _client.Log += ClientLog;

            _client.MessageReceived += MessageReceivedAsync;

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
            foreach (var arg in args) {
                Caretaker.Log(arg);
            }
            DebugMode = args.Contains("debug");

            await _client.DownloadUsersAsync(_client.Guilds);

            // login and connect with token (change to config json file?)
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await _client.StartAsync();

            // wait infinitely so the bot stays connected
            await Task.Delay(Timeout.Infinite);
        }

        int lol = 0;

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // make sure the message is a user sent message, and output a new msg variable
            // also make sure it's not a bot.
            bool banned = Array.Exists(BannedUsers, x => x == message.Author.Id);
            if ((message is not SocketUserMessage msg) || msg.Author.IsBot && !banned) return; 

            var stopwatch = new Stopwatch();
            
            if (msg.Content.StartsWith(prefix)) {
                
                (string command, string parameters) = msg.Content[prefix.Length..].SplitByFirstChar(' ');
                if (string.IsNullOrEmpty(command)) return;

                try {
                    var typing = msg.Channel.EnterTypingState();
                    await CommandHandler.ParseCommand(msg, command, parameters);
                    typing.Dispose();
                } catch (Exception error) {
                    await msg.ReplyAsync(error.ToString(), allowedMentions: AllowedMentions.None);
                    throw;
                }
            }
            await Task.CompletedTask; 
        }
    }
}