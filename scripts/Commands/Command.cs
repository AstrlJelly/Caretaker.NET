using System;
using CaretakerNET.Core;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET.Commands
{
    public class Command
    {
        /// <summary>
        /// yeah
        /// </summary>
        /// <param name="params">parsed params in a dictionary.</param>
        /// <param name="unparams">unparsed params; the strings that get parsed to the params dictionary.</param>
        /// <param name="unparamsParams">the "params" input, which is always an array.</param>
        public class ParsedParams(Dictionary<string, dynamic> @params, Dictionary<string, string?> unparams, string[] unparamsParams)
        {
            public dynamic this[string key] => Params[key];

            public readonly Dictionary<string, dynamic> Params = @params;
            public readonly Dictionary<string, string?> Unparams = unparams;
            public readonly string[] UnparamsParams = unparamsParams;
        }

        public readonly string Name;
        public readonly string Desc;
        public readonly string Genre;
        public delegate Task RunAsync(IUserMessage msg, ParsedParams p);
        public readonly RunAsync Func;
        // public readonly Dictionary<string, Param> Parameters;
        public readonly Param[] Params;
        public readonly Param? Inf = null;
        public HashSet<ChannelPermission> LimitedToPerms;
        public HashSet<ulong> LimitedToIds;
        public int Timeout;
        public int CurrentTimeout = 0;

        public Command(string name, string desc, string genre, RunAsync? func = null, List<Param>? parameters = null, HashSet<ChannelPermission>? limitedToPerms = null, HashSet<ulong>? limitedToIds = null, int timeout = 500)
        {
            this.Name = name;
            this.Desc = desc;
            this.Genre = genre;
            this.Func = func ?? ((_, _) => Task.CompletedTask);
            this.LimitedToPerms = limitedToPerms ?? [];
            this.LimitedToIds = limitedToIds ?? [];
            this.Timeout = timeout;
            // CurrentTimeout = 0;

            // this.Inf = null;
            if (parameters != null) {
                if (parameters.TryFindIndex(x => x.Name == "params", out int infIndex)) {
                    this.Inf = parameters[infIndex];
                    parameters.RemoveAt(infIndex);
                }
                this.Params = [..parameters];
                // this.Parameters = parameters.ToDictionary(p => p.Name);
            } else {
                this.Params = [];
            }
        }

        public bool HasPerms(IUserMessage msg) {
            if (msg.GetGuild() == null) return true;
            return HasPerms((SocketGuildUser)msg.Author, (IGuildChannel)msg.Channel);
        }
        public bool HasPerms(SocketGuildUser user, IGuildChannel chnl)
        {
            if (LimitedToPerms.Count <= 0 && LimitedToIds.Count <= 0) return true; // most common case
            if (LimitedToIds.Contains(user.Id)) return true; // id is easiest to check first
            var userPerms = user.GetPermissions(chnl);
            foreach (var perm in LimitedToPerms) {
                if (userPerms.Has(perm)) return true;
            }
            return false;
        }
    }


    public class Param(string name, string desc, dynamic preset, Param.ParamType? type = null)
    {
        public enum ParamType
        {
            String,
            Boolean,
            Double,
            Integer,
            UInteger,
            Guild,
            Channel,
            User,
        }
        public dynamic? ToType(string str, SocketGuild? guild) {
            return Type switch {
                ParamType.Integer     => int.Parse(str),
                ParamType.UInteger    => uint.Parse(str),
                ParamType.Double      => double.Parse(str),
                ParamType.Boolean     => str == "true",
                ParamType.User        => MainHook.instance.Client.ParseUser(str, guild),
                ParamType.Channel     => guild?.ParseChannel(str),
                ParamType.Guild       => MainHook.instance.Client.ParseGuild(str),
                ParamType.String or _ => str, // gotta always have that "or _" :)
            };
        }

        public string Name = name;
        public string Desc = desc;
        public dynamic Preset = preset;
        public ParamType Type = type ?? preset.GetType().ToString() switch
        {
            "Boolean" => ParamType.Boolean,
            "Double" => ParamType.Double,
            "Int32" => ParamType.Integer,
            "UInt32" => ParamType.UInteger,
            "String" or _ => ParamType.String,
        };
    }
}


/*
```cs
if (PlayerInput.GetIsAction(InputAction_BasicPress) && isCharging && PlayerInput.CurrentControlStyle == InputController.ControlStyles.Touch)
{
  hand.DoScaledAnimationAsync("Charge", 0.5f);
}
```
*/