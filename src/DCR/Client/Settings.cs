namespace Client
{
    class Settings
    {
        public string ServerAddress { get; set; }
        public string Username { get; set; }

        public static Settings LoadSettings()
        {

#if DEBUG
            var settings = new Settings
            {
                ServerAddress = Properties.Settings.Default.ServerAddressDebug,
                Username = Properties.Settings.Default.Username
            };
#else
            var settings = new Settings
            {
                ServerAddress = Properties.Settings.Default.ServerAddress,
                Username = Properties.Settings.Default.Username
            };
#endif

            return settings;
        }

        public void SaveSettings()
        {
#if DEBUG
            Properties.Settings.Default.ServerAddressDebug = ServerAddress;
#else
            Properties.Settings.Default.ServerAddress = ServerAddress;
#endif
            Properties.Settings.Default.Username = Username;

            Properties.Settings.Default.Save();
        }
    }
}
