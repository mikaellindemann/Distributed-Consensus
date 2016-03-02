using System.IO;
using Newtonsoft.Json;

namespace Client
{
    class Settings
    {
        public string ServerAddress { get; set; }
        public string Username { get; set; }

        public static Settings LoadSettings()
        {
            var settings = 
                !File.Exists("settings.json") 
                ? new Settings() 
                : JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.json"));

            settings.ServerAddress = settings.ServerAddress ?? "http://flowit.azurewebsites.net/";
            settings.Username = settings.Username ?? "";
            return settings;
        }

        public void SaveSettings()
        {
            File.WriteAllText("settings.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
