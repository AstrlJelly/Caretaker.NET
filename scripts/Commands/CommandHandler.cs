using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Caretaker.ExternalEmojis;
using Caretaker.Games;
using Caretaker.Helper;


// using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;

namespace Caretaker.Commands
{
    public class CommandHandler
    {
        public static readonly Command[] commands = {
            new("help", "list all normal commands", "bot/commands", async (msg, p) => {
                // await msg.ReplyAsync((string)p["command"]);
                string reply = ListCommands(p["command"]);
                await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "")]),

            new("echo", "list all normal commands", "bot/commands", (msg, p) => {
                Console.WriteLine(p["reply"]);
                // if (p["wait"] > 0) await Task.Delay(p["wait"]);
                // await msg.ReplyAsync((string)p["reply"], allowedMentions: AllowedMentions.None);
            }, [
                new Param("reply", "the message to echo", Emojis.Smide),
                new Param("wait", "how long to wait until replying", 0),
            ]),

            new("true", Emojis.True, "bot/commands", async (msg, p) => {
                if (!Emoji.TryParse(Emojis.True, out Emoji emoji)) return;
                SocketMessage? reactMessage = (msg.ReferencedMessage as SocketMessage) ?? msg.Channel.CachedMessages.LastOrDefault();
                // Console.WriteLine(msg.ReferencedMessage);
                // Console.WriteLine(msg.Reference);
                
                for (int i = 0; i < p["amount"]; i++) {
                    if (reactMessage == null) return;
                    await reactMessage.AddReactionAsync(emoji);
                }
                // await msg.ReplyAsync("hi");
                // string reply = ListCommands(p["command"]);
                // await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
                
            }, [new Param("amount", "the amount you agree with this message", 20)]),

            new("wynn", "hiii wynn", "bot/commands", async (msg, p) => {
                // await msg.ReplyAsync("hi");
                // string reply = ListCommands(p["command"]);
                // await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
                var wynn = await MainHook.instance._client.GetUserAsync(411496909505560578);
                await wynn.SendMessageAsync($"{msg.Author.Username} from {msg.GetGuild().Name} says hi!");
            }, []),

            new("test", ":3", "bot/commands", async (msg, p) => {
                MainHook.instance.c4 = new ConnectFour();
                MainHook.instance.c4.SetColumn(0, ConnectFour.Player.One);
                // c4.SetColumn(1, 0);
                await msg.ReplyAsync(MainHook.instance.c4.DisplayBoard());
            }, []),

            new("cmd", "run more internal commands, will probably just be limited to astrl", "bot/internal", (msg, p) => {

            }),

            new("params", "list all cmd commands", "cmd", async (msg, p) => {
                var keys = p.Keys;
                foreach (var value in p.Values) {
                    await msg.ReplyAsync((string)value);
                }
                // await msg.ReplyAsync(p);
            }, [new Param("test", "", 0)]),

            new("test1", "list all cmd commands", "hidden", async (msg, p) => {
                var keys = p.Keys;
                foreach (var value in p.Values) {
                    await msg.ReplyAsync((string)value);
                }
                // await msg.ReplyAsync(p);
            }, [ new Param("test", "", false) ]),

            new("test2", "list all cmd commands", "hidden", async (msg, p) => {
                var keys = p.Keys;
                foreach (var value in p.Values) {
                    await msg.ReplyAsync((string)value);
                }
                // await msg.ReplyAsync(p);
            }, [ new Param("test", "", "") ]),

            new("help", "list all cmd commands", "cmd", async (msg, p) => {
                string reply = ListCommands(p["command"]);
                await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "help")]),

            new("test", "testing code out", "cmd", (msg, p) => {

            })
        };

        private static readonly Dictionary<string, Command> Commands = [];
        private static readonly Dictionary<string, Command> CmdCommands = [];

        // public static CommandHandler instance = new();

        // might wanna make it return what the command.func returns, though i don't know how helpful that would be
        public static void ParseCommand(SocketUserMessage msg, string command, string parameters = "")
        {
            // Console.WriteLine($"-{command}-");
            // Console.WriteLine($"-{parameters}-");
            
            var whichComms = Commands;
            if (command == "cmd") {
                whichComms = CmdCommands;
                var spaceIndex = parameters.IndexOf(' ');
                if (spaceIndex == -1) {
                    command = parameters;
                    parameters = "";
                } else {
                    command = parameters[..spaceIndex];
                    parameters = parameters[(spaceIndex + 1)..];
                }
                // Console.WriteLine($"command : -{command}-");
                // Console.WriteLine($"parameters : -{parameters}-");
                
            }

            if (!whichComms.TryGetValue(command, out Command? com) || com == null) return;
            Dictionary<string, dynamic> paramDict = [];
            if (com.parameters != null && com.parameters.Length > 0) {
                string[] tempParameters = [];
                if (parameters != "") { // only do these checks if there are parameters to check for
                    const char space = '↭';
                    if (parameters.Contains('"')) {
                        string[] quoteSplit = parameters.Split('"');

                        for (var i = 1; i < quoteSplit.Length; i += 2) { 
                            // Console.WriteLine(quoteSplit[i]);
                            // check every other section (they will always be "in" double quotes) and check if it actually has spaces needed to be replaced
                            if (quoteSplit[i].Contains(' ')) {
                                quoteSplit[i] = string.Join(space, quoteSplit[i].Split(' ')); // why is this the best way to do it
                            }
                        }
                        parameters = string.Join("", quoteSplit); // join everything back together
                    }
                    tempParameters = parameters.Split(' '); // then split it up as parameters
                }

                int tempLength = tempParameters.Length, comLength = com.parameters.Length;
                int test = (com.inf != null && tempLength > comLength) ? tempLength : comLength;
                Console.WriteLine("test : " + test);
                Console.WriteLine("tempParameters // " + string.Join(", ", tempParameters));
                Console.WriteLine("com.parameters // " + string.Join(", ", com.parameters.Select(x => x.name)));
                for (int i = 0; i < test; i++) 
                {
                    bool setting = tempParameters.IsIndexValid(i);
                    Param? settingParam;
                    // Console.WriteLine("presetting : " + setting);
                    int colon = setting ? tempParameters[i].IndexOf(':') : -1;
                    string valueStr = "";
                    if (colon != -1) {
                        string tempParam = tempParameters[i];
                        // maybe use second iterator to change how params are automatically set? worked before ig
                        valueStr = tempParam[(colon + 1)..];
                        settingParam = Array.Find(com.parameters, x => x.name == tempParam[..colon]);
                    } else {
                        settingParam = com.parameters[i];
                    }
                    
                    if (settingParam == null) {
                        if (colon == -1) {
                            msg.ReplyAsync($"incorrect param name! use \"{MainHook.prefix}help {command}\" to get params for {command}.");
                        }
                        return;
                    }
                    string key = settingParam.name;
                    dynamic value = !setting ? settingParam.preset : (settingParam.type switch {
                        "string" => valueStr.ToString(),
                        "int32" => int.Parse(valueStr),
                        "boolean" => valueStr == "true",
                        _ => valueStr,
                    });
                    // Console.WriteLine("value : " + value);
                    bool success = paramDict.TryAdd(key, value);
                }
                foreach (var (key1, value1) in paramDict) {
                    Console.WriteLine("PARAMDICT // = " + key1 + " : " + value1);
                }
            }
            
            com.func.Invoke(msg, paramDict);
            // return com;
        }
        public static string ListCommands(string singleCom, bool showHidden = false, bool cmd = false)
        {
            var commandDict = cmd ? CmdCommands : Commands;
            if (singleCom != "" && !commandDict.ContainsKey(singleCom)) {
                return $"{singleCom} is NOT a command. try again :/";
            }
            List<string> response = [];
            string[] commands = singleCom != "" ? [singleCom] : commandDict.Keys.ToArray();
            for (var i = 0; i < commands.Length; i++) {
                Command com = commandDict[commands[i]];
                if (com.genre == "hidden" && !showHidden) continue;
                var paramNames = com.parameters?.Select(x => x.name);
                string joinedParams = "";
                if (paramNames?.FirstOrDefault() != null) {
                    joinedParams = $"{string.Join(", ", paramNames)}";
                }

                response.Add($"{MainHook.prefix}{commands[i]} ({joinedParams}) : {com.desc}\n");
            }
            return string.Join("", response);
        }

        public static void Init()
        {
            var whichComms = Commands;
            foreach (var command in commands) {
                whichComms.Add(command.name, command);
                if (command.name == "cmd") whichComms = CmdCommands;
            }
        }
    }
}
