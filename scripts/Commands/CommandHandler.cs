﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CaretakerNET.ExternalEmojis;
using CaretakerNET.Games;
using CaretakerNET.Helper;


// using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET.Commands
{
    public class CommandHandler
    {
        public static readonly Command[] commands = [
            new("help", "list all normal commands", "bot/commands", async (msg, p) => {
                // await msg.ReplyAsync((string)p["command"]);
                string reply = ListCommands(p["command"]);
                await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "")]),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                if ((string)p["reply"] is not "@everyone" and not "@here" and not "") {
                    if (p["wait"] > 0) await Task.Delay(p["wait"]);
                    await msg.ReplyAsync((string)p["reply"]);
                } else {
                    string[] replies = [ "stop that!!!", "hey you can't do that :(", "explode", "why...", Emojis.Sab ];
                    await msg.ReplyAsync(p["reply"] != "" ? replies.GetRandom() : Emojis.Sab);
                }
                // if (p["wait"] > 0) await Task.Delay(p["wait"]);
                // await msg.ReplyAsync((string)p["reply"], allowedMentions: AllowedMentions.None);
            }, [
                new Param("reply", "the message to echo", Emojis.Smide),
                new Param("wait", "how long to wait until replying", 0),
            ]),

            new("true", Emojis.True, "silly", async (msg, p) => {
                if (!Emoji.TryParse(Emojis.True, out Emoji emoji)) return;
                SocketMessage? reactMessage = (msg.ReferencedMessage as SocketMessage) ?? msg.Channel.CachedMessages.LastOrDefault();
                // Console.WriteLine(msg.ReferencedMessage);
                // Console.WriteLine(msg.Reference);
                
                for (int i = 0; i < p["amount"]; i++) {
                    if (reactMessage == null) return;
                    Console.WriteLine(emoji.Name);
                    await reactMessage.AddReactionAsync(emoji);
                }
                // await msg.ReplyAsync("hi");
                // string reply = ListCommands(p["command"]);
                // await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
                
            }, [new Param("amount", "the amount you agree with this message", 20)]),

            new("challenge", "challenge another user to a game", "silly", async (msg, p) => {
                SocketUser? victim;
                if (p["username"] == "") {
                    victim = msg.ReferencedMessage.Author as SocketUser;
                } else {
                    victim = msg.GetGuild().Users.FirstOrDefault(x => x.Nickname == p["username"] || x.GlobalName == p["username"]);
                }
                if (victim == null) {
                    await msg.ReplyAsync($"hmm... seems like the user you tried to challenge is unavailable.");
                    return;
                } else {

                }
                switch (p["game"])
                {
                    case "c4" or "connect4": {

                    } break;
                    default: {

                    } break;
                }
            }, [
                new Param("game", "which game would you like to challenge with?", ""),
                new Param("user", "the username/display name of the person you'd like to challenge", "", "user")
            ]),

            new("hello", "say hi to a user", "silly", async (msg, p) => {
                // await msg.ReplyAsync("hi");
                // string reply = ListCommands(p["command"]);
                // await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
                IUser user = p["user"];
                if (user != null) {
                    await msg.EmojiReact("✅");
                    await user.SendMessageAsync($"{msg.Author.GlobalName} from {msg.GetGuild().Name} says hi!");
                } else {
                    await msg.ReplyAsync($"i can't reach that person right now :( maybe just send them a normal hello");
                }
            }, [new Param("user", "the username of the person you'd like to say hi to", "astrljelly", "user")]),

            new("c41", "these only exist because parsing parameters is hard :(", "hidden", async (msg, p) => {
                if (MainHook.instance.c4 == null) return;
                MainHook.instance.c4.SetColumn(ConnectFour.MAXWIDTH, ConnectFour.Player.One);
                await msg.ReplyAsync(MainHook.instance.c4.DisplayBoard());
                // await msg.ReplyAsync(((new int[1])[0]).ToString());
            }, []),

            new("c42", "refer to the above one", "hidden", async (msg, p) => {
                if (MainHook.instance.c4 == null) return;
                MainHook.instance.c4.SetColumn(2, ConnectFour.Player.Two);
                await msg.ReplyAsync(MainHook.instance.c4.DisplayBoard());
                // await msg.ReplyAsync(((new int[1])[0]).ToString());
            }, []),

            new("test", ":3", "testing", async (msg, p) => {
                MainHook.instance.c4 = new ConnectFour();
                MainHook.instance.c4.SetColumn(1, ConnectFour.Player.Two);
                await msg.ReplyAsync(MainHook.instance.c4.DisplayBoard());
                // await msg.ReplyAsync(((new int[1])[0]).ToString());
            }, []),

            new("cmd", "run more internal commands, will probably just be limited to astrl", "internal", async (msg, p) => {

            }),

            new("help", "list all cmd commands", "comands", async (msg, p) => {
                string reply = ListCommands(p["command"]);
                await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "help")]),

            new("params", "list all cmd commands", "testing", async (msg, p) => {
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

            new("test", "testing code out", "cmd", async (msg, p) => {

            })
        ];

        private static readonly Dictionary<string, Command> Commands = [];
        private static readonly Dictionary<string, Command> CmdCommands = [];

        // public static CommandHandler instance = new();

        // might wanna make it return what the command.func returns, though i don't know how helpful that would be
        public async static Task<bool> ParseCommand(SocketUserMessage msg, string command, string parameters = "")
        {
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

            Console.WriteLine($"-{command}-");
            Console.WriteLine($"-{parameters}-");

            if (!whichComms.TryGetValue(command, out Command? com) || com == null) return false;
            Dictionary<string, dynamic> paramDict = [];
            if (com.parameters != null && com.parameters.Length > 0) {
                string[] tempParameters = [];
                if (parameters != "") { // only do these checks if there are parameters to check for
                    const char space = '↭';
                    if (parameters.Contains('"')) {
                        string[] quoteSplit = parameters.Split('"');

                        for (var i = 1; i < quoteSplit.Length; i += 2) { 
                            // Console.WriteLine(quoteSplit[i]);
                            // check every other section (they will always be "in" double quotes) 
                            // and check if it actually has spaces needed to be replaced
                            if (quoteSplit[i].Contains(' ')) {
                                // quoteSplit[i] = string.Join(space, quoteSplit[i].Split(' ')); // why is this the best way to do it
                                quoteSplit[i] = quoteSplit[i].ReplaceAll(' ', space);
                            }
                        }
                        parameters = string.Join("", quoteSplit); // join everything back together
                    }
                    tempParameters = parameters.Split(' '); // then split it up as parameters
                }

                int tempLength = tempParameters.Length, comLength = com.parameters.Length;
                int max = (com.inf != null && tempLength > comLength) ? tempLength : comLength;
                Console.WriteLine("max : " + max);
                Console.WriteLine("tempParameters // " + string.Join(", ", tempParameters));
                Console.WriteLine("com.parameters // " + string.Join(", ", com.parameters.Select(x => x.name)));
                for (int i = 0; i < max; i++) 
                {
                    bool isInput = tempParameters.IsIndexValid(i); // will this parameter be set manually?
                    Param? settingParam; // the Param the the preset and type are being grabbed from
                    // Console.WriteLine("presetting : " + setting);
                    int colon = !isInput ? -1 : tempParameters[i].IndexOf(':');
                    string valueStr = "";
                    if (colon != -1) { // if colon exists, attempt to set settingParam to the string before the colon
                        string tempParam = tempParameters[i];
                        settingParam = Array.Find(com.parameters, x => x.name == tempParameters[i][..colon]);
                    } else {           // otherwise the settingParam will just be in order of the commands param array
                        settingParam = com.parameters[i];
                    }
                    if (isInput) valueStr = tempParameters[i][(colon + 1)..];
                    
                    if (settingParam == null) {
                        if (colon == -1) {
                            await msg.ReplyAsync($"incorrect param name! use \"{MainHook.prefix}help {command}\" to get params for {command}.");
                        }
                        return false;
                    }
                    string key = settingParam.name;
                    Caretaker.Log($"valueStr = {valueStr}, key = {key}");
                    dynamic? value = !isInput ? settingParam.preset : (settingParam.type switch {
                        "string" => valueStr.ToString(),
                        "int32" => Int32.Parse(valueStr),
                        "boolean" => valueStr == "true",
                        "user" => Caretaker.ParseUser(valueStr),
                        _ => valueStr,
                    });
                    // Console.WriteLine("value : " + value);
                    bool success = paramDict.TryAdd(key, value);
                }
                foreach (var (key1, value1) in paramDict) {
                    Console.WriteLine("PARAMDICT // = " + key1 + " : " + value1);
                }
            }
            
            try {
                await com.func.Invoke(msg, paramDict);
                return true;
            } catch (System.Exception err) {
                await msg.ReplyAsync(err.ToString());
                return false;
            }
            
            // try {
            //     com.func.Invoke(msg, paramDict);
            // } catch (System.Exception error) {
            //     Caretaker.LogError(error.ToString());
            // }
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

        // currently an instance isn't needed; try avoiding one?
        // i.e just put the logic that would need an instance in a different script
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
