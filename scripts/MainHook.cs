using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
// using Microsoft.CodeAnalysis.CSharp.Scripting;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Caretaker.Commands;
using Caretaker.Games;

namespace Caretaker
{
    public class MainHook
    {
        // gets called when program is ran; starts async loop
        public readonly static MainHook instance = new();
        static Task Main(string[] args) => instance.MainAsync(args);

        public DiscordSocketClient _client;
        public ConnectFour? c4;

        // private readonly DateTime startTime = new();
        // public CommandHandler commandHandler = new();
        public const string prefix = ">";
        public static bool DebugMode = false;

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
        }

        public static void Log(string message, LogSeverity severity = LogSeverity.Info)
        {
            Console.ForegroundColor = severity switch {
                LogSeverity.Critical or LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                LogSeverity.Verbose or LogSeverity.Debug => ConsoleColor.DarkGray,
                LogSeverity.Info or _ => ConsoleColor.White,
            };
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static Task ClientLog(LogMessage message)
        {
            Log($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}", message.Severity);
            
            return Task.CompletedTask;
        }

        private async Task MainAsync(string[] args)
        {
            CommandHandler.Init();
            foreach (var arg in args) {
                Log(arg);
            }
            DebugMode = args.Contains("debug");

            // login and connect with token (change to config json file?)
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await _client.StartAsync();

            // wait infinitely so the bot stays connected
            await Task.Delay(Timeout.Infinite);
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // make sure the message is a user sent message, and output a new msg variable
            // also make sure it's not a bot.
            if (message is not SocketUserMessage msg || msg.Author.IsBot) return; 

            var stopwatch = new Stopwatch();
            
            if (msg.Content.StartsWith(prefix)) {
                string content = msg.Content[prefix.Length..];
                int firstSpace = content.IndexOf(' ');
                string command = firstSpace == -1 ? content : content[..firstSpace];
                string parameters = firstSpace == -1 ? "" : content[(firstSpace + 1)..];
                if (string.IsNullOrEmpty(command)) return;
                try {
                    var typing = msg.Channel.EnterTypingState();
                    CommandHandler.ParseCommand(msg, command, parameters);
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