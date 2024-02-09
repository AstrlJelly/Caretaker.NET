using System;
using CaretakerNET.Core;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET.Commands
{
    public class Command
    {
        public readonly string name;
        public readonly string desc;
        public readonly string genre;
        public delegate Task RunAsync(IUserMessage msg, Dictionary<string, dynamic> p);
        public delegate Task RunInGuildAsync(IUserMessage msg, Dictionary<string, dynamic> p, Dictionary<string, GuildPersist> s);
        // public delegate void Run(IUserMessage msg, Dictionary<string, dynamic> p);
        public readonly RunAsync func;
        public readonly Param[] parameters;
        public readonly Param? inf;
        public string[][] limitedTo;
        public int timeout;
        public int currentTimeout;
        public Command(string name, string desc, string genre, RunAsync func, List<Param>? parameters = null, string[][]? limitedTo = null, int timeout = 500)
        {
            this.name = name;
            this.desc = desc;
            this.genre = genre;
            this.func = func;
            this.limitedTo = new string[3][];
            for (int i = 0; i < 3; i++) {
                // if limitedTo isn't null, and limitedTo[i] is usable, assign limitedTo[i]. else just []
                this.limitedTo[i] = limitedTo != null && limitedTo.IsIndexValid(i) && limitedTo[i] != null ? limitedTo[i] : [];
            }
            this.timeout = timeout;
            currentTimeout = 0;

            this.inf = null;
            if (parameters != null) {
                if (parameters.TryFindIndex(x => x.name == "params", out int infIndex)) {
                    this.inf = parameters[infIndex];
                    parameters.RemoveAt(infIndex);
                }
                this.parameters = [..parameters];
            } else {
                this.parameters = [];
            }
        }
    }

    public class Param
    {
        public dynamic? ToType(string str, SocketGuild? guild) {
            return type switch {
                "int32"       => int.Parse(str),
                "uint32"      => uint.Parse(str),
                "double"      => double.Parse(str),
                "boolean"     => str == "true",
                "user"        => MainHook.instance.Client.ParseUser(str, guild),
                "channel"     => guild?.ParseChannel(str),
                "guild"       => MainHook.instance.Client.ParseGuild(str),
                "string" or _ => str,
            };
        }

        public string name;
        public string desc;
        public dynamic preset;
        public string type;
        public Param(string name, string desc, dynamic preset, string? type = null)
        {
            this.name = name;
            this.desc = desc;
            this.preset = preset;
            
            type ??= preset.GetType().Name;
            this.type = type.ToLower();
        }
    }
}
