using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CaretakerNET.Persistence
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    // public struct Chain
    // {
    //     public object channel;
    //     public string current;
    //     public int ChainLength;
    //     public string prevChain;
    //     public string lastChainer;
    //     public int autoChain;
    // }

    // public class Convo
    // {
    //     public object convoChannel
    //     public object replyChannel
    // }

    // public class Count
    // {
    //     public object channel { get; set; }
    //     public int current { get; set; }
    //     public int prevNumber { get; set; }
    //     public int highestNum { get; set; }
    //     public string lastCounter { get; set; }
    // }

    // public class Default
    // {
    //     public Dictionary<string, dynamic> CommandData;
    //     public Count Count;
    //     public Chain Chain;
    //     public Convo Convo;
    //     public SlowMode SlowMode;
    // }

    // public class Root
    // {
    //     public Default @default { get; set; }
    // }

    // public class SlowMode
    // {
    //     public object channel { get; set; }
    //     public int timer { get; set; }
    // }

    // public class Persist
    // {
    //     public static void UpdateProperties<T>(T from, T to)
    //     {
    //         // Get the type of the class
    //         Type t = typeof(T);
    //         // Get the public instance properties of the class
    //         PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    //         // Loop through each property
    //         foreach (PropertyInfo p in props)
    //         {
    //             // Check if the property can be read and written
    //             if (!p.CanRead || !p.CanWrite) continue;
    //             // Get the value of the property from the updated class
    //             object val = p.GetGetMethod().Invoke(from, null);
    //             // Set the value of the property to the existing object
    //             p.GetSetMethod().Invoke(to, [val]);
    //         }
    //     }

    //     public Persist() {

    //     }
    // }
}
