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
    public static class CommandHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static readonly Command[] commands = [
            new("prefix", "set the prefix for the current guild", "commands", async (msg, p) => {
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist s) || s == null) return;
                s.Prefix = string.IsNullOrEmpty(p["prefix"]) ? ">" : p["prefix"]!;
                _ = msg.ReactAsync("✅");
            }, [
                new Param("prefix", $"the prefix to set to. if empty, resets to \"{DEFAULT_PREFIX}\"", "")
            ], [ ChannelPermission.ManageChannels ]),

            new("help, listCommands", "list all normal commands", "commands", async (msg, p) => {
                string command = p["command"] ?? "";
                (string, string)[]? commandLists = ListCommands(msg, command, p["listParams"]);
                if (commandLists == null) {
                    await msg.Reply(
                        $"{command} is NOT a command. try again :/" + (string.IsNullOrEmpty(command) ? "\nor something just went wrong. idk" : ""),
                        false
                    );
                    return;
                }
                if (commandLists.Length <= 0) {
                    _ = msg.Reply("commandLists was empty!");
                    return;
                }

                var dictIndex = 0;
                Dictionary<string, Embed> commandEmbeds = commandLists.Select((genreAndCom, index) => {
                    (string genre, string com) = genreAndCom;
                    return (new EmbedBuilder {
                        Title = "commands : " + genre /* + $" ({index + 1} out of {commandLists.Length})" */,
                        Description = com
                    }).WithCurrentTimestamp().Build();
                }).ToDictionary(x => commandLists[dictIndex++].Item1);

                var components = new ComponentBuilder {
                    ActionRows = [
                        new ActionRowBuilder().WithSelectMenu(
                            "commands",
                            commandLists.Select((genreAndCom) => new SelectMenuOptionBuilder(genreAndCom.Item1, genreAndCom.Item1)).ToList(),
                            "command select!!"
                        )
                    ]
                };

                var helpMsg = await msg.ReplyAsync(components: components.Build(), embed: commandEmbeds.Values.ElementAt(0));

                async Task<bool> OnDropdownChange(SocketMessageComponent args)
                {
                    IUser reactUser = args.User;
                    IMessage message = args.Message;
                    ulong ruId = reactUser.Id;
                    if (message.Id != helpMsg.Id || ruId == CARETAKER_ID) {
                        return false;
                    }

                    // if it's not the victim nor the caretaker, ephemerally tell them they can't use it
                    if (ruId != msg.Author.Id) {
                        _ = args.RespondAsync("not for you.", ephemeral: true);
                        return false;
                    }

                    if (args.Data.CustomId is "commands") {
                        var genre = string.Join("", args.Data.Values);
                        await helpMsg.ModifyAsync(m => m.Embed = commandEmbeds[genre]);
                    }

                    return false;
                }
                
                using var rs = new ComponentSubscribe(OnDropdownChange, helpMsg);
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (stopwatch.Elapsed.TotalSeconds < 60 && !rs.Destroyed) {
                        // await Task.Delay(1000);
                    }
                    stopwatch.Stop();
                rs.Dispose();
            }, [ 
                new Param("command", "the command to get help for (if empty, just lists all)", ""),
                new Param("listParams", "list parameters?", false)
            ]),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                (string? reply, int wait) = (p["reply"], p["wait"]);
                if (string.IsNullOrEmpty(reply)) return;
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

            new("hello, hi", "say hi to a user", "silly", async (msg, p) => {
                IUser? user = p["user"];
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

            new("math, calc, calculator", "do math!", "silly", async (msg, p) => {
                string? math = p["math"];
                if (string.IsNullOrEmpty(math)) return;
                string reply = math.Replace(" ", "");
                var expression = new Expression(reply);
                reply = reply switch {
                    "9+10" => "21",
                    _ => expression.calculate().ToString() ?? "null",
                };
                await msg.Reply(reply);
            }, [ new Param("math", "the math to do", "NaN"), ]),

            new("count", "set the counting channel", "gimmick", async (msg, p) => {
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

            new("countSet", "set the current count", "gimmick", async (msg, p) => {
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
                GetRandomFileFromSSH(msg, p["fileName"], "jermaSFX");
            }, [ new Param("fileName", "what the file will be renamed to", "") ]),

            new("flower", "Hiiii! " + Emojis.TalkingFlower, "silly", async (msg, p) => {
                // GetRandomFileFromSSH(msg, p["fileName"], "flowerSFX");
            }, [ new Param("fileName", "what the file will be renamed to", "") ]),

            new("challenge, game", "challenge another user to a game", "games", async (msg, p) => {
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
                (string game, IUser? victim) = (p["game"] ?? "", (p["victim"] ?? msg.ReferencedMessage?.Author));
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
                int cFourChance    = Fuzz.WeightedRatio(game, "connect4");
                int checkersChance = Fuzz.WeightedRatio(game, "checkers");
                int unoChance      = Fuzz.WeightedRatio(game, "uno");
                if ((new int[] {cFourChance, checkersChance, unoChance}).All(c => c <= 60)) { // if none of them are valid
                    await msg.Reply("that's not a game!");
                    return;
                }
                var challengeMsg = await msg.Reply($"{(victim != null ? UserPingFromID(victim.Id) : "anybody here")}, do you accept {UserPingFromID(msg.Author.Id)}'s challenge?");
                Emoji? checkmark = Emoji.Parse("✅"); Emoji? crossmark = Emoji.Parse("❌");
                _ = challengeMsg.AddReactionAsync(checkmark);
                if (!anyone) _ = challengeMsg.AddReactionAsync(crossmark);

                async Task<bool> ReactionCheck(IUserMessage message, SocketReaction reaction)
                {
                    IUser reactUser = reaction.User.Value;
                    ulong ruId = reactUser.Id;
                    if (message.Id != challengeMsg.Id || ruId == CARETAKER_ID) {
                        return false;
                    }

                    // if it's not the victim nor the caretaker (and it's not for anyone) remove the reaction
                    if (ruId != victim?.Id && !anyone) {
                        _ = challengeMsg.RemoveReactionAsync(reaction.Emote, reactUser);
                        return false;
                    }

                    if (reaction.Emote.Name == "✅") {
                        if (ruId == victim?.Id || anyone) {
                            if (cFourChance > 60) {
                                s.CurrentGame = new ConnectFour(challengeMsg.Channel.Id, msg.Author.Id, ruId);
                            } else if (checkersChance > 60) {
                                // s.CurrentGame = new Checkers(challengeMsg.Channel.Id, msg.Author.Id, ruId);
                                _ = msg.Reply("not implemented yet! soon tho");
                            } else if (unoChance > 60) {
                                _ = msg.Reply("not implemented yet! soon tho");
                            } else {
                                _ = msg.Reply("okay this error should literally never happen. " + UserPingFromID(ASTRL_ID), true);
                            }
                            _ = challengeMsg.Reply($"{UserPingFromID(msg.Author.Id)} and {UserPingFromID(ruId)}, begin!");
                            return true;
                        }
                    } else if (reaction.Emote.Name == "❌") {
                        if ((ruId == victim?.Id || ruId == msg.Author.Id) && !anyone) {
                            _ = challengeMsg.Reply("awww... they denied. 😢");
                            return true;
                        }
                    }

                    return false;
                }

                using var rs = new ReactionSubscribe(ReactionCheck, challengeMsg);
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (true)
                    {
                        if (rs.Destroyed) return; // end loop if already destroyed

                        if (stopwatch.Elapsed.TotalSeconds >= 60 || s.CurrentGame != null) {
                            string newMsg = s.CurrentGame != null ? "sorryyy another game started in the middle of you asking :/" :  "took too long! oops.";
                            _ = challengeMsg.OverwriteMessage(newMsg);
                            _ = challengeMsg.RemoveAllReactionsAsync();
                            break;
                        }
                        await Task.Delay(1000);
                    }
                    stopwatch.Stop();
                rs.Dispose();
            }, [
                new Param("victim", "the username/display name of the person you'd like to challenge", "anyone", Param.ParamType.User),
                new Param("game", "which game would you like to challenge with?", "connect4"),
            ]),

            new("leaderboard, loserboard", "see the top ranking individuals on this bot", "games", async (msg, p) => {
                (int amount, bool loserboard) = (p["amount"], p.Command == "loserboard");
                var topUsers = MainHook.instance.UserData.OrderByDescending((x) => !loserboard ? x.Value.Wins.Count : x.Value.Losses.Count).Take(amount);
                StringBuilder desc = new();
                int i = 0;
                foreach ((ulong id, var u) in topUsers)
                {
                    var count = !loserboard ? u.Wins.Count : u.Losses.Count;
                    if (count == 0) continue;
                    desc.Append(i + 1);
                    desc.Append(". ");
                    desc.Append(UserPingFromID(id));
                    desc.Append(" - ");
                    desc.AppendLine(count.ToString());
                    i++;
                }
                EmbedBuilder leaderboard = new() {
                    Title = !loserboard ? "Leaderboard" : "Loserboard",
                    Description = desc.ToString(),
                };
                await msg.ReplyAsync(embed: leaderboard.Build());
            }, [
                // new Param("loserboard", "get the people who have LOST the most instead", false),
                new Param("amount", "the amount of people to grab for the leaderboard", 10),
            ]),

            new("bet, gamble", "see the top ranking individuals on this bot", "games", async (msg, p) => {
                int amount = p["amount"];
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist s)) return;

                var u = MainHook.instance.GetUserData(msg);
                if (u.TryStartEconomy(msg)) return;

                string reply = "";
                if (s.CurrentGame == null) {
                    reply = "silly billy! there's no game in play right now.";
                } else if (amount <= 0) {
                    reply = "that's... not right. gamble more, pls";
                } else if (amount > u.Balance) {
                    reply = "damnnn you're poor. get ur money up i think";
                } else {
                    s.CurrentGame.AddBet(msg.Author.Id, amount);
                }
                if (!string.IsNullOrEmpty(reply)) {
                    _ = msg.ReactAsync("❌");
                    _ = msg.Reply(reply);
                } else {
                    _ = msg.ReactAsync("✅");
                }
            }, [
                new Param("amount", "the amount of jell to gamble", 1),
            ]),

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
                            _ = msg.ReactAsync("❌");
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

            new("test", "for testing :)", "caretaker", async (msg, p) => {
                var builder = new ComponentBuilder()
                    .WithButton("label", "show-cards");

                await msg.ReplyAsync("button", components: builder.Build());
            }),

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            new("cmd", "run more internal commands, will probably just be limited to astrl", "caretaker"),
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            new("help", "list all cmd commands", "commands", async (msg, p) => {
                // string reply = ListCommands(msg, p["command"] ?? "", p["listParams"], true);
                // await msg.Reply(reply, false);
                await msg.Reply("implementing this later", false);
            }, [
                new Param("command", "the command to get help for (if empty, just lists all)", ""),
                new Param("listParams", "list parameters?", false)
            ]),

            new("echo", "huh", "silly", async (msg, p) => {
                string? reply = p["reply"];
                if (string.IsNullOrEmpty(reply)) return;

                if (!reply.Contains("@everyone") && !reply.Contains("@here") && reply != "") {
                    await Task.Delay(int.Abs(p["wait"]));
                    await msg.Reply((string?)p["reply"] ?? "", false);
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

            new("game", "controls the current server's game state", "games", async (msg, p) => {
                string? thing = p["thing"];
                if (string.IsNullOrEmpty(thing)) return;
                if (!MainHook.instance.TryGetGuildData(msg, out GuildPersist s) || s == null) return;
                switch (thing)
                {
                    case "cancel": {
                        s.CurrentGame = null;
                        _ = msg.ReactAsync("✅");
                    } break;
                    case "refresh": {
                        switch (s.CurrentGame)
                        {
                            case ConnectFour c4: {
                                _ = msg.Reply(c4.DisplayBoard());
                            } break;
                            default: break;
                        }
                    } break;
                    // case "cancel": {

                    // } break;
                    default: {
                        _ = msg.Reply("Bitch.");
                    } break;
                }
            }, [ new Param("thing", "the thing to control", "cancel") ]),

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
                (string? channel, SocketGuild? guild) = (p["channel"], p["guild"] ?? msg.GetGuild());
                if (guild == null) {
                    _ = msg.Reply("mmm... nope.");
                    return;
                }
                IMessageChannel? talkingChannel = !string.IsNullOrEmpty(channel) ? guild.ParseChannel(channel) : msg.Channel;
                if (talkingChannel == null) {
                    _ = msg.Reply("mmm... nope!!");
                    return;
                }
                MainHook.instance.TalkingChannel = (ITextChannel)talkingChannel;
                _ = msg.ReactAsync("✅");
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

        public static (Command?, string) ParseCommand(string command, string parameters, ulong userId)
        {
            var whichComms = Commands;
            if (command == "cmd" && MainHook.TrustedUsers.Contains(userId)) {
                whichComms = CmdCommands;
                (command, parameters) = parameters.SplitByFirstChar(' ');
            }

            return (whichComms[command.ToLower()], parameters);
        }
        // might wanna make it return what the command.func returns, though i don't know how helpful that would be
        public async static Task<bool> DoCommand(IUserMessage msg, Command com, string parameters, string commandName)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Dictionary<string, dynamic?> paramDict = [];
            Dictionary<string, string?> unparamDict = [];
            string[] unparams = [];
            if (com.Params != null && (com.Params.Length > 0 || com.Inf != null))
            {
                bool isBetweenQuotes = false;
                int currentParamIndex = 0;
                Param? currentParam = null;
                bool stopLoop = false;

                List<char> currentString = new(parameters.Length);
                Dictionary<char, Action<int>> charActions = new() {
                    { '"', i => isBetweenQuotes = !isBetweenQuotes },
                    { ' ', i => {
                        if (isBetweenQuotes) {
                            currentString.Add(parameters[i]);
                            return;
                        }
                        if ((parameters.IsIndexValid(i + 1) && parameters[i + 1] == ':') || (parameters.IsIndexValid(i - 1) && parameters[i - 1] == ':')) return;

                        if (currentParam == null) {
                            currentParam ??= com.Params[currentParamIndex];
                            currentParamIndex++;
                        }
                        var paramStr = string.Concat(currentString);
                        unparamDict.TryAdd(currentParam.Name, paramStr);
                        dynamic? paramVal = currentParam.ToType(paramStr, msg.GetGuild());
                        paramDict.TryAdd(currentParam.Name, paramVal);
                        currentParam = null;
                        currentString.Clear();
                    }},
                    { ':', i => {
                        int paramIndex = Array.FindIndex(com.Params, p => p.Name == string.Concat(currentString));
                        currentString.Clear();
                        if (paramIndex > -1) {
                            currentParam = com.Params[paramIndex];
                        } else {
                            var s = MainHook.instance.GetGuildData(msg);
                            _ = msg.Reply($"incorrect param name! use \"{s?.Prefix ?? DEFAULT_PREFIX}help {com.Name}\" to get params for {com.Name}.");
                            stopLoop = true;
                        }
                    }},
                };
                for (int i = 0; i < parameters.Length; i++)
                {
                    // if you can get the action from the character, and there's not a \ before the character
                    if (charActions.TryGetValue(parameters[i], out var action) && !(parameters.IsIndexValid(i - 1) && parameters[i - 1] == '\\')) {
                        action.Invoke(i);
                    } else {
                        currentString.Add(parameters[i]);
                    }
                    if (stopLoop) return false;
                }
                // might be a better way to do this? works wonders rn!
                if (currentString.Count > 0) {
                    isBetweenQuotes = false;
                    charActions[' '].Invoke(-69); // arbitrary. no other reason i chose it 😊😊😊
                }

                foreach (var param in com.Params) {
                    if (!paramDict.ContainsKey(param.Name)) {
                        dynamic? paramVal = 
                            param.Preset is string preset && param.Type != Param.ParamType.String ? 
                                param.ToType(preset, msg.GetGuild()) : 
                                param.Preset;

                        paramDict.Add(param.Name, paramVal);
                    }
                }
            }

            stopwatch.Stop();
            LogDebug($"took {stopwatch.Elapsed.TotalMilliseconds} ms to parse parameters", true);
            
            await com.Func.Invoke(msg, new Command.ParsedParams(commandName, paramDict, unparamDict, unparams));
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

        public static (string, string)[]? ListCommands(IUserMessage msg, string singleCom, bool listParams, bool cmd = false, bool showHidden = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var commandDict = cmd ? CmdCommands : Commands;
            if (singleCom != "" && !commandDict.ContainsKey(singleCom)) {
                return null;
            }
            var s = MainHook.instance.GetGuildData(msg);
            string prefix = s?.Prefix ?? DEFAULT_PREFIX;
            StringBuilder tempCommandList = new();

            // StringBuilder response = new();
            // string[] commandKeys = singleCom != "" ? [ singleCom ] : [ ..commandDict.Keys ];
            Dictionary<string, List<Command>> comsSortedByGenre = [];
            foreach (Command command in commandDict.Values)
            {
                string comGenre = command.Genre;
                if (!comsSortedByGenre.TryGetValue(comGenre, out List<Command>? commands)) {
                    commands = ([]);
                    comsSortedByGenre.Add(comGenre, commands);
                }

                commands.Add(command);
            }
            List<(string, string)> listedCommands = [];
            foreach ((string key, List<Command> coms) in comsSortedByGenre)
            {
                tempCommandList.Clear();
                for (int i = 0; i < coms.Count; i++)
                {
                    var com = coms[i];
                    if ((coms.IsIndexValid(i - 1) && coms[i - 1].Name == com.Name) || 
                        (com.Genre == "hidden" && !showHidden))
                    {
                        continue;
                    }

                    tempCommandList.Append(prefix);
                    tempCommandList.Append(com.Name);
                    if (com.Params != null && !listParams) {
                        IEnumerable<string> paramNames = com.Params.Select(x => x.Name);
                        tempCommandList.Append(" (");
                        tempCommandList.Append(string.Join(", ", paramNames));
                        if (com.Inf != null) tempCommandList.Append(", params");
                        tempCommandList.Append(')');
                    }

                    tempCommandList.Append(" : ");
                    tempCommandList.AppendLine(com.Desc);

                    if (com.Params != null && listParams) {
                        foreach (var param in com.Params) {
                            tempCommandList.Append("\t-");
                            tempCommandList.Append(param.Name);
                            tempCommandList.Append(" : ");
                            tempCommandList.AppendLine(param.Desc);
                        }
                    }
                }
                listedCommands.Add((key, tempCommandList.ToString()));
            }
            sw.Stop();
            LogDebug($"ListCommands() took {sw.ElapsedMilliseconds} milliseconds to complete");
            return listedCommands.Count > 0 ? [.. listedCommands] : null;
        }

        private static async void GetRandomFileFromSSH(IUserMessage msg, string? fileName, string sfxFolder)
        {
            var connectionInfo = new ConnectionInfo(
                "150.230.169.222", "opc",
                new PrivateKeyAuthenticationMethod("opc", new PrivateKeyFile(Path.Combine(PrivatesPath, "ssh.key")))
            );
            // using (var client = new ScpClient(connectionInfo))
            using var client = new SftpClient(connectionInfo);
                await msg.ReactAsync("✅");
                try {
                    client.Connect();
                    string remoteDirectory = $"/home/opc/mediaHosting/{sfxFolder}/";
                    var files = client.ListDirectory(remoteDirectory);
                    if (files == null) {
                        await msg.ReactAsync("❌");
                        return;
                    }
                    var randomFile = files.GetRandom()!;
                    string n = "./temp/" + (!string.IsNullOrEmpty(fileName) ? fileName + ".mp3" : randomFile.Name);
                    Stream newFile = File.Exists(n) ? File.OpenRead(n) : File.Create(n);
                    client.DownloadFile(randomFile.FullName, newFile);
                    newFile.Dispose();
                    await msg.Channel.SendFileAsync(n);
                    File.Delete(n);
                } catch (Exception err) {
                    await msg.ReactAsync("❌");
                    LogError(err);
                    throw;
                }
            client.Dispose();
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
