using System;
using System.Diagnostics;
using System.Linq;
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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static readonly Command[] commands = [
            new("help", "list all normal commands", "bot/commands", async (msg, p) => {
                // await msg.ReplyAsync((string)p["command"]);
                string reply = ListCommands(p["command"]);
                await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "")]),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                string reply = (string)p["reply"];
                if (!reply.Contains("@everyone") && !reply.Contains("@here") && reply != "") {
                    // DelayFactory.DelayAction(500, new Action(() => { this.RunAction(); }));
                    await msg.ReplyAsync((string)p["reply"], allowedMentions: AllowedMentions.None);
                } else {
                    string[] replies = [ "stop that!!!", "hey you can't do that :(", "explode", "why...", Emojis.Sab ];
                    await msg.ReplyAsync(p["reply"] != "" ? replies.GetRandom() : Emojis.Sab);
                }
            }, [
                new Param("reply", "the message to echo", Emojis.Smide),
                // new Param("wait", "how long to wait until replying", 0),
            ]),

            new("true", Emojis.True, "silly", async (msg, p) => {
                if (!Emoji.TryParse(Emojis.True, out Emoji emoji)) return;
                SocketMessage? reactMessage = (msg.ReferencedMessage as SocketMessage) ?? msg.Channel.CachedMessages.LastOrDefault();
                
                for (int i = 0; i < p["amount"]; i++) {
                    if (reactMessage == null) return;
                    await reactMessage.AddReactionAsync(emoji);
                }
            }, [new Param("amount", "the amount you agree with this message", 20)]),

            new("challenge", "challenge another user to a game", "games", async (msg, p) => {
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
                IUser user = p["user"];
                if (user != null) {
                    if (user.Id != 1182009469824139395) {
                        var msgGuild = msg.TryGetGuild();
                        string from = msgGuild != null ? " from " + msgGuild.Name : "";
                        _ = msg.EmojiReact("✅");
                        await user.SendMessageAsync($"{Caretaker.UserPingFromID(msg.Id)}{from} says hi!");
                    } else {
                        await msg.ReplyAsync("aw hii :3");
                    }
                } else {
                    await msg.ReplyAsync($"i can't reach that person right now :( maybe just send them a normal hello");
                }
            }, [new Param("user", "the username of the person you'd like to say hi to", "astrljelly", "user")]),

            new("cmd", "run more internal commands, will probably just be limited to astrl", "internal", async (msg, p) => {}),

            new("help", "list all cmd commands", "comands", async (msg, p) => {
                string reply = ListCommands(p["command"], true);
                await msg.ReplyAsync(reply, allowedMentions: AllowedMentions.None);
            }, [new Param("command", "the command to get help for (if empty, just lists all)", "")]),

            new("params", "reply with all params", "testing", async (msg, p) => {
                var keys = p.Keys;
                foreach (var value in p.Values) {
                    await msg.ReplyAsync((string)value);
                }
                // await msg.ReplyAsync(p);
            }, [new Param("test", "", 0)]),

            new("c4", "MANIPULATE connect 4", "games", async (msg, p) => {
                if (MainHook.instance.c4 == null) MainHook.instance.c4 = new();
                MainHook.instance.c4.SetColumn(p["column"], p["player"]);
            }, [ new("column", "", 0), new("player", "", 1) ]),
            
            new("c4get", "GET connect 4", "games", async (msg, p) => {
                if (MainHook.instance.c4 == null) MainHook.instance.c4 = new();
                await msg.ReplyAsync(((int)MainHook.instance.c4.GetElement(p["x"], p["y"])).ToString());
            }, [ new("x", "", 0), new("y", "", 0) ]),
            
            new("c4display", "DISPLAY connect 4", "games", async (msg, p) => {
                if (MainHook.instance.c4 == null) MainHook.instance.c4 = new();
                await msg.ReplyAsync(MainHook.instance.c4.DisplayBoard());
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
                    await msg.ReplyAsync("darn. no server with that name");
                } else {
                    // var invite = await guild.GetVanityInviteAsync();
                    // await msg.ReplyAsync(invite.Url);
                    var invite = await guild.GetInvitesAsync();
                    await msg.ReplyAsync(invite.First().Url);
                }
            }, [ new("serverName", "the name of the server to make an invite for", "") ]),

            new("test", "testing code out", "testing", async (msg, p) => {
                foreach (string str in p["params"]) {
                    await msg.ReplyAsync(str);
                }
            }, [ new("test1", "for testing", ""), new("test2", "for testing: electric boogaloo", ""), new("params", "params!!!", "") ]),
        ];
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private static readonly Dictionary<string, Command> Commands = [];
        private static readonly Dictionary<string, Command> CmdCommands = [];

        // public static CommandHandler instance = new();

        // might wanna make it return what the command.func returns, though i don't know how helpful that would be
        public async static Task<bool> ParseCommand(SocketUserMessage msg, string command, string parameters = "")
        {
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
                                    await msg.ReplyAsync($"incorrect param name! use \"{MainHook.PREFIX}help {command}\" to get params for {command}.");
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
            
            try {
                await com.func.Invoke(msg, paramDict);
                return true;
            } catch (System.Exception err) {
                await msg.ReplyAsync(err.ToString());
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
            string[] commands = singleCom != "" ? [singleCom] : commandDict.Keys.ToArray();
            for (int i = 0; i < commands.Length; i++) {
                Command com = commandDict[commands[i]];
                if (com.genre == "hidden" && !showHidden) continue;
                var paramNames = com.parameters?.Select(x => x.name);
                string joinedParams = "";
                if (paramNames?.FirstOrDefault() != null) {
                    joinedParams = $"{string.Join(", ", paramNames)}";
                }

                response.Add($"{MainHook.PREFIX}{commands[i]} ({joinedParams}) : {com.desc}\n");
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
