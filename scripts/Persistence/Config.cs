using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CaretakerNET.Persistence
{
    public static class ConfigHandler
    {
        private const string CONFIG_PATH = "./config.yaml";
        public static async void Save(Config config)
        {
            var serializer = new SerializerBuilder()
                // .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(config);
            await File.WriteAllTextAsync(CONFIG_PATH, yaml); // creates new file or overwrites existing one
            Console.WriteLine(yaml);
        }

        public static async Task<Config> Load()
        {
            if (!File.Exists(CONFIG_PATH)) {
                return Fail();
            } else {
                try {
                    string yaml = await File.ReadAllTextAsync(CONFIG_PATH);
                    Log(yaml);
                    var deserializer = new Deserializer();
                    return deserializer.Deserialize<Config>(yaml) ?? Fail();
                } catch (Exception err) {
                    return Fail(err);
                }
            }

            static Config Fail(Exception? err = null)
            {
                string log = $"couldn't load from {CONFIG_PATH}, returning default";
                if (err != null) log += "\n" + err;
                LogWarning(log);
                var config = new Config();
                Save(config);
                return config;
            }
        }
    }

    [YamlSerializable]
    public class Config
    {
        public bool DebugMode = false;
        public bool TestingMode = false;
        public string PrivatesPath = "";
        public string Token = "";
        // [YamlMember(Alias = "Token")] 
        // private string token = "";
        // [YamlIgnore] public string Token { get {
        //     string tempToken = token;
        //     token = "";
        //     return tempToken;
        // }}
    }
}