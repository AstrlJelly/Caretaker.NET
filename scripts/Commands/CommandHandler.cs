using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Security.Cryptography.X509Certificates;

namespace Caretaker.Commands
{
    public class CommandHandler
    {
        public static readonly Command[] commands = {
            new("help", "list all normal commands", "bot/commands", async (msg, p) => {
                await msg.ReplyAsync("hi");
            }),

            new("cmd", "run more internal commands, will probably just be limited to astrl", "bot/internal", (msg, p) => {

            }),

            new("params", "list all cmd commands", "cmd", async (msg, p) => {
                var keys = p.Keys;
                foreach (var value in p.Values) {
                    await msg.ReplyAsync((string)value);
                }
                // await msg.ReplyAsync(p);
            }),

            new("help", "list all cmd commands", "cmd", (msg, p) => {

            })
        };

        public Dictionary<string, Command> Commands = new();
        public Dictionary<string, Command> CmdCommands = new();

        public Command ParseCommand(SocketUserMessage msg, string command, string parameters)
        {
            var splitParams = parameters.Split(' ');
            var whichComms = Commands;
            if (command == "cmd") {
                whichComms = CmdCommands;
                command = splitParams[0];
            }
            Command? com = whichComms[command];
            
            Dictionary<string, dynamic> paramDict = splitParams.ToDictionary(x => x, x => (dynamic)x);
            com.func.Invoke(msg, paramDict);
            return com;
        }

        public CommandHandler()
        {
            var whichComms = Commands;
            foreach (var command in commands) {
                whichComms.Add(command.name, command);
                if (command.name == "cmd") whichComms = CmdCommands;
            }

            foreach (var command in Commands.Values) {
                Console.WriteLine("normal : " + command.name);
            }
            foreach (var command in CmdCommands.Values) {
                Console.WriteLine("cmd : " + command.name);
            }
        }
    }
}
