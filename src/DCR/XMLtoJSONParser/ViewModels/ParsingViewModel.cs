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
            _serverUri = "http://";
            _eventUris = "http://";
        }

        public string XmlFilePath
        {
            get { return _xmlFilePath; }
            set
            {
                _xmlFilePath = value;
                NotifyPropertyChanged("XmlFilePath");
            }
        }

        public string WorkflowId
        {
            get { return _workflowId; }
            set
            {
                _workflowId = value;
                NotifyPropertyChanged("WorkflowId");
            }
        }

        public string WorkflowName
        {
            get { return _workflowName; }
            set
            {
                _workflowName = value;
                NotifyPropertyChanged("WorkflowName");
            }
        }

        public string ServerUri
        {
            get { return _serverUri; }
            set
            {
                _serverUri = value;
                NotifyPropertyChanged("ServerUri");
            }
        }

        public string DefaultPassword
        {
            get { return _defaultPassword; }
            set
            {
                _defaultPassword = value;
                NotifyPropertyChanged("DefaultPassword");
            }
        }

        public string EventUris
        {
            get { return _eventUris; }
            set
            {
                _eventUris = value;
                NotifyPropertyChanged("EventUris");
            }
        }

        public bool CreateWorkflow
        {
            get { return _createWorkflow; }
            set
            {
                _createWorkflow = value;
                NotifyPropertyChanged("CreateWorkflow");
            }
        }

        public bool CreateUsers
        {
            get { return _createUsers; }
            set
            {
                _createUsers = value;
                NotifyPropertyChanged("CreateUsers");
            }
        }

        public bool UploadButtonActive
        {
            get { return _uploadButtonActive; }
            set
            {
                _uploadButtonActive = value;
                NotifyPropertyChanged("UploadButtonActive");
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
