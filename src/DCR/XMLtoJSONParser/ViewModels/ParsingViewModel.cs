using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace XMLtoJSONParser.ViewModels
{
    public class ParsingViewModel : ViewModelBase
    {
        private string _xmlFilePath;
        private string _workflowId;
        private string _workflowName;
        private string _serverUri;
        private string _eventUris;
        private bool _createWorkflow;
        private bool _createUsers;
        private bool _uploadButtonActive;
        private string _defaultPassword;

        public ParsingViewModel()
        {
            _uploadButtonActive = true;
#if DEBUG
            _serverUri = "http://localhost:13768";
            _eventUris = "http://localhost:13752";
#else
            _serverUri = "http://flowit.azurewebsites.net";
            _eventUris = "http://flowites1.azurewebsites.net,http://flowites2.azurewebsites.net";
            _defaultPassword = "Password";
            _createUsers = true;
            _createWorkflow = true;
#endif
        }

        public string XmlFilePath
        {
            get { return _xmlFilePath; }
            set
            {
                _xmlFilePath = value;
                NotifyPropertyChanged();
            }
        }

        public string WorkflowId
        {
            get { return _workflowId; }
            set
            {
                _workflowId = value;
                NotifyPropertyChanged();
            }
        }

        public string WorkflowName
        {
            get { return _workflowName; }
            set
            {
                _workflowName = value;
                NotifyPropertyChanged();
            }
        }

        public string ServerUri
        {
            get { return _serverUri; }
            set
            {
                _serverUri = value;
                NotifyPropertyChanged();
            }
        }

        public string DefaultPassword
        {
            get { return _defaultPassword; }
            set
            {
                _defaultPassword = value;
                NotifyPropertyChanged();
            }
        }

        public string EventUris
        {
            get { return _eventUris; }
            set
            {
                _eventUris = value;
                NotifyPropertyChanged();
            }
        }

        public bool CreateWorkflow
        {
            get { return _createWorkflow; }
            set
            {
                _createWorkflow = value;
                NotifyPropertyChanged();
            }
        }

        public bool CreateUsers
        {
            get { return _createUsers; }
            set
            {
                _createUsers = value;
                NotifyPropertyChanged();
            }
        }

        public bool UploadButtonActive
        {
            get { return _uploadButtonActive; }
            set
            {
                _uploadButtonActive = value;
                NotifyPropertyChanged();
            }
        }

        public void Choose()
        {
            var openFileDialog1 = new OpenFileDialog { Filter = (string) Application.Current.FindResource("XmlFileType"), FilterIndex = 1 };

            // Set filter options and filter index.
            if (!(openFileDialog1.ShowDialog() ?? false)) return;

            var file = openFileDialog1.FileName;
            XmlFilePath = file;
        }

        public async void Convert()
        {
            if (string.IsNullOrEmpty(XmlFilePath) || string.IsNullOrEmpty(EventUris) || string.IsNullOrEmpty(WorkflowId))
            {
                MessageBox.Show("You have to fill in the information first.");
            }
            else
            {
                try
                {
                    var eventUrls = GetUrls(EventUris);
                    await DcrParser.Parse(XmlFilePath, WorkflowId, eventUrls).CreateJsonFile();

                    MessageBox.Show((string)Application.Current.FindResource("ParsingToJsonOk"));
                    ClearFields();
                }
                catch (Exception)
                {
                    MessageBox.Show((string)Application.Current.FindResource("ParsingToJsonProblem"));
                }
            }
        }

        public async void Upload()
        {
            UploadButtonActive = false;
            if (!string.IsNullOrEmpty(XmlFilePath) && !string.IsNullOrEmpty(EventUris) && !string.IsNullOrEmpty(WorkflowId))
            {
                DcrParser parser;
                try
                {
                    var eventUrls = GetUrls(EventUris);

                    //var ips = TextBoxUrl.Text.Replace(" ","").Split(',');
                    parser = DcrParser.Parse(XmlFilePath, WorkflowId, eventUrls);
                }
                catch (Exception ex)
                {
                    MessageBox.Show((string)Application.Current.FindResource("ParsingToJsonOk") + Environment.NewLine + ex);
                    UploadButtonActive = true;
                    return;
                }
                var map = parser.GetMap();
                var roles = parser.GetRoles();
                var uploader = new EventUploader(WorkflowId, ServerUri, parser.IdToAddress);
                if (CreateWorkflow)
                {
                    try
                    {
                        await uploader.CreateWorkflow(WorkflowName);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(Application.Current.FindResource("UploadWorkflowFailed") +
                                        Environment.NewLine + e);
                        UploadButtonActive = true;
                        return;
                    }
                }
                try
                {
                    await uploader.Upload(map.Values.ToList());
                }
                catch (Exception e)
                {
                    MessageBox.Show(Application.Current.FindResource("UploadEventsFailed") + Environment.NewLine + e);
                    UploadButtonActive = true;
                    return;
                }
                if (CreateUsers)
                {
                    try
                    {
                        if (!await uploader.UploadUsers(roles, DefaultPassword))
                        {
                            // Some of the users wasn't created, but updated with new roles.
                            MessageBox.Show("One or more of the users in this workflow already exists" +
                                            Environment.NewLine +
                                            "These users have the same password as before, but the new roles have been added");
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(Application.Current.FindResource("UploadUsersFailed") + Environment.NewLine +
                                        e);
                        UploadButtonActive = true;
                        return;
                    }
                }
                MessageBox.Show((string)Application.Current.FindResource("UploadOk"));
                ClearFields();
            }
            UploadButtonActive = true;
        }

        private void ClearFields()
        {
            XmlFilePath = "";
            EventUris = "";
            ServerUri = "";
            WorkflowId = "";
            WorkflowName = "";
        }

        private static string[] GetUrls(string eventUrls)
        {
            return eventUrls.Replace(" ", "").Split(',');
        }
    }
}
