using System;
using System.Diagnostics;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Frozen; // frozen dictionary doesn't seem very important

using Discord;
using Discord.WebSocket;

using CaretakerNET.ExternalEmojis;
using CaretakerNET.Games;
using CaretakerNET.Core;

namespace CaretakerNET.Commands
{
    public class CommandHandler
    {
        public enum ParseFailType
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static readonly Command[] commands = [
            new("help", "list all normal commands", "commands", async (msg, p) => {
                // await msg.Reply((string)p["command"]);
                string reply = ListCommands(p["command"]);
                await msg.Reply(reply, false);
            }, [ 
                new Param("command", "the command to get help for (if empty, just lists all)", ""),
                new Param("listParams", "list parameters?", false)
            ]),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                string reply = (string)p["reply"];
                if (!reply.Contains("@everyone") && !reply.Contains("@here") && reply != "") {
                    await Task.Delay(int.Abs(p["wait"]));
                    await msg.Reply((string)p["reply"], false);
                } else {
                    string[] replies = [ "stop that!!!", "hey you can't do that :(", "explode", "why...", Emojis.Sab ];
                    await msg.Reply(p["reply"] != "" ? replies.GetRandom()! : Emojis.Sab);
                }
            }, [
                new Param("reply", "the message to echo", Emojis.Smide),
                new Param("wait", "how long to wait until replying", 0),
            ]),

            new("math", "do math!", "silly", async (msg, p) => {
                DataTable dt = new();
                string reply = ((string)p["math"]).Replace(" ", "");
                reply = reply switch {
                    "9+10" => "21",
                    _ => dt.Compute(reply, null)?.ToString() ?? "null",
                };
                await msg.Reply(reply);
            }, [ new Param("math", "the math to do", "2 + 2"), ]),

            new("count", "set the counting channel", "silly", async (msg, p) => {
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
                ITextChannel? channel = p["channel"] ?? msg.Channel; // TryGetGuildData makes sure this is done in a guild, so this is 99.9% not null?
                if (p["reset"]) {
                    s.count.Reset(true);
                }
                s.count.Channel = channel; 
                await msg.AddReactionAsync(Emoji.Parse("✅"));
            }, [
                new Param("channel", "the channel to count in", "", "channel"),
                new Param("reset", "reset everything?", true),
            ], [ [  ] ]),

            new("flower", "Hiiii! " + Emojis.TalkingFlower, "silly", async (msg, p) => {

            }, [new Param("fileName", "what the file will be renamed to", "")]),

            new("c4go", "play connect 4 using this command!", "hidden", async (msg, p) => {

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
                        Emoji? checkmark = Emoji.Parse("✅");
                        Emoji? crossmark = Emoji.Parse("❌");
                        // var checkmarkReaction = await challengeMsg.AddReactionAsync(checkmark);
                        await challengeMsg.AddReactionAsync(checkmark);
                        await challengeMsg.AddReactionAsync(crossmark);
                        for (int i = 0; i < 60; i++)
                        {
                            if (!challengeMsg.Reactions.ContainsKey(checkmark)) continue;
                            int reactionCount = challengeMsg.Reactions.ContainsKey(checkmark) ? challengeMsg.Reactions[checkmark].ReactionCount : 0;
                            var acceptedUser = 
                            (await challengeMsg.GetReactionUsersAsync(checkmark, reactionCount)
                                .FlattenAsync())
                                .First(user => !user.IsBot && user.Id == victim.Id);
                            accepted = true;
                            if (acceptedUser != null) break;
                            // Caretaker.Log("Hmmmmm " + i);
                            await Task.Delay(1000);
                        }
                        if (accepted) {
                            if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
                            s.connectFour = new(msg.Author.Id, victim.Id);
                            await msg.Reply($"{Caretaker.UserPingFromID(msg.Author.Id)} and {Caretaker.UserPingFromID(victim.Id)}, begin!");
                        } else {
                            var prevContent = challengeMsg.Content;
                            _ = challengeMsg.ModifyAsync(msg => msg.Content = $"*{prevContent}*\ntook too long! oops.");
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
                    } else if (msg.Content == ">hello world") { // silly workaround, i wonder if i should improve this
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
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
                var c4 = s.connectFour ??= new();
                c4.AddToColumn(p["column"], p["player"]);
            }, [ new("column", "", 0), new("player", "", 1) ]),
            
            new("c4get", "GET connect 4", "games", async (msg, p) => {
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
                var c4 = s.connectFour ??= new();
                await msg.Reply(((int)c4.ElementAt(p["x"], p["y"])).ToString());
            }, [ new("x", "", 0), new("y", "", 0) ]),
            
            new("c4display", "DISPLAY connect 4", "games", async (msg, p) => {
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
                var c4 = s.connectFour ??= new();
                await msg.Reply(c4.DisplayBoard());
                var win = c4.WinCheck();
                if (win.winningPlayer != ConnectFour.Player.None) await msg.Reply(win.winningPlayer);
            }, [ new("x", "", 0), new("y", "", 0) ]),

            new("guilds", "get all guilds", "hidden", async (msg, p) => {
                var client = MainHook.instance.Client;
                Caretaker.LogInfo(client.Guilds.Count);
                foreach (var guild in client.Guilds) {
                    Caretaker.LogInfo(guild.Name);
                }
            }),

            new("invite", "make an invite to a guild i'm in", "hidden", async (msg, p) => {
                SocketGuild? guild = p["guild"];
                if (guild == null) {
                    await msg.Reply("darn. no guild with that name");
                } else {
                    // Caretaker.LogTemp(guild.Name);
                    var invite = await guild.GetInvitesAsync();
                    await msg.Reply(invite.First().Url);
                }
            }, [ new("guild", "the name of the guild to make an invite for", "", "guild") ]),

            new("talkingChannel", "set the channel that Console.ReadLine() will send to", "hidden", async (msg, p) => {
                SocketGuild? guild = p["guild"] ?? msg.GetGuild();
                if (guild == null) {
                    await msg.Reply("mmm... nope.");
                    return;
                }
                MainHook.instance.talkingChannel = (ITextChannel?)(string.IsNullOrEmpty(p["channel"]) ? guild.ParseChannel((string)p["channel"]) : msg.Channel);
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

                var guild = msg.GetGuild();

                int i = 0; // make i global so it can be used later
                for (i = 0; i < com.parameters.Length; i++)
                {
                    Param? setParam = com.parameters[i]; // the Param the name/preset/type are being grabbed from
                    dynamic? value;
                    
                    if (splitParams.IsIndexValid(i)) { // will this parameter be set manually?
                        int colon = splitParams[i].IndexOf(';'); // used to be a colon, but there's not a reliable(/efficient) check if it's an emoji or not
                        string valueStr = splitParams[i].ReplaceAll(space, ' '); // get the spaces back
                        if (colon != -1) { // if colon exists, attempt to set settingParam to the string before the colon
                            string paramName = splitParams[i][..colon];
                            if (paramName == "params" && com.inf != null) { // if it's params, break this loop and start adding to the params
                                // if inf params are needed, grab everything after
                                var paramsAsInfTypes = splitParams.Skip(i + 1)
                                                                  .Select(splitParam => com.inf.ToType(splitParam, guild));
                                paramDict.TryAdd("params", paramsAsInfTypes.ToArray());
                                break;
                            }
                            valueStr = splitParams[i][(colon + 1)..];
                            setParam = Array.Find(com.parameters, x => x.name == paramName);
                            if (setParam == null) {
                                await msg.Reply($"incorrect param name! use \"{MainHook.PREFIX}help {command}\" to get params for {command}.");
                                return false;
                            }
                        }
                        value = setParam.ToType(valueStr, guild);
                    } else {
                        var p = setParam.preset;
                        value = p.GetType() == typeof(string) ? setParam.ToType(p, guild) : p;
                    }

                    // bool success = paramDict.TryAdd(setParam.name, value);
                    paramDict.TryAdd(setParam.name, value);
                }
            }

            stopwatch.Stop();
            Caretaker.LogDebug($"took {stopwatch.Elapsed.TotalMilliseconds} ms to parse parameters", true);
            
            try {
                await com.func.Invoke(msg, paramDict);
                return true;
            } catch (System.Exception err) {
                await msg.Reply(err.Message);
                return false;
            }
        }
        
        public static string ListCommands(string singleCom, bool cmd = false, bool showHidden = false)
        {
            var commandDict = cmd ? CmdCommands : Commands;
            if (singleCom != "" && !commandDict.ContainsKey(singleCom)) {
                return $"{singleCom} is NOT a command. try again :/";
            }
            // List<string> response = [];
            StringBuilder response = new();
            string[] commandKeys = singleCom != "" ? [ singleCom ] : commandDict.Keys.ToArray();
            for (int i = 0; i < commandKeys.Length; i++)
            {
                var com = commandDict[commandKeys[i]];
                if (commandKeys.IsIndexValid(i - 1) && commandDict[commandKeys[i - 1]].name == com.name) continue;
                if (com.genre == "hidden" && !showHidden) continue;
                response.Append($"{MainHook.PREFIX}{com.name} (");
                // string joinedParams = "";
                if (com.parameters != null) {
                    IEnumerable<string> paramNames = com.parameters.Select(x => x.name);
                    response.Append(string.Join(", ", paramNames) + (com.inf != null ? ", params" : ""));
                }

                response.Append($") : {com.desc}\n");
            }
            return response.Length > 0 ? response.ToString() : "mmm... no.";
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
