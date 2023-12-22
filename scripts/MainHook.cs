using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
// using Microsoft.CodeAnalysis.CSharp.Scripting;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Caretaker.Commands;
using System.Diagnostics;

namespace Caretaker
{
    public class MainHook
    {
        // gets called when program is ran; starts async loop
        public readonly static MainHook instance = new();
        static Task Main(string[] args) => instance.MainAsync();

        public DiscordSocketClient _client;

        // private readonly DateTime startTime = new();
        // public CommandHandler commandHandler = new();
        public const string prefix = ">";

        private MainHook()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 50,
            });

            // Subscribe the logging handler to both the client and the CommandService.
            _client.Log += Log;

            _client.MessageReceived += MessageReceivedAsync;
        }

        private static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();
            
            return Task.CompletedTask;
        }

        private async Task MainAsync()
        {
            CommandHandler.Init();
            Console.WriteLine("start!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

            // login and connect with token (change to config json file?)
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await _client.StartAsync();

            // wait infinitely so the bot stays connected
            await Task.Delay(Timeout.Infinite);
        }

        enum Time { ms, sec, min, hr, day, week, }

        // converts from seconds to minutes, hours to ms, minutes to days, etc.
        private static double ConvertTime(double time, Time typeFromTemp = Time.sec, Time typeToTemp = Time.ms) {
            if (typeToTemp == typeFromTemp) return time;
            var typeFrom = (int)typeFromTemp;
            var typeTo = (int)typeToTemp;
            Console.WriteLine("typeFrom : " + typeFrom);
            Console.WriteLine("typeTo : " + typeTo);

            int modifier = 1;
            int[] converts = [1000, 60, 60, 24, 7];

            for (var i = Math.Min(typeFrom, typeTo); i < Math.Max(typeFrom, typeTo); i++) {
                modifier *= converts[i];
            }

            Console.WriteLine(modifier);

            return (typeFrom > typeTo) ? (time * modifier) : (time / modifier);
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