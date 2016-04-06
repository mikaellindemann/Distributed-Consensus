namespace Client
{
    class Settings
    {
        public string ServerAddress { get; set; }
        public string Username { get; set; }

        public static Settings LoadSettings()
        {
            var settings = new Settings
            {
                ServerAddress = Properties.Settings.Default.ServerAddress,
                Username = Properties.Settings.Default.Username
            };

            return settings;
        }

        public void SaveSettings()
        {
            Properties.Settings.Default.ServerAddress = ServerAddress;
            Properties.Settings.Default.Username = Username;

            Properties.Settings.Default.Save();
        }
    }
}
