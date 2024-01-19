using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CaretakerNET.ExternalEmojis;
using CaretakerNET.Games;
using CaretakerNET.Core;
using System.Collections.Frozen; // frozen dictionary doesn't seem very important

using Discord;
using Discord.WebSocket;
using System.Threading.Channels;

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

            new("flower", "Hiiii! " + Emojis.TalkingFlower, "silly", async (msg, p) => {

            }, [new Param("fileName", "what the file will be renamed to", "")]),

            new("c4go", "play connect 4 using this command!", "games", async (msg, p) => {

            }),

            new("challenge", "challenge another user to a game", "games", async (msg, p) => {
                IUser? victim = p["user"];
                if (victim == null) {
                    await msg.Reply($"hmm... seems like the user you tried to challenge is unavailable.");
                    return;
                }
                switch (p["game"])
                {
                    case "c4" or "connect4": {
                        var challengeMsg = await msg.Reply($"{Caretaker.UserPingFromID(victim.Id)}, do you accept {Caretaker.UserPingFromID(msg.Author.Id)}'s challenge?");
                        bool accepted = false;
                        for (int i = 0; i < 60; i++)
                        {
                            Emoji? checkmark = Emoji.Parse("✅");
                            if (!challengeMsg.Reactions.ContainsKey(checkmark)) continue;
                            int reactionCount = challengeMsg.Reactions.ContainsKey(checkmark) ? challengeMsg.Reactions[checkmark].ReactionCount : 0;
                            var acceptedUsers = await challengeMsg.GetReactionUsersAsync(checkmark, reactionCount)
                                                                  .FlattenAsync();
                            foreach (var user in acceptedUsers)
                            {
                                if (!user.IsBot && user.Id == victim.Id) {
                                    accepted = true;
                                    break;
                                }
                            }
                            if (accepted) break;
                            await Task.Delay(1000);
                        }
                        if (accepted) {
                            MainHook.instance.c4 = new(msg.Author.Id, victim.Id);
                            await msg.Reply($"{Caretaker.UserPingFromID(msg.Author.Id)} and {Caretaker.UserPingFromID(victim.Id)}, begin!");
                        } else {
                            _ = challengeMsg.ModifyAsync(msg => msg.Content = $"*{msg.Content}*\ntook too long! oops.");
                        }
                    } break;
                    default: {
                        await msg.Reply("that's not a game!");
                    } break;
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
                        await msg.RandomReply([":(", "you can just say hello to somebody else..!", Emojis.Sab]);
                    } else if (msg.Content == ">hello world") { // silly workaround
                        await msg.Reply("Hello, world!");
                    } else {
                        var msgGuild = msg.GetGuild();
                        string from = msgGuild != null ? " from " + msgGuild.Name : "";
                        _ = msg.EmojiReact("✅");
                        await user.SendMessageAsync($"{msg.Author.GlobalName} ({msg.Author.Username}){from} says hi!");
                    }
                } else {
                    await msg.Reply($"i can't reach that person right now :( maybe just send them a normal hello");
                }
            }, [new Param("user", "the username of the person you'd like to say hi to", "1182009469824139395", "user")]),

            new("cmd", "run more internal commands, will probably just be limited to astrl", "internal", async (_, _) => {}),

            new("help", "list all cmd commands", "commands", async (msg, p) => {
                string reply = ListCommands(p["command"], true);
                await msg.Reply(reply, false);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "")]),

            new("save", "save _s and _u", "internal", async (_, _) => await MainHook.instance.Save()),
            new("load", "save _s and _u", "internal", async (_, _) => await MainHook.instance.Load()),

            new("c4", "MANIPULATE connect 4", "games", async (msg, p) => {
                var c4 = MainHook.instance.c4 ??= new();
                MainHook.instance.c4.AddToColumn(p["column"], p["player"]);
            }, [ new("column", "", 0), new("player", "", 1) ]),
            
            new("c4get", "GET connect 4", "games", async (msg, p) => {
                var c4 = MainHook.instance.c4 ??= new();
                await msg.Reply(((int)MainHook.instance.c4.ElementAt(p["x"], p["y"])).ToString());
            }, [ new("x", "", 0), new("y", "", 0) ]),
            
            new("c4display", "DISPLAY connect 4", "games", async (msg, p) => {
                var c4 = MainHook.instance.c4 ??= new();
                await msg.Reply(MainHook.instance.c4.DisplayBoard());
                var win = MainHook.instance.c4.WinCheck();
                if (win.winningPlayer != ConnectFour.Player.None) await msg.Reply(win.winningPlayer);
            }, [ new("x", "", 0), new("y", "", 0) ]),

            new("servers", "get all servers", "hidden", async (msg, p) => {
                var client = MainHook.instance.Client;
                Caretaker.Log(client.Guilds.Count);
                foreach (var guild in client.Guilds) {
                    Caretaker.Log(guild.Name);
                }
            }),

            new("invite", "make an invite to a server i'm in", "hidden", async (msg, p) => {
                SocketGuild? guild = p["guild"];
                if (guild == null) {
                    await msg.Reply("darn. no server with that name");
                } else {
                    Caretaker.Log(guild.Name);
                    var invite = await guild.GetInvitesAsync();
                    await msg.Reply(invite.First().Url);
                }
            }, [ new("guild", "the name of the server to make an invite for", "", "guild") ]),

            new("talkingChannel", "set the channel that Console.ReadLine() will send to", "hidden", async (msg, p) => {
                SocketGuild? guild = p["guild"] ?? msg.GetGuild();
                if (guild == null) {
                    await msg.Reply("mmm... nope.");
                    return;
                }
                MainHook.instance.talkingChannel = string.IsNullOrEmpty(p["channel"]) ? guild.ParseChannel((string)p["channel"]) : (ITextChannel)msg.Channel;
            }, [ new("channel", "the channel to talk in", ""), new("guild", "the guild to talk in", "", "guild") ]),

            new("kill", "kills the bot", "hidden", async (msg, p) => {
                await Task.Delay(p["delay"]);
                MainHook.instance.Stop();
            }, [ new("delay", "the time to wait before the inevitable end", 0) ]),

            new("test", "testing code out", "testing", async (msg, p) => {

            }, [ new("test1", "for testing", ""), new("test2", "for testing: electric boogaloo", ""), new("params", "params!!!", "") ]),
        ];
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private static readonly Dictionary<string, Command> Commands = [];
        private static readonly Dictionary<string, Command> CmdCommands = [];

        // public static CommandHandler instance = new();

        // might wanna make it return what the command.func returns, though i don't know how helpful that would be
        public async static Task<bool> ParseCommand(IUserMessage msg, string command, string parameters = "")
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
                        "uint32"      => uint.Parse(str),
                        "boolean"     => str == "true",
                        "user"        => MainHook.instance.Client.ParseUser(str, guild),
                        "channel"     => guild?.ParseChannel(str),
                        "guild"       => MainHook.instance.Client.ParseGuild(str),
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
                        value = ToType(setParam.type, valueStr, msg.GetGuild());
                    } else {
                        var p = setParam.preset;
                        value = p.GetType() == typeof(string) ? ToType(setParam.type, p, msg.GetGuild()) : p;
                    }

                    bool success = paramDict.TryAdd(setParam.name, value);
                }

                if (com.inf != null) {
                    // if inf params are needed, grab everything after
                    var paramsAsInfTypes = splitParams.Skip(i)
                                                      .Select(splitParam => ToType(com.inf.type, splitParam, msg.GetGuild()));
                    paramDict.TryAdd("params", paramsAsInfTypes.ToArray());
                }
            }

            stopwatch.Stop();
            Caretaker.LogDebug($"took {stopwatch.Elapsed.TotalMilliseconds} ms to parse parameters", true);
            
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
            string[] commandKeys = singleCom != "" ? [ singleCom ] : commandDict.Keys.ToArray();
            for (int i = 0; i < commandKeys.Length; i++)
            {
                var com = commandDict[commandKeys[i]];
                if (commandKeys.IsIndexValid(i - 1) && commandDict[commandKeys[i - 1]].name == com.name) continue;
                if (com.genre == "hidden" && !showHidden) continue;
                string joinedParams = "";
                if (com.parameters != null) {
                    IEnumerable<string> paramNames = com.parameters.Select(x => x.name);
                    joinedParams = string.Join(", ", paramNames) + (com.inf != null ? ", params" : "");
                }

                response.Add($"{MainHook.PREFIX}{com.name} ({joinedParams}) : {com.desc}\n");
            }
            return response.Count > 0 ? string.Join("", response) : "mmm... no.";
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
        }
    }
}
