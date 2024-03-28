using System;
using System.Diagnostics;
using System.Linq;
using System.Data;
using System.Text;

using Discord;
using Discord.WebSocket;

using CaretakerNET.ExternalEmojis;
using CaretakerNET.Games;

using org.mariuszgromada.math.mxparser;
using Renci.SshNet;
using FuzzySharp;

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
                string reply = ListCommands(p["command"], p["listParams"]);
                await msg.Reply(reply, false);
            }, [ 
                new Param("command", "the command to get help for (if empty, just lists all)", ""),
                new Param("listParams", "list parameters?", false)
            ]),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                (string reply, int wait) = (p["reply"], p["wait"]);
                if (!reply.Contains("@everyone") && !reply.Contains("@here") && reply != "") {
                    await Task.Delay(int.Abs(wait));
                    await msg.Reply(reply, false);
                } else {
                    string[] replies = [ "stop that!!!", "hey you can't do that :(", "explode", "why...", Emojis.Sab ];
                    await msg.Reply(reply != "" ? replies.GetRandom()! : Emojis.Sab);
                }
            }, [
                new Param("reply", "the message to echo", Emojis.Smide),
                new Param("wait", "how long to wait until replying", 0),
            ], timeout: 4000),

            new("math", "do math!", "silly", async (msg, p) => {
                string math = p["math"];
                string reply = ((string)p["math"]).Replace(" ", "");
                var expression = new Expression(reply);
                reply = reply switch {
                    "9+10" => "21",
                    _ => expression.calculate().ToString() ?? "null",
                };
                await msg.Reply(reply);
            }, [ new Param("math", "the math to do", "NaN"), ]),

            new("count", "set the counting channel", "silly", async (msg, p) => {
                (ITextChannel? channel, bool reset) = (p["channel"], p["reset"]);
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist s) || s == null) return;
                ITextChannel? countChannel = channel ?? (ITextChannel?)msg.Channel; // TryGetGuildData makes sure this is done in a guild, so this is 99.9% not null?
                if (reset) {
                    s.count.Reset(true);
                }
                s.count.Channel = countChannel; 
                await msg.ReactAsync("✅");
            }, [
                new Param("channel", "the channel to count in", "", Param.ParamType.Channel),
                new Param("reset", "reset everything?", true),
            ], [ ChannelPermission.ManageChannels ]),

            new("countSet", "set the current count", "silly", async (msg, p) => {
                int newCount = p["newCount"];
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist s)) return;
                if (s.count?.Channel != null) {
                    _ = msg.ReactAsync("✅");
                    s.count.Current = newCount;
                } else {
                    _ = msg.ReactAsync("❌");
                    await msg.Reply("couldn't get the guild data!");
                }
            }, [
                new Param("newCount", "the new current count", 0),
            ], [ ChannelPermission.ManageChannels ]),

            new("jerma", "Okay, if I... if I chop you up in a meat grinder, and the only thing that comes out, that's left of you, is your eyeball, you'r- you're PROBABLY DEAD!", "silly", async (msg, p) => {
                string fileName = p["fileName"];
                var connectionInfo = new ConnectionInfo(
                    "150.230.169.222", "opc",
                    new PrivateKeyAuthenticationMethod("opc", new PrivateKeyFile(Path.Combine(PRIVATES_PATH, "ssh.key")))
                );
                // using (var client = new ScpClient(connectionInfo))
                using var client = new SftpClient(connectionInfo);
                await msg.ReactAsync("✅");
                try {
                    client.Connect();
                    const string remoteDirectory = "/home/opc/mediaHosting/jermaSFX/";
                    var files = client.ListDirectory(remoteDirectory);
                    if (files == null) {
                        LogError("\"files\" was null.");
                        _ = msg.RemoveAllReactionsAsync();
                        _ = msg.ReactAsync("❌");
                        return;
                    }
                    var randomFile = files.GetRandom()!;
                    string path = randomFile.FullName.SplitByLastChar('/').Item1 + fileName;
                    // Log(path);
                    Stream newFile = File.Create(path);
                    client.DownloadFile(randomFile.FullName, newFile, async id => {
                        newFile.Dispose();
                        await msg.Channel.SendFileAsync(path, messageReference: msg.Reference);
                        File.Delete(path);
                    });
                } catch (Exception err) {
                    _ = msg.RemoveAllReactionsAsync();
                    _ = msg.ReactAsync("❌");
                    LogError(err);
                    throw;
                }
            }, [new Param("fileName", "what the file will be renamed to", "")]),

            new("flower", "Hiiii! " + Emojis.TalkingFlower, "silly", async (msg, p) => {
                var connectionInfo = new ConnectionInfo(
                    "150.230.169.222", "opc",
                    new PrivateKeyAuthenticationMethod("opc", new PrivateKeyFile(Path.Combine(PRIVATES_PATH, "ssh.key")))
                );
                // using (var client = new ScpClient(connectionInfo))
                using var client = new SftpClient(connectionInfo);
                await msg.ReactAsync("✅");
                try {
                    client.Connect();
                    const string remoteDirectory = "/home/opc/mediaHosting/jermaSFX/";
                    var files = client.ListDirectory(remoteDirectory);
                    if (files == null) {
                        await msg.ReactAsync("❌");
                        return;
                    }
                    var randomFile = files.GetRandom()!;
                    Stream newFile = File.OpenRead(randomFile.Name);
                    client.DownloadFile(randomFile.FullName, newFile);
                    await msg.Reply(files.Count());
                    await msg.Channel.SendFileAsync(randomFile.Name);
                } catch (Exception err) {
                    await msg.ReactAsync("❌");
                    LogError(err);
                    throw;
                }
            }, [new Param("fileName", "what the file will be renamed to", "")]),

            new("challenge", "challenge another user to a game", "games", async (msg, p) => {
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
                (string game, IUser? victim) = (p["game"], (p["victim"] ?? msg.ReferencedMessage?.Author));
                bool anyone = p.Unparams["victim"] is "any" or "anyone" or "<@&1219981878895968279>";
                string? thrown = null;
                if (s.CurrentGame != null) {
                    thrown = "there's a game already in play!!";
                } else if (victim == null && !anyone) {
                    thrown = "hmm... seems like the user you tried to challenge is unavailable.";
                } else if (victim?.Id == msg.Author.Id) {
                    thrown = "MainHook.BannedUsers.Add(msg.Author.Id); " + Emojis.Smide;
                    MainHook.BannedUsers.Add(msg.Author.Id);
                } else if (victim?.IsBot ?? false) {
                    thrown = "dude. that's a bot.";
                }
                if (thrown != null) {
                    _ = msg.Reply(thrown);
                    return;
                }
                int cFourChance =    Fuzz.WeightedRatio(game, "connect4");
                int checkersChance = Fuzz.WeightedRatio(game, "checkers");
                int unoChance =      Fuzz.WeightedRatio(game, "uno");
                if ((new int[] {cFourChance, checkersChance, unoChance}).All(c => c <= 60)) { // if none of them are valid
                    await msg.Reply("that's not a game!");
                    return;
                }
                var challengeMsg = await msg.Reply($"{(victim != null ? UserPingFromID(victim.Id) : "anybody here")}, do you accept {UserPingFromID(msg.Author.Id)}'s challenge?");
                bool destroyed = false;
                Emoji? checkmark = Emoji.Parse("✅"); Emoji? crossmark = Emoji.Parse("❌");
                _ = challengeMsg.AddReactionAsync(checkmark);
                if (!anyone) _ = challengeMsg.AddReactionAsync(crossmark);

                async Task ReactionCheck(Cacheable<IUserMessage, ulong> messageCache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
                {
                    if (destroyed) return; // failsafe
                    var message = messageCache.Value;
                    IUser reactUser = reaction.User.Value;
                    ulong ruId = reactUser.Id;
                    if (message.Id == challengeMsg.Id && ruId != CARETAKER_ID) {
                        Log("ruId : " + ruId);
                        // if it's not the challenger, the victim, or caretaker (and it's not for anyone) remove the reaction
                        if (ruId != msg.Author.Id && ruId != victim?.Id && !anyone) {
                            await challengeMsg.RemoveReactionAsync(reaction.Emote, reactUser);
                            return;
                        }
                        Log("reaction.Emote : " + reaction.Emote);
                        Log("reaction.Emote.Name : " + reaction.Emote.Name);
                        if (reaction.Emote.Name == "✅") {
                            if (ruId == victim?.Id || anyone) {
                                if (cFourChance > 60) {
                                    s.CurrentGame = new ConnectFour(challengeMsg.Channel.Id, msg.Author.Id, ruId);
                                } else if (checkersChance > 60) {
                                    s.CurrentGame = new Checkers(challengeMsg.Channel.Id, msg.Author.Id, ruId);
                                    await msg.Reply("not implemented yet! soon tho");
                                } else if (unoChance > 60) {
                                    await msg.Reply("not implemented yet! soon tho");
                                } else {
                                    _ = msg.Reply("okay this error should literally never happen. " + UserPingFromID(ASTRL_ID), true);
                                }
                                await challengeMsg.Reply($"{UserPingFromID(msg.Author.Id)} and {UserPingFromID(ruId)}, begin!");
                                DestroySelf();
                            }
                        } else if (reaction.Emote.Name == "❌") {
                            if ((ruId == victim?.Id || ruId == msg.Author.Id) && !anyone) {
                                await challengeMsg.Reply("awww... they denied. 😢");
                                DestroySelf();
                            }
                        }
                    }
                }
                MainHook.instance.Client.ReactionAdded += ReactionCheck;

                void DestroySelf()
                {
                    if (destroyed) return;
                    destroyed = true;
                    MainHook.instance.Client.ReactionAdded -= ReactionCheck;
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                while (true)
                {
                    if (destroyed) return; // end loop if already destroyed

                    if (stopwatch.Elapsed.TotalSeconds >= 60 || s.CurrentGame != null) {
                        _ = challengeMsg.RemoveAllReactionsAsync();
                        string newMsg = s.CurrentGame != null ? "sorryyy another game started in the middle of you asking :/" :  "took too long! oops.";
                        _ = challengeMsg.OverwriteMessage(newMsg);
                        stopwatch.Stop();
                        break;
                    }
                    await Task.Delay(1000);
                }
                DestroySelf();
            }, [
                new Param("victim", "the username/display name of the person you'd like to challenge", "anyone", Param.ParamType.User),
                new Param("game", "which game would you like to challenge with?", "connect4"),
            ]),

            new("leaderboard", "see the top ranking individuals on this bot", "games", async (msg, p) => {
                (int amount, bool loserboard) = (p["amount"], p["loserboard"]);
                var topUsers = MainHook.instance.UserData.OrderByDescending((x) => !loserboard ? x.Value.Wins.Count : x.Value.Losses.Count).Take(amount);
                StringBuilder desc = new();
                int i = 0;
                foreach ((ulong id, var u) in topUsers)
                {
                    desc.Append(i + 1);
                    desc.Append(". ");
                    desc.Append(UserPingFromID(id));
                    desc.Append(" - ");
                    desc.AppendLine((!loserboard ? u.Wins.Count : u.Losses.Count).ToString());
                    i++;
                }
                var leaderboard = new EmbedBuilder {
                    Title = !loserboard ? "Leaderboard" : "Loserboard",
                    Description = desc.ToString(),
                };
                await msg.ReplyAsync(embed: leaderboard.Build());
            }, [
                new Param("loserboard", "get the people who have LOST the most instead", false),
                new Param("amount", "the amount of people to grab for the leaderboard", 10),
            ]),

            new("hello, hi", "say hi to a user", "silly", async (msg, p) => {
                IUser user = p["user"];
                if (user != null) {
                    if (user.Id == CARETAKER_ID) {
                        await msg.Reply("aw hii :3");
                    } else if (user.IsBot) {
                        await msg.RandomReply([
                            "that's... a bot.", 
                            "i don't think they'll even know you said hi", 
                            "bots. aren't. people.", 
                            "i can't reach that person right now :( maybe just send them a normal hello\n(or DON'T because they're a BOT??)"
                        ]);
                    } else if (user.Id == msg.Author.Id) {
                        await msg.RandomReply([
                            Emojis.Sab,
                            ":(", 
                            "you can just say hello to somebody else..!",
                            "you MUST know other people here, right?",
                            "why r u like this",
                        ]);
                    } else {
                        string name = string.IsNullOrEmpty(msg.Author.GlobalName) ? msg.Author.Username : $"{msg.Author.GlobalName} ({msg.Author.Username})";
                        string from = !IsNull(msg.GetGuild(), out var msgGuild) ? " from " + msgGuild!.Name : "";
                        _ = msg.ReactAsync("✅");
                        await user.SendMessageAsync(name + from + " says hi!");
                    }
                } else if (p.Unparams["user"] is "world" or "world!") { // the entire reason why Unparams exists
                    await msg.Reply("Hello, world!");
                } else {
                    await msg.Reply($"i can't reach that person right now :( maybe just send them a normal hello");
                }
            }, [ new Param("user", "the username of the person you'd like to say hi to", CARETAKER_ID.ToString(), Param.ParamType.User) ]),

            new("playtest", "give yourself the playtester role, or dm you an invite to the caretaker server if it's the wrong server", "caretaker", async (msg, p) => {
                var guild = msg.GetGuild(); // remember, returns null in dms
                var invite = await MainHook.instance.Client.GetGuild(CARETAKER_CENTRAL_ID).GetBestInvite();
                if (invite == null && guild?.Id != CARETAKER_CENTRAL_ID) { // handle no invite being found but only if you need it
                    _ = msg.Reply("okay so apparently there's no invite for the caretaker central server. oops");
                    return;
                }
                switch (guild?.Id)
                {
                    // caretaker server
                    case CARETAKER_CENTRAL_ID: {
                        IGuildUser user = (IGuildUser)msg.Author;
                        var role = guild.Roles.FirstOrDefault(x => x.Name == "Playtester");
                        if (user.RoleIds.Any(x => x == role?.Id)) {
                            // _ = user.RemoveRoleAsync(role);
                            // _ = msg.ReactAsync("❌");
                            // _ = msg.Reply("okay... :(");
                            _ = msg.Reply("You're stuck here now.");
                        } else {
                            _ = user.AddRoleAsync(role);
                            _ = msg.ReactAsync("✅");
                            _ = msg.Reply("thanks :3");
                        }
                    } break;
                    // dms
                    case null: {
                        _ = msg.Channel.SendMessageAsync("ermm " + invite!.Url);
                    } break;
                    // any other server
                    default: {
                        _ = msg.ReactAsync(Emojis.Smide);
                        _ = msg.Reply("Check your dms.");
                        _ = msg.Author.SendMessageAsync("here's where you can ACTUALLY use that command! :D\n" + invite!.Url);
                    } break;
                }
            }),

            new("test", "for testing :)", "silly", async (msg, p) => {
                var builder = new ComponentBuilder()
                    .WithButton("label", "show-cards");

                await msg.ReplyAsync("button", components: builder.Build());
            }),

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            new("cmd", "run more internal commands, will probably just be limited to astrl", "internal"),
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            new("help", "list all cmd commands", "commands", async (msg, p) => {
                string reply = ListCommands(p["command"], p["listParams"], true);
                await msg.Reply(reply, false);
            }, [
                new Param("command", "the command to get help for (if empty, just lists all)", ""),
                new Param("listParams", "list parameters?", false)
            ]),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                string reply = p["reply"];
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

            new("save", "save _s and _u", "internal", async (_, _) => await MainHook.instance.Save()),
            new("load", "save _s and _u", "internal", async (_, _) => await MainHook.instance.Load()),

            new("cancelGame", "cancels the current game in play on the server", "games", async(msg, p) => {
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist s) || s == null) return;
                s.CurrentGame = null;
                _ = msg.ReactAsync("✅");
            }),

            new("guilds", "get all guilds", "hidden", async delegate {
                var client = MainHook.instance.Client;
                LogInfo(client.Guilds.Count);
                foreach (var guild in client.Guilds) {
                    LogInfo(guild.Name);
                }
            }),

            new("invite", "make an invite to a guild i'm in", "hidden", async (msg, p) => {
                SocketGuild? guild = p["guild"];
                if (guild == null) {
                    await msg.Reply("darn. no guild with that name");
                } else {
                    var invite = await guild.GetInvitesAsync();
                    await msg.Reply(invite.First().Url);
                }
            }, [ new("guild", "the name of the guild to make an invite for", "", Param.ParamType.Guild) ]),

            new("talkingChannel", "set the channel that Console.ReadLine() will send to", "hidden", async (msg, p) => {
                SocketGuild? guild = p["guild"] ?? msg.GetGuild();
                if (guild == null) {
                    await msg.Reply("mmm... nope.");
                    return;
                }
                MainHook.instance.TalkingChannel = (ITextChannel?)(string.IsNullOrEmpty(p["channel"]) ? guild.ParseChannel((string)p["channel"]) : msg.Channel);
            }, [ new("channel", "the channel to talk in", ""), new("guild", "the guild to talk in", "", Param.ParamType.Guild) ]),

            new("kill", "kills the bot", "hidden", async (msg, p) => {
                await Task.Delay((int)p["delay"]);
                MainHook.instance.Stop();
            }, [ new("delay", "the time to wait before the inevitable end", 0) ]),

            new("test", "testing code out", "testing", async (msg, p) => {

            }, [ new("test1", "for testing", ""), new("test2", "for testing: electric boogaloo", ""), new("params", "params!!!", "") ]),
        ];
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private static readonly Dictionary<string, Command> Commands = [];
        private static readonly Dictionary<string, Command> CmdCommands = [];

        // public static CommandHandler instance = new();

        public static (Command?, string) ParseCommand(string command, string parameters)
        {
            var whichComms = Commands;
            if (command == "cmd") {
                whichComms = CmdCommands;
                (command, parameters) = parameters.SplitByFirstChar(' ');
            }

            return (whichComms[command.ToLower()], parameters);
        }
        // might wanna make it return what the command.func returns, though i don't know how helpful that would be
        public async static Task<bool> DoCommand(IUserMessage msg, Command com, string parameters)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Dictionary<string, dynamic> paramDict = [];
            Dictionary<string, string?> unparamDict = [];
            string[] unparams = [];
            if (com.Params != null && (com.Params.Length > 0 || com.Inf != null)) {
                const char SPACE = '↭';
                string[] splitParams = [];
                if (parameters != "") { // only do these checks if there are parameters to check for
                    if (parameters.Contains('"')) {
                        string[] quoteSplit = parameters.Split('"');
                        
                        for (int x = 1; x < quoteSplit.Length; x += 2) { 
                            // check every other section (they will always be "in" double quotes) 
                            // and check if it actually has spaces needed to be replaced
                            if (quoteSplit[x].Contains(' ')) {
                                quoteSplit[x] = quoteSplit[x].ReplaceAll(' ', SPACE);
                            }
                        }
                        parameters = string.Join("", quoteSplit); // join everything back together
                    }
                    splitParams = parameters.Split(' '); // then split it up as parameters
                }

                LogDebug("splitParams    // " + string.Join(", ", splitParams));
                LogDebug("com.parameters // " + string.Join(", ", com.Params.Select(x => x.Name)));

                var guild = msg.GetGuild();

                int i = 0; // make i global so it can be used later
                int j = 0; // second iterator variable
                bool paramsAdded = false;
                for (i = 0; i < com.Params.Length; i++)
                {
                    Param? setParam; // the Param the name/preset/type are being grabbed from
                    dynamic? value;
                    
                    if (splitParams.IsIndexValid(i) && !paramsAdded) { // will this parameter be set manually?
                        setParam = com.Params[i];
                        int colonIndex = splitParams[i].IndexOf('='); // used to be a colon, but there's not a reliable(/efficient) check if it's an emoji or not
                        string valueStr = splitParams[i].ReplaceAll(SPACE, ' '); // get the spaces back
                        if (colonIndex != -1) { // if colon exists, attempt to set settingParam to the string before the colon
                            string paramName = splitParams[i][..colonIndex];
                            if (paramName == "params" && com.Inf != null) { // if it's params, add the rest as the type of params and break the loop
                                // if inf params are needed, grab everything after
                                var unparamKeys = splitParams.Skip(i + 1);
                                unparams = unparamKeys.ToArray();
                                paramDict.TryAdd("params", unparamKeys.Select(splitParam => com.Inf.ToType(splitParam, guild)).ToArray());
                                paramsAdded = true;
                                continue;
                            }
                            valueStr = splitParams[i][(colonIndex + 1)..];
                            setParam = Array.Find(com.Params, x => x.Name == paramName);
                            if (setParam == null) {
                                await msg.Reply($"incorrect param name! use \"{PREFIX}help {com.Name}\" to get params for {com.Name}.");
                                return false;
                            }
                        } else {
                            j++;
                        }
                        value = setParam.ToType(valueStr, guild);
                        unparamDict.TryAdd(setParam.Name, valueStr);
                    } else {
                        setParam = com.Params[j];
                        var p = setParam.Preset;
                        value = p.GetType() == typeof(string) ? setParam.ToType(p, guild) : p;
                        Log(value);
                        j++;
                        unparamDict.TryAdd(setParam.Name, null);
                    }

                    paramDict.TryAdd(setParam.Name, value);
                }
            }

            stopwatch.Stop();
            LogDebug($"took {stopwatch.Elapsed.TotalMilliseconds} ms to parse parameters", true);
            
            await com.Func.Invoke(msg, new Command.ParsedParams(paramDict, unparamDict, unparams));
            return true;
            // try {
            //     await com.Func.Invoke(msg, new Command.ParsedParams(paramDict, unparamDict, unparams));
            //     return true;
            // } catch (Exception err) {
            //     LogError(err);
            //     await msg.Reply(err.Message, false);
            //     return false;
            // }
        }

        public static string ListCommands(string singleCom, bool listParams, bool cmd = false, bool showHidden = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var commandDict = cmd ? CmdCommands : Commands;
            if (singleCom != "" && !commandDict.ContainsKey(singleCom)) {
                return $"{singleCom} is NOT a command. try again :/";
            }
            StringBuilder response = new();
            string[] commandKeys = singleCom != "" ? [ singleCom ] : [ ..commandDict.Keys ];
            for (int i = 0; i < commandKeys.Length; i++)
            {
                var com = commandDict[commandKeys[i]];
                if ((commandKeys.IsIndexValid(i - 1) && commandDict[commandKeys[i - 1]].Name == com.Name) || 
                    (com.Genre == "hidden" && !showHidden)) {
                    continue;
                }
                response.Append(PREFIX);
                response.Append(com.Name);
                if (com.Params != null && !listParams) {
                    IEnumerable<string> paramNames = com.Params.Select(x => x.Name);
                    response.Append(" (");
                    response.Append(string.Join(", ", paramNames));
                    if (com.Inf != null) response.Append(", params");
                    response.Append(')');
                }

                response.Append(" : ");
                response.AppendLine(com.Desc);

                if (com.Params != null && listParams) {
                    foreach (var param in com.Params) {
                        response.Append("\t-");
                        response.Append(param.Name);
                        response.Append(" : ");
                        response.AppendLine(param.Desc);
                    }
                }
            }
            sw.Stop();
            LogDebug($"ListCommands() took {sw.ElapsedMilliseconds} milliseconds to complete");
            return response.Length > 0 ? response.ToString() : "mmm... no.";
        }

        // currently an instance isn't needed; try avoiding one?
        // i.e just put the logic that would need an instance in a different script
        public static void Init()
        {
            // commands = ;
            var whichComms = Commands;
            foreach (var command in commands) {
                var commandNames = command.Name.Split(", ");
                foreach (var commandName in commandNames) {
                    whichComms.Add(commandName.ToLower(), command);
                }
                
                if (command.Name == "cmd") {
                    whichComms = CmdCommands;
                }
            }
        }
    }
}
