using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CaretakerNET.ExternalEmojis;
using CaretakerNET.Games;
using CaretakerNET.Core;


// using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using System.Security.Cryptography;
using System.Collections.Frozen;

namespace CaretakerNET.Commands
{
    public class CommandHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static readonly Command[] commands = [
            new("help", "list all normal commands", "commands", async (msg, p) => {
                // await msg.Reply((string)p["command"]);
                string reply = ListCommands(p["command"]);
                await msg.Reply(reply, false);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "")]),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                string reply = (string)p["reply"];
                if (!reply.Contains("@everyone") && !reply.Contains("@here") && reply != "") {
                    await Task.Delay(p["wait"]);
                    await msg.Reply((string)p["reply"], false);
                } else {
                    string[] replies = [ "stop that!!!", "hey you can't do that :(", "explode", "why...", Emojis.Sab ];
                    await msg.Reply(p["reply"] != "" ? replies.GetRandom()! : Emojis.Sab);
                }
            }, [
                new Param("reply", "the message to echo", Emojis.Smide),
                new Param("wait", "how long to wait until replying", 0),
            ]),

            new("true", Emojis.True, "silly", async (msg, p) => {
                EmojiSpam(msg, Emojis.True, p["amount"]);
            }, [new Param("amount", "the amount you agree with this message", 20)]),

            // new("false", Emojis.True, "silly", async (msg, p) => {
            //     await EmojiSpam(msg, Emojis.False);
            // }, [new Param("amount", "the amount you agree with this message", 20)]),

            new("smide", Emojis.Smide, "silly", async (msg, p) => {
                EmojiSpam(msg, Emojis.Smide, p["amount"]);
            }, [ new Param("amount", $"how {Emojis.Smide} this message makes you feel", 20) ]),

            // new("explode", Emojis.True, "silly", async (msg, p) => {
            //     await EmojiSpam(msg, Emojis.ExplodingHead);
            // }, [new Param("amount", "the amount you agree with this message", 20)]),

            new("c4go", "play connect 4 using this command!", "games", async (msg, p) => {

            }),

            new("challenge", "challenge another user to a game", "games", async (msg, p) => {
                IUser? victim = p["user"];
                if (victim == null) {
                    await msg.Reply($"hmm... seems like the user you tried to challenge is unavailable.");
                    return;
                } else {
                    switch (p["game"])
                    {
                        case "c4" or "connect4": {
                            var challengeMsg = await msg.ReplyAsync($"{Caretaker.UserPingFromID(victim.Id)}, do you accept {Caretaker.UserPingFromID(msg.Author.Id)}'s challenge?");

                        } break;
                        default: {

                        } break;
                    }
                }
            }, [
                new Param("game", "which game would you like to challenge with?", ""),
                new Param("user", "the username/display name of the person you'd like to challenge", "", "user")
            ]),

            new("hello, hi", "say hi to a user", "silly", async (msg, p) => {
                IUser user = p["user"];
                if (user != null) {
                    if (user.Id == 1182009469824139395) {
                        await msg.Reply("aw hii :3");
                    } else if (user.Id == msg.Author.Id) {
                        await msg.RandomReply([":(", "you can just say hello to somebody else..!", ""]);
                    } else {
                        var msgGuild = msg.TryGetGuild();
                        string from = msgGuild != null ? " from " + msgGuild.Name : "";
                        _ = msg.EmojiReact("✅");
                        await user.SendMessageAsync($"{msg.Author.GlobalName} ({msg.Author.Username}){from} says hi!");
                    }
                } else {
                    await msg.Reply($"i can't reach that person right now :( maybe just send them a normal hello");
                }
            }, [new Param("user", "the username of the person you'd like to say hi to", "1182009469824139395", "user")]),

            new("cmd", "run more internal commands, will probably just be limited to astrl", "internal", async (msg, p) => {}),

            new("help", "list all cmd commands", "commands", async (msg, p) => {
                string reply = ListCommands(p["command"], true);
                await msg.Reply(reply, false);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "")]),

            new("params", "reply with all params", "testing", async (msg, p) => {
                var keys = p.Keys;
                foreach (var value in p.Values) {
                    await msg.Reply((string)value);
                }
                // await msg.Reply(p);
            }, [new Param("test", "", 0)]),

            new("c4", "MANIPULATE connect 4", "games", async (msg, p) => {
                if (MainHook.instance.c4 == null) MainHook.instance.c4 = new();
                MainHook.instance.c4.AddToColumn(p["column"], p["player"]);
            }, [ new("column", "", 0), new("player", "", 1) ]),
            
            new("c4get", "GET connect 4", "games", async (msg, p) => {
                if (MainHook.instance.c4 == null) MainHook.instance.c4 = new();
                await msg.Reply(((int)MainHook.instance.c4.GetElement(p["x"], p["y"])).ToString());
            }, [ new("x", "", 0), new("y", "", 0) ]),
            
            new("c4display", "DISPLAY connect 4", "games", async (msg, p) => {
                if (MainHook.instance.c4 == null) MainHook.instance.c4 = new();
                await msg.Reply(MainHook.instance.c4.DisplayBoard());
                var win = MainHook.instance.c4.WinCheck();
                if (win.winningPlayer != ConnectFour.Player.None) await msg.Reply(win.winningPlayer);
            }, [ new("x", "", 0), new("y", "", 0) ]),

            new("servers", "get all servers", "hidden", async (msg, p) => {
                var client = MainHook.instance._client;
                Caretaker.Log(client.Guilds.Count);
                foreach (var guild in client.Guilds) {
                    Caretaker.Log(guild.Name);
                }
            }),

            new("invite", "make an invite to a server i'm in", "hidden", async (msg, p) => {
                SocketGuild? guild = MainHook.instance._client.Guilds.FirstOrDefault(x => x.Name.Equals((string)p["serverName"], StringComparison.CurrentCultureIgnoreCase));
                if (guild == null) {
                    await msg.Reply("darn. no server with that name");
                } else {
                    // var invite = await guild.GetVanityInviteAsync();
                    // await msg.Reply(invite.Url);
                    var invite = await guild.GetInvitesAsync();
                    await msg.Reply(invite.First().Url);
                }
            }, [ new("serverName", "the name of the server to make an invite for", "") ]),

            new("kill", "kills the bot", "hidden", async (msg, p) => {
                await Task.Delay(p["delay"]);
                // MainHook.instance.Kill(true);
            }, [ new("delay", "the time to wait before the inevitable end", 0) ]),

            new("test", "testing code out", "testing", async (msg, p) => {
                MainHook.instance.CloseMainWindow(); 
            }, [ new("test1", "for testing", ""), new("test2", "for testing: electric boogaloo", ""), new("params", "params!!!", "") ]),
        ];
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private static readonly Dictionary<string, Command> Commands = [];
        private static readonly Dictionary<string, Command> CmdCommands = [];

        // public static CommandHandler instance = new();

        // might wanna make it return what the command.func returns, though i don't know how helpful that would be
        public async static Task<bool> ParseCommand(SocketUserMessage msg, string command, string parameters = "")
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var whichComms = Commands;
            if (command == "cmd") {
                whichComms = CmdCommands;
                (command, parameters) = parameters.SplitByFirstChar(' ');
            }

            Caretaker.LogDebug($"-{command}-");
            Caretaker.LogDebug($"-{parameters}-");

            if (!whichComms.TryGetValue(command, out Command? com) || com == null) return false;
            Dictionary<string, dynamic> paramDict = [];
            if (com.parameters != null && (com.parameters.Length > 0 || com.inf != null)) {
                const char space = '↭';
                string[] splitParams = [];
                if (parameters != "") { // only do these checks if there are parameters to check for
                    if (parameters.Contains('"')) {
                        string[] quoteSplit = parameters.Split('"');

                        for (int j = 1; j < quoteSplit.Length; j += 2) { 
                            // check every other section (they will always be "in" double quotes) 
                            // and check if it actually has spaces needed to be replaced
                            if (quoteSplit[j].Contains(' ')) {
                                quoteSplit[j] = quoteSplit[j].ReplaceAll(' ', space);
                            }
                        }
                        parameters = string.Join("", quoteSplit); // join everything back together
                    }
                    splitParams = parameters.Split(' '); // then split it up as parameters
                }

                Caretaker.LogDebug("splitParams    // " + string.Join(", ", splitParams));
                Caretaker.LogDebug("com.parameters // " + string.Join(", ", com.parameters.Select(x => x.name)));

                static dynamic? ToType(string type, string str, SocketGuild? guild) => 
                    type switch {
                        "int32"       => int.Parse(str),
                        "boolean"     => str == "true",
                        "user"        => Caretaker.ParseUser(str),
                        "channel"     => Caretaker.ParseChannel(str, guild),
                        "string" or _ => str,
                    };
                
                int i = 0;
                for (i = 0; i < com.parameters.Length; i++)
                {
                    Param? setParam = com.parameters[i]; // the Param the name/preset/type are being grabbed from
                    dynamic? value;
                    
                    if (splitParams.IsIndexValid(i)) { // will this parameter be set manually?
                        int colon = splitParams[i].IndexOf(':');
                        string valueStr = splitParams[i].ReplaceAll(space, ' ');
                        if (colon != -1) { // if colon exists, attempt to set settingParam to the string before the colon
                            char? colonCheck = splitParams[i].TryGet(colon - 1);
                            if (colonCheck != null && colonCheck != '<') {
                                string paramName = splitParams[i][..colon];
                                if (paramName == "params") {
                                    i++;
                                    break;
                                }
                                valueStr = splitParams[i][(colon + 1)..];
                                setParam = Array.Find(com.parameters, x => x.name == paramName);
                                if (setParam == null) {
                                    await msg.Reply($"incorrect param name! use \"{MainHook.PREFIX}help {command}\" to get params for {command}.");
                                    return false;
                                }
                            }
                        }
                        value = ToType(setParam.type, valueStr, msg.TryGetGuild());
                    } else {
                        var p = setParam.preset;
                        value = p.GetType() == typeof(string) ? ToType(setParam.type, p, msg.TryGetGuild()) : p;
                    }

                    bool success = paramDict.TryAdd(setParam.name, value);
                }

                if (com.inf != null) {
                    // if inf params are needed, grab everything after
                    var paramsAsInfTypes = splitParams.Skip(i)
                                                      .Select(splitParam => ToType(com.inf.type, splitParam, msg.TryGetGuild()));
                    paramDict.TryAdd("params", paramsAsInfTypes.ToArray());
                }
            }

            stopwatch.Stop();
            Caretaker.LogDebug($"[{DateTime.Now:HH:mm:ss tt}] took {stopwatch.Elapsed.TotalMilliseconds} ms to parse parameters");
            
            try {
                await com.func.Invoke(msg, paramDict);
                return true;
            } catch (System.Exception err) {
                await msg.Reply(err.ToString());
                return false;
            }
        }
        
        public static string ListCommands(string singleCom, bool cmd = false, bool showHidden = false)
        {
            var commandDict = cmd ? CmdCommands : Commands;
            if (singleCom != "" && !commandDict.ContainsKey(singleCom)) {
                return $"{singleCom} is NOT a command. try again :/";
            }
            List<string> response = [];
            // string[] commands = singleCom != "" ? [singleCom] : commandDict.Keys.ToArray();
            Command[] commandsToList = singleCom != "" ? [ commandDict[singleCom] ] : commands;
            foreach (var com in commandsToList)
            {
                if (com.genre == "hidden" && !showHidden) continue;
                var paramNames = com.parameters?.Select(x => x.name);
                string joinedParams = "";
                if (paramNames?.FirstOrDefault() != null) {
                    joinedParams = $"{string.Join(", ", paramNames)}";
                }

                response.Add($"{MainHook.PREFIX}{com.name} ({joinedParams}) : {com.desc}\n");
            }
            return response.Count > 0 ? string.Join("", response) : "mmm... no.";
        }
        
        public static void EmojiSpam(SocketUserMessage msg, string emojiStr, int amount)
        {
            SocketMessage? reactMessage = (SocketMessage?)msg.ReferencedMessage ?? msg.Channel.CachedMessages.ToArray()[^2];
            Caretaker.Log(reactMessage?.Content ?? "it's null.");
            
            // if (!Emoji.TryParse(emojiStr, out Emoji emoji)) return;
            var emoji = Emote.Parse(emojiStr);
            
            for (int i = 0; i < amount; i++) {
                if (reactMessage == null) return;
                _ = Task.Run(async () => {await reactMessage.AddReactionAsync(emoji);});
            }
        }

        // currently an instance isn't needed; try avoiding one?
        // i.e just put the logic that would need an instance in a different script
        public static void Init()
        {
            var whichComms = Commands;
            foreach (var command in commands) {
                var commandNames = command.name.Split(", ");
                // if (commandNames.Length > 1) command.name = commandNames[0];
                foreach (var commandName in commandNames) {
                    whichComms.Add(commandName, command);
                }
                
                if (command.name == "cmd") whichComms = CmdCommands;
            }
            Commands.ToFrozenDictionary();
            CmdCommands.ToFrozenDictionary();
        }
    }
}
