using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

class Program
{
    // Program entry point
    static Task Main(string[] args) => new Program().MainAsync();

    private readonly DiscordSocketClient _client;

    // private readonly DateTime startTime = new();

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

    // Example of a logging handler. This can be re-used by addons
    // that ask for a Func<LogMessage, Task>.
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
        // Login and connect.
        await _client.LoginAsync(TokenType.Bot, File.ReadAllText("./token.txt"));
        await _client.StartAsync();

        // Wait infinitely so your bot actually stays connected.
        await Task.Delay(Timeout.Infinite);
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message is not SocketUserMessage msg) return;

        long ms1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string prefix = ">";
        if (msg.Content.StartsWith(prefix)) {
            int firstSpace = msg.Content.IndexOf(' ');
            string command = msg.Content.Substring(prefix.Length, firstSpace == -1 ? msg.Content.Length - 1 : firstSpace);
            if (string.IsNullOrEmpty(command)) return;
            switch (command)
            {
                case "ping":
                    await msg.ReplyAsync("pong <:smide:1136427209041649694>");
                break;
                case "unixTime":
                    await msg.ReplyAsync(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
                break;
                default:
                    await msg.ReplyAsync("erm actually that's not a command");
                break;
            }
            Console.WriteLine(command);
        }
        await Task.CompletedTask; 
        // await Task.CompletedTask; 
    }
}