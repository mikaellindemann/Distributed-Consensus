using System;
using System.Collections.Generic;
using Client.Connections;
using Client.Exceptions;
using Client.Views;

namespace Client.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        public Action CloseAction { get; set; }
        private Dictionary<string, ICollection<string>> RolesForWorkflows { get; set; }

        private bool _loginStarted;
        private readonly Uri _serverAddress;
        private readonly IServerConnection _serverConnection;

        public LoginViewModel()
        {
            var settings = Settings.LoadSettings();
            _serverAddress = new Uri(settings.ServerAddress);
            _serverConnection = new ServerConnection(_serverAddress);

            Username = settings.Username;
            Status = "";
            Password = "";
            CloseAction += () => new MainWindow(RolesForWorkflows).Show();
        }

        public LoginViewModel(IServerConnection serverConnection, Uri serverAddress, Action mainWindowAction)
        {
            _serverConnection = serverConnection;
            CloseAction += mainWindowAction;
            _serverAddress = serverAddress;
        }

        #region Databindings

        private string _username;
        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                NotifyPropertyChanged();
            }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged();
            }
        }
        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                NotifyPropertyChanged();
            }
        }
        #endregion

        #region Actions

        public async void Login()
        {
            if (_loginStarted) return;
            _loginStarted = true;
            Status = "Attempting login...";

            try
            {
                RolesForWorkflows = (await _serverConnection.Login(Username, Password)).RolesOnWorkflows;
                Status = "Login successful";

                // Save settings
                new Settings
                {
                    ServerAddress = _serverAddress.AbsoluteUri,
                    Username = Username
                }.SaveSettings();

                CloseAction();
            }
            catch (LoginFailedException)
            {
                _loginStarted = false;
                Status = "The provided username and password does not correspond to a user in Flow";
            }
            catch (HostNotFoundException)
            {
                _loginStarted = false;
                Status = "The server is not available, or the settings file is pointing to an invalid address";
            }
            catch (Exception)
            {
                _loginStarted = false;
                Status = "An unexpected error occured. Try again in a while.";
            }
        }

        #endregion


    }
}
