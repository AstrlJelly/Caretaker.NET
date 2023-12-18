using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
// using Microsoft.CodeAnalysis.CSharp.Scripting;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Caretaker.Commands;

namespace Caretaker
{
    public class Program
    {
        // Program entry point
        static Task Main(string[] args) => new Program().MainAsync();

        private readonly DiscordSocketClient _client;

        // private readonly DateTime startTime = new();
        public CommandHandler commandHandler = new();

        private Program()
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
            Console.WriteLine("start!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

            // Login and connect.
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
            await _client.StartAsync();

            // Wait infinitely so your bot actually stays connected.
            await Task.Delay(Timeout.Infinite);
        }

        enum Time {
            ms, sec, min, hr, day, week,
        }

        // converts from seconds to minutes, hours to ms, minutes to days, etc.
        private double ConvertTime(double time, Time typeFromTemp = Time.sec, Time typeToTemp = Time.ms) {
            if (typeToTemp == typeFromTemp) return time;
            var typeFrom = (int)typeFromTemp;
            var typeTo = (int)typeToTemp;
            Console.WriteLine("typeFrom : " + typeFrom);
            Console.WriteLine("typeTo : " + typeTo);

            int modifier = 1;
            int[] converts = { 1000, 60, 60, 24, 7 };

            for (var i = Math.Min(typeFrom, typeTo); i < Math.Max(typeFrom, typeTo); i++) {
                modifier *= converts[i];
            }

            Console.WriteLine(modifier);

            return (typeFrom > typeTo) ? (time * modifier) : (time / modifier);
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message is not SocketUserMessage msg) return; // make sure the message is a user sent message, and output a new msg variable

            long ms1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string prefix = ">";
            if (msg.Content.StartsWith(prefix)) {
                string content = msg.Content[prefix.Length..];
                int firstSpace = content.IndexOf(' ');
                string command = firstSpace == -1 ? content : content[..firstSpace];
                string parameters = content[(firstSpace - 1)..];
                if (string.IsNullOrEmpty(command)) return;
                Console.WriteLine(command);
                commandHandler.ParseCommand(msg, command, parameters);
                
                // switch (command)
                // {
                //     case "ping":
                //         await msg.ReplyAsync("pong <:smide:1136427209041649694>");
                //     break;
                //     case "unixTime":
                //         await msg.ReplyAsync(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
                //     break;
                //     case "convertTime":
                //         var parts = msg.Content.Split(' ');
                //         var time = ConvertTime(double.Parse(parts[1]), (Time)Enum.Parse(typeof(Time), parts[2]), (Time)Enum.Parse(typeof(Time), parts[3]));
                //         await msg.ReplyAsync(time.ToString());
                //     break;
                //     default:
                //         await msg.ReplyAsync("erm actually that's not a command");
                //     break;
                // }
                Console.WriteLine(command);
            }
            await Task.CompletedTask; 
            // await Task.CompletedTask; 
        }
    }
}