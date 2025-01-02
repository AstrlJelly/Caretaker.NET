using System;
using System.Diagnostics;
using System.Linq;
using System.Data;
using System.Text;

using Discord;
using Discord.WebSocket;

using CaretakerNET.Persistence;
using CaretakerNET.ExternalEmojis;
using CaretakerNET.Games;

using org.mariuszgromada.math.mxparser;
using Renci.SshNet;
using FuzzySharp;
using ComputeSharp;
using Z.Expressions.CodeCompiler.CSharp;
using Z.Expressions;
using ChatGPT.Net;

namespace CaretakerNET.Commands
{
    public static class CommandHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static readonly Command[] commands = [
            new("prefix", "set the prefix for the current guild", "commands", async (msg, p) => {
                if (!MainHook.Instance.PersistenceHandler.TryGetGuildData(msg, out GuildPersist s) || s == null) return;
                s.Prefix = string.IsNullOrEmpty(p["prefix"]) ? ">" : p["prefix"]!;
                _ = msg.React("✅");
            }, [
                new Param("prefix", $"the prefix to set to. if empty, resets to \"{DEFAULT_PREFIX}\"", "")
            ], [ ChannelPermission.ManageChannels ]),

            new("help, listCommands", "list all normal commands", "commands", async (msg, p) => {
                await HelpReply(msg, p["command"] ?? "", p["listParams"], false);
            }, [ 
                new Param("command", "the command to get help for (if empty, just lists all)", ""),
                new Param("listParams", "list parameters?", false)
            ]),

            new("true", Emojis.True, "silly", async (msg, p) => {
                var reactMsg = msg.ReferencedMessage ?? (await msg.Channel.GetMessagesAsync(2).LastAsync()).Last();
                _ = reactMsg.DeleteAsync();
                foreach (string trueId in Emojis.Trues) {
                    // await reactMsg.React($"<:true:{trueId}>");
                    _ = reactMsg.React($"<:true:{trueId}>");
                    await Task.Delay(800);
                }
            }, timeout: 10000),

            new("echo", "list all normal commands", "silly", async (msg, p) => {
                (string? reply, int wait) = (p["reply"], p["wait"]);
                if (string.IsNullOrEmpty(reply)) return;
                if (wait < 120000 && reply != "" && !reply.Contains("@everyone") && !reply.Contains("@here")) {
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
                        _ = msg.React("✅");
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

            new("say, ai", "talk to caretaker, except this time it's not just astrl", "hidden", async (msg, p) => {
                string? ask = (string?)p["ask"];
                if (string.IsNullOrEmpty(ask)) {
                    await msg.Reply("how am i gonna respond to an empty message?");
                    return;
                }
                _ = msg.React("👍");
                string response = await MainHook.Instance.CaretakerChat.Ask(ask);
                await msg.Reply(response);
            }, [ new Param("ask", "the message you'd like to say to caretaker", "") ]),

            new("optout, optin", "set the counting channel", "gimmick", async (msg, p) => {
                string f = p["feature"] ?? "";
                bool empty = string.IsNullOrEmpty(f);
                
                if (!empty && Enum.TryParse(f, out UserPersist.Features feature)) {
                    if (!PersistenceHandler.Instance.TryGetUserData(msg.Author.Id, out UserPersist u)) return;

                    bool optOut = p.Command == "optout";
                    if (optOut && u.OptedOutFeatures.Contains(feature)) {
                        u.OptedOutFeatures.Remove(feature);
                    } else {
                        u.OptedOutFeatures.Add(feature);
                    }
                    _ = msg.Reply(optOut ? "opted out!" : "opted in!");
                } else {
                    var replyBuilder = new StringBuilder();
                    foreach (var element in Enum.GetNames<UserPersist.Features>())
                    {
                        replyBuilder.AppendLine($"* \"{element}\"");
                    }
                    _ = msg.Reply((!empty ? "that's not a feature!\n" : "") + "here are the features you can opt out of :\n" + replyBuilder.ToString());
                }
            }, [
                new Param("feature", "the feature to opt out of", ""),
            ]),

            new("count", "set the counting channel", "gimmick", async (msg, p) => {
                (ITextChannel? channel, bool reset) = (p["channel"], p["reset"]);
                if (!PersistenceHandler.Instance.TryGetGuildData(msg, out GuildPersist s) || s == null) return;
                ITextChannel? countChannel = channel ?? (ITextChannel?)msg.Channel; // TryGetGuildData makes sure this is done in a guild, so this is 99.9% not null?
                if (reset) {
                    s.Count.Reset(true);
                }
                s.Count.Channel = countChannel; 
                await msg.React("✅");
            }, [
                new Param("channel", "the channel to count in", "", Param.ParamType.Channel),
                new Param("reset", "reset everything?", true),
            ], [ ChannelPermission.ManageChannels ]),

            new("countSet", "set the current count", "gimmick", async (msg, p) => {
                int newCount = p["newCount"];
                if (!PersistenceHandler.Instance.TryGetGuildData(msg, out GuildPersist s)) return;
                if (s.Count?.Channel != null) {
                    _ = msg.React("✅");
                    s.Count.Current = newCount;
                } else {
                    _ = msg.React("❌");
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
                if (!PersistenceHandler.Instance.TryGetGuildData(msg, out GuildPersist? s) || s == null) return;
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
                } else if ((victim?.IsBot ?? false) && victim.Id != CARETAKER_ID) {
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
                    if (message.Id != challengeMsg.Id || (ruId == CARETAKER_ID && victim?.Id != CARETAKER_ID)) {
                        return false;
                    }

                    // if it's not the victim nor the caretaker (and it's not for anyone) remove the reaction
                    if (ruId != victim?.Id && !anyone) {
                        _ = challengeMsg.RemoveReactionAsync(reaction.Emote, reactUser);
                        return false;
                    }

                    if (reaction.Emote.Name == "✅") {
                        if (ruId == victim?.Id || anyone) {
                            BoardGame.Player caretakerPlayer = victim?.Id == CARETAKER_ID ? BoardGame.Player.Two : BoardGame.Player.None;
                            if (cFourChance > 60) {
                                s.CurrentGame = new ConnectFour(challengeMsg.Channel.Id, caretakerPlayer, msg.Author.Id, ruId);
                            } else if (checkersChance > 60) {
                                s.CurrentGame = new Checkers(challengeMsg.Channel.Id, msg.Author.Id, ruId);
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
                var userData = PersistenceHandler.Instance.UserData;
                var topUsers = userData.OrderByDescending((x) => !loserboard ? x.Value.Wins.Count : x.Value.Losses.Count).Take(amount);
                StringBuilder desc = new();
                int i = 0;
                foreach ((ulong id, var u) in topUsers)
                {
                    var count = !loserboard ? u.Wins.Count : u.Losses.Count;
                    if (count == 0) continue;
                    desc.Append(i + 1);
                    desc.Append(". ");
                    desc.Append(UserPingFromID(id));
                    desc.Append("                                                                                                                                                                                             (");
                    desc.Append(u.Username);
                    desc.Append(") - ");
                    desc.AppendLine(count.ToString());
                    i++;
                }
                EmbedBuilder leaderboard = new() {
                    Title = !loserboard ? "Leaderboard" : "Loserboard",
                    Description = desc.ToString(),
                };
                await msg.ReplyAsync(embed: leaderboard.Build());
            }, [
                new Param("amount", "the amount of people to grab for the leaderboard", 10),
            ]),

            new("bet, gamble", "see the top ranking individuals on this bot", "games, economy", async (msg, p) => { // MAKE SURE MONEY IS SOMEHOW KEPT UNTIL THE GAME IS DESTROYED IN ANY WAY
                (long amount, IUser? user) = (p["amount"], p["user"]);
                if (!PersistenceHandler.Instance.TryGetGuildData(msg, out GuildPersist s)) return;

                var u = PersistenceHandler.Instance.GetUserData(msg);
                if (!u.HasStartedEconomy) {
                    u.StartEconomy(msg);
                    return;
                }

                string reply = "";
                // HOW DO I NOT DO THIS. THIS IS SO MANY ELSE IFS.
                if (s.CurrentGame == null) {
                    reply = "silly billy... there's no game in play right now.";
                } else if (s.CurrentGame.Players.ContainsValue(msg.Author.Id)) {
                    reply = "HEY!! that's illegal!";
                } else if (amount <= 0) {
                    reply = "that's... not right. gamble more, pls";
                } else if (amount > u.Balance) {
                    reply = "damnnn you're poor. get ur money up i think";
                } else if (user == null) {
                    reply = "that's not a valid user!";
                } else if (!s.CurrentGame.Players.ContainsValue(user.Id)) {
                    reply = "that user isn't playing here right now.";
                } else {
                    s.CurrentGame.AddBet(msg.Author.Id, amount, user.Id);
                }
                if (!string.IsNullOrEmpty(reply)) {
                    _ = msg.React("❌");
                    _ = msg.Reply(reply);
                } else {
                    _ = msg.React("✅");
                }
            }, [
                new Param("amount", "the amount of jell to gamble", 1.00m),
                new Param("user", "the user to gamble on", "", Param.ParamType.User),
            ]),

            new("donate, donut", "give money to another user", "economy, hidden", async (msg, p) => {
                var u = PersistenceHandler.Instance.GetUserData(msg);

            }, [
                new Param("amount", "the amount of money to give", 4.20m),
                new Param("user", "the user to give money to", "astrljelly", Param.ParamType.User),
            ]),

            new("playtest", "give yourself the playtester role, or dm you an invite to the caretaker server if it's the wrong server", "caretaker", async (msg, p) => {
                var guild = msg.GetGuild(); // remember, returns null in dms
                var invite = await Client.GetGuild(CARETAKER_CENTRAL_ID).GetBestInvite();
                if (invite == null && guild?.Id != CARETAKER_CENTRAL_ID) { // handle no invite being found but only if you need it
                    _ = msg.Reply("okay so apparently there's no invite for the caretaker central server. oops");
                    return;
                }
                switch (guild?.Id)
                {
                    // dms
                    case null: {
                        _ = msg.Channel.SendMessageAsync("ermm " + invite!.Url);
                    } break;
                    // caretaker server
                    case CARETAKER_CENTRAL_ID: {
                        IGuildUser user = (IGuildUser)msg.Author;
                        var role = guild.Roles.FirstOrDefault(x => x.Name == "Playtester");
                        if (user.RoleIds.Any(x => x == role?.Id)) {
                            // _ = user.RemoveRoleAsync(role);
                            _ = msg.React("❌");
                            // _ = msg.Reply("okay... :(");
                            _ = msg.Reply("You're stuck here now.");
                        } else {
                            _ = user.AddRoleAsync(role);
                            _ = msg.React("✅");
                            _ = msg.Reply("thanks :3");
                        }
                    } break;
                    // any other server
                    default: {
                        _ = msg.React(Emojis.Smide);
                        _ = msg.Reply("Check your dms.");
                        _ = msg.Author.SendMessageAsync("here's where you can ACTUALLY use that command! :D\n" + invite!.Url);
                    } break;
                }
            }),

            new("test", "for testing :)", "caretaker", async (msg, p) => {
                string unsplitVals = p["vals"] ?? "";
                double[] vals = unsplitVals.Split(" ").Select(x => double.Parse(x)).ToArray();
                if (vals.Length < 1) {
                    await msg.Reply(new string[] { "Bitch.", "i hope you die", "DON'T!!! DO THAT!!!" }.GetRandom()!);
                }
                double mean, median;
                List<double> modes = [];
                mean = median = 0;
                Dictionary<double, int> modeChecker = [];
                for (int i = 0; i < vals.Length; i++) {
                    double val = vals[i];

                    // mean
                    mean += val;
                    // mode
                    if (modeChecker.ContainsKey(val)) {
                        modeChecker[val]++;
                    } else {
                        modeChecker[val] = 1;
                    }
                }

                // mean
                mean /= vals.Length;

                // median
                if (vals.Length % 2 == 1) {
                    median = vals[(vals.Length - 1) / 2];
                } else {
                    median = (vals[(vals.Length / 2) - 1] + vals[vals.Length / 2]) / 2;
                }

                // mode
                double firstVal = double.NaN;
                foreach ((var numThatWasRepeated, var timesRepeated) in modeChecker.OrderByDescending(x => x.Value))
                {
                    if (double.IsNaN(firstVal) || timesRepeated == firstVal) {
                        modes.Add(numThatWasRepeated);
                        firstVal = timesRepeated;
                    }
                }
                await msg.Reply($"mean : {mean}\nmedian : {median}\nmodes : {string.Join(", ", modes)}");
            }, [
                new("vals", "for testing", ""),
            ]),

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            new("cmd", "run more internal commands, will probably just be limited to astrl", "caretaker"),
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            new("help", "list all cmd commands", "commands", async (msg, p) => {
                await HelpReply(msg, p["command"] ?? "", p["listParams"], true);
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

            new("save", "save _s and _u", "internal", async (_, _) => await PersistenceHandler.Instance.Save()),
            new("load", "save _s and _u", "internal", async (_, _) => await PersistenceHandler.Instance.Load()),

            new("game", "controls the current server's game state", "games", async (msg, p) => {
                string? thing = p["thing"];
                if (string.IsNullOrEmpty(thing)) return;
                if (!PersistenceHandler.Instance.TryGetGuildData(msg, out GuildPersist s) || s == null) return;
                switch (thing)
                {
                    case "cancel": {
                        s.CurrentGame = null;
                        _ = msg.React("✅");
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
                var client = Client;
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
                (int index, string? channel, SocketGuild? guild) = (p["index"], p["channel"], p["guild"] ?? msg.GetGuild());
                if (guild == null) {
                    _ = msg.Reply("mmm... nope.");
                    return;
                }
                IMessageChannel? talkingChannel = !string.IsNullOrEmpty(channel) ? guild.ParseChannel(channel) : msg.Channel;
                if (talkingChannel == null) {
                    _ = msg.Reply("mmm... nope!!");
                    return;
                }
                if (CaretakerConsole.Instance.TrySetTalkingChannelAtIndex(index, (ITextChannel)talkingChannel)) {
                    _ = msg.React("✅");
                } else {
                    _ = msg.Reply("too high. or too low! idk and idc");
                }
            }, [
                new("index", "the index to set the channel", 0),
                new("channel", "the channel to talk in", ""),
                new("guild", "the guild to talk in", "", Param.ParamType.Guild)
            ]),

            new("eval", "run code!!", "internal", async (msg, p) => {
                string code = p["code"] ?? "null";
                
                // i am NOT satisfied with this at all but i can debug a few things which makes me happy
                _ = msg.Reply(MainHook.Instance.CompileContext.Execute(code, MainHook.Instance));
            }, [ new("code", "the code to execute", "") ]),

            new("kill", "kills the bot", "hidden", async (msg, p) => {
                int delay = p["delay"];
                if (delay > 0) await Task.Delay(delay);
                await msg.React("✅");
                string[] replies = [
                    "sounds good! shutting down",
                    "sighhhh meowkay",
                    "Um. Okay",
                    "okay.. sure..",
                    $"shutting down... {Emojis.Cride}",
                    $"i... i don't wanna go {Emojis.Cride} {Emojis.Cride} {Emojis.Cride}",
                    Emojis.Cride,
                ];
                await msg.RandomReply(replies);
                MainHook.Instance.Stop();
            }, [ new("delay", "the time to wait before the inevitable end", 0) ]),

            new("test", "testing code out", "testing", async (msg, p) => {
                SocketGuild? guild = msg.GetGuild();
                if (guild == null) {
                    _ = msg.Reply("no dms!!!");
                    return;
                }
                
                SocketRole trustedRole = guild.Roles.First(r => r.Id == 1230658842879332353);
                SocketGuildChannel chnl = guild.GetChannel(msg.Channel.Id);
                var permissionOverwrite = chnl.GetPermissionOverwrite(trustedRole);
                if (permissionOverwrite == null) {
                    _ = msg.Reply("\"permissionOverwrite\" was null.");
                    return;
                }
                foreach (var user in guild.Users) {
                    if (user.Roles.Any(r => r.Id == trustedRole.Id)) { // check if user is trusted
                        // Log(user.GlobalName);
                        await chnl.AddPermissionOverwriteAsync(user, (OverwritePermissions)permissionOverwrite);
                    }
                }
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
        public async static Task<bool> DoCommand(IUserMessage msg, Command com, string strParams, string commandName)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var guild = msg.GetGuild();
            Dictionary<string, dynamic?> paramDict = []; // main dictionary of parsed parameters
            Dictionary<string, string?> unparamDict = []; // dictionary of unparsed parameters; just the strings
            string[]? unparams = null;
            if (com.Params != null && (com.Params.Length > 0 || com.Inf != null))
            {
                bool isBetweenQuotes = false;   // 
                bool backslash = false;         // true if last character was '\'
                int currentParamIndex = 0;      // 
                Param? currentParam = null;     // null unless manually set using "paramName : value"
                List<string>? infParams = null; // inf params; gets converted to an array when setting unparams
                int stopState = 0;              // a way for the Action<int> to return in the method; 0 = nothing, 1 = break, 2 = return

                // a list of chars that get concatenated into either a parameter name or a parameter value
                List<char> currentStringAsChars = new(strParams.Length); // setting the capacity gives negligible change in performance but i think it's funny

                // dictionary of actions that have the current index as an input, accessed using different types of chars
                Dictionary<char, Action<int>> charActions = new() {
                    { '"', i => isBetweenQuotes = !isBetweenQuotes },
                    { '\\', i => backslash = true }, // used to be a check in the for loop; this is much more intuitive
                    { ' ', i => {
                        // just act like a normal character if between quotes or if the space won't signify a new param
                        if (isBetweenQuotes || (currentParamIndex >= (com.Params.Length - 1)) && (i < strParams.Length && i >= 0)) {
                            currentStringAsChars.Add(strParams[i]);
                            return;
                        }

                        // makes sure cases like ">echo thing  1000" don't break anything
                        // maybe just check if last character was a space?
                        if (currentStringAsChars.Count < 1) return;
                        // lets you do things like "paramName : value" or "paramName: value" or even "paramName :value"
                        if ((strParams.IsIndexValid(i + 1) && strParams[i + 1] == ':') || (strParams.IsIndexValid(i - 1) && strParams[i - 1] == ':')) return;

                        if (currentParamIndex >= com.Params.Length) {
                            if (com.Inf != null) {
                                infParams ??= [];
                            } else {
                                stopState = 1;
                                return;
                            }
                        }

                        var paramStr = string.Concat(currentStringAsChars);
                        if (infParams == null) {
                            // if it's not being manually set (and not adding to inf params), use currentParamIndex then add 1 to it
                            if (currentParam == null) {
                                currentParam = com.Params[currentParamIndex];
                                currentParamIndex++;
                            }
                            unparamDict.TryAdd(currentParam.Name, paramStr); // use TryAdd cuz i don't really care about duplicate keys
                            dynamic? paramVal = currentParam.ToType(paramStr, guild);
                            paramDict.TryAdd(currentParam.Name, paramVal);
                        } else {
                            infParams.Add(paramStr);
                        }
                        currentParam = null;
                        currentStringAsChars.Clear();
                    }},
                    { ':', i => {
                        string paramName = string.Concat(currentStringAsChars);
                        currentStringAsChars.Clear();
                        if (paramName == "params") {
                            infParams = [];
                        } else {
                            Param? param = Array.Find(com.Params, p => p.Name == paramName);
                            if (param != null) {
                                currentParam = param;
                            } else {
                                GuildPersist? s = PersistenceHandler.Instance.GetGuildData(msg);
                                _ = msg.Reply($"incorrect param name! use \"{s?.Prefix ?? DEFAULT_PREFIX}help {com.Name}\" to get params for {com.Name}.");
                                stopState = 2;
                            }
                        }
                    }},
                };
                for (int i = 0; i < strParams.Length; i++)
                {
                    // if you can get the action from the character, and there's not a \ before the character
                    if (charActions.TryGetValue(strParams[i], out var action) && !backslash) {
                        action.Invoke(i);
                    } else {
                        currentStringAsChars.Add(strParams[i]);
                        backslash = false;
                    }
                    // refer to stopLoop comment
                    if (stopState == 1) {
                        break;
                    } else if (stopState == 2) {
                        return false;
                    }
                }
                // there might be a better way to do this? works super well rn tho
                if (currentStringAsChars.Count > 0) {
                    isBetweenQuotes = false; // make sure the action doesn't just add a space
                    charActions[' '].Invoke(-69); // arbitrary. no other reason i chose it 😊😊😊
                }

                // check if every param is actually in paramDict
                // much better than the old method! this practically guarantees safety :)
                foreach (var param in com.Params) {
                    if (!paramDict.ContainsKey(param.Name)) {
                        // convert if it's needed; if the preset is a string but the param type is not supposed to be a string
                        // this is for users, guilds, etc.
                        dynamic? paramVal = 
                            param.Preset is string preset && param.Type != Param.ParamType.String ? 
                                param.ToType(preset, guild) : 
                                param.Preset;

                        paramDict.Add(param.Name, paramVal);
                    }
                }
                if (com.Inf != null) {
                    // set unparams to infParams as string[]
                    unparams = infParams?.ToArray() ?? [];
                    // add an array to paramDict that is infParams converted to "params"'s type
                    paramDict.Add("params", infParams?.Select(p => (object?)com.Inf.ToType(p, guild)).ToArray() ?? []);
                }
            }

            stopwatch.Stop();
            LogDebug($"took {stopwatch.Elapsed.TotalMilliseconds} ms to parse parameters", true);
            
            await com.Func.Invoke(msg, new Command.ParsedParams(commandName, paramDict, unparamDict, unparams));
            return true;
        }

        private static async Task HelpReply(IUserMessage msg, string command, bool listParams, bool cmd = false, bool showHidden = false)
        {
            (string, string)[]? commandLists = ListCommands(msg, command, listParams, cmd, showHidden);
            if (commandLists == null) {
                _ = msg.Reply(
                    $"{command} is NOT a command. try again :/" + (string.IsNullOrEmpty(command) ? "\nor something just went wrong. idk" : "")
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
                    Title = "commands : " + genre,
                    Description = com
                }).WithCurrentTimestamp().Build();
            }).ToDictionary(x => commandLists[dictIndex++].Item1);

            // there should only be components if there's more than one genre
            // for example, when you select a command manually there won't be any components
            var components = commandEmbeds.Keys.Count > 1 ? new ComponentBuilder {
                ActionRows = [
                    new ActionRowBuilder().WithSelectMenu(
                        "commands",
                        commandLists.Select((genreAndCom) => new SelectMenuOptionBuilder(genreAndCom.Item1, genreAndCom.Item1)).ToList(),
                        "command select!!"
                    )
                ]
            } : null;

            var helpMsg = await msg.ReplyAsync(components: components?.Build(), embed: commandEmbeds.Values.ElementAt(0));

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
                    // await Task.Delay(100);
                }
                stopwatch.Stop();
            rs.Dispose();
        }

        private static (string, string)[]? ListCommands(IUserMessage msg, string? singleCom, bool listParams, bool cmd = false, bool showHidden = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var commandDict = cmd ? CmdCommands : Commands;
            if (!string.IsNullOrEmpty(singleCom) && !commandDict.ContainsKey(singleCom)) {
                return null;
            }
            var s = PersistenceHandler.Instance.GetGuildData(msg);
            string prefix = s?.Prefix ?? DEFAULT_PREFIX;

            Dictionary<string, List<Command>> comsSortedByGenre = [];
            if (string.IsNullOrEmpty(singleCom)) {
                foreach (Command command in commandDict.Values) {
                    foreach (var genre in command.Genres) {
                        if (!comsSortedByGenre.TryGetValue(genre, out List<Command>? commands)) {
                            commands = ([]);
                            comsSortedByGenre.Add(genre, commands);
                        }

                        commands.Add(command);
                    }
                }
            } else {
                var command = commandDict[singleCom];
                foreach (var genre in command.Genres) {
                    comsSortedByGenre.Add(genre, [ command ]);
                }
            }

            StringBuilder comListBuilder = new();
            List<(string, string)> listedCommands = [];
            foreach ((string key, List<Command> coms) in comsSortedByGenre)
            {
                comListBuilder.Clear();
                for (int i = 0; i < coms.Count; i++)
                {
                    var com = coms[i];
                    if ((coms.IsIndexValid(i - 1) && coms[i - 1].Name == com.Name) || 
                        (com.IsGenre("hidden") && !showHidden))
                    {
                        continue;
                    }

                    comListBuilder.Append(prefix);
                    comListBuilder.Append(com.Name);
                    if (com.Params != null && !listParams) {
                        IEnumerable<string> paramNames = com.Params.Select(x => x.Name);
                        comListBuilder.Append(" (");
                        comListBuilder.Append(string.Join(", ", paramNames));
                        if (com.Inf != null) comListBuilder.Append(", params");
                        comListBuilder.Append(')');
                    }

                    comListBuilder.Append(" : ");
                    comListBuilder.AppendLine(com.Desc);

                    if (com.Params != null && listParams) {
                        foreach (var param in com.Params) {
                            comListBuilder.Append("　-"); // that's an indentation, apparently
                            comListBuilder.Append(param.Name);
                            comListBuilder.Append(" : ");
                            comListBuilder.AppendLine(param.Desc);
                        }
                    }
                }
                listedCommands.Add((key, comListBuilder.ToString()));
            }
            sw.Stop();
            LogDebug($"ListCommands() took {sw.ElapsedMilliseconds} milliseconds to complete");
            return listedCommands.Count > 0 ? [.. listedCommands] : null;
        }

        private static async void GetRandomFileFromSSH(IUserMessage msg, string? fileName, string sfxFolder)
        {
            var connectionInfo = new ConnectionInfo(
                "150.230.169.222", "opc",
                new PrivateKeyAuthenticationMethod("opc", new PrivateKeyFile(Path.Combine(MainHook.Instance.Config.PrivatesPath, "ssh.key")))
            );
            // using (var client = new ScpClient(connectionInfo))
            using var client = new SftpClient(connectionInfo);
                await msg.React("✅");
                try {
                    client.Connect();
                    string remoteDirectory = $"/home/opc/mediaHosting/{sfxFolder}/";
                    var files = client.ListDirectory(remoteDirectory);
                    if (files == null) {
                        await msg.React("❌");
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
                    await msg.React("❌");
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
