using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CaretakerNET.Persistence
{
    public static class ConfigHandler
    {
        private const string CONFIG_PATH = "./config.yaml";
        private const string BACKUP_PATH = "./config_backup.yaml";
        public static async void Save(Config config, bool backup)
        {
            var serializer = new SerializerBuilder().Build();

            var yaml = serializer.Serialize(config);
            await File.WriteAllTextAsync(backup ? BACKUP_PATH : CONFIG_PATH, yaml); // creates new file or overwrites existing one
            Console.WriteLine(yaml);
        }

        public static async Task<Config> Load()
        {
            if (!File.Exists(CONFIG_PATH)) {
                return Fail();
            } else {
                try {
                    string yaml = await File.ReadAllTextAsync(CONFIG_PATH);
                    var deserializer = new Deserializer();
                    var config = deserializer.Deserialize<Config>(yaml);
                    if (config == null) {
                        config = Fail();
                    } else {
                        Save(config, false);
                    }
                    return config;
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
                Save(config, true);
                return config;
            }
        }
    }

    [YamlSerializable]
    public class Config
    {
        public bool DebugMode = false;
        public string PrivatesPath = "";
        public string Token = "";
        public string CaretakerChatApiToken = "";
        public string CaretakerChatPrompt = "";
    }
}