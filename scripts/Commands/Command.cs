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
        // public delegate Task RunInGuildAsync(IUserMessage msg, Dictionary<string, dynamic> p, Dictionary<string, GuildPersist> s);
        public readonly RunAsync func;
        public readonly Param[] parameters;
        public readonly Param? inf;
        public HashSet<ChannelPermission> limitedToPerms;
        public HashSet<ulong> limitedToIds;
        public int timeout;
        public int currentTimeout;
        public Command(string name, string desc, string genre, RunAsync func, List<Param>? parameters = null, HashSet<ChannelPermission>? limitedToPerms = null, HashSet<ulong>? limitedToIds = null, int timeout = 500)
        {
            this.name = name;
            this.desc = desc;
            this.genre = genre;
            this.func = func;
            this.limitedToPerms = limitedToPerms ?? [];
            this.limitedToIds = limitedToIds ?? [];
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

        public bool HasPerms(IUserMessage msg) {
            if (msg.GetGuild() == null) return true;
            return HasPerms((SocketGuildUser)msg.Author, (IGuildChannel)msg.Channel);
        }
        public bool HasPerms(SocketGuildUser user, IGuildChannel chnl)
        {
            if (limitedToIds.Contains(user.Id)) return true;
            var userPerms = user.GetPermissions(chnl);
            foreach (var perm in limitedToPerms) {
                if (userPerms.Has(perm)) return true;
            }
            return limitedToPerms.Count <= 0 && limitedToIds.Count <= 0;
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
