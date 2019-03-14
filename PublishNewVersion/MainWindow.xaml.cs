using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Windows.Threading;

namespace PublishNewVersion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set branch content
            branchCb.ItemsSource = new string[]
            {
                "INTERNAL",
                "BETA",
                "RELEASE"
            };
            branchCb.SelectedIndex = 0;

            // Logger
            new Thread(() => RunLogger()).Start();

            // bgworker
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += Worker_ProgressChanged;

            // Set access token
            accessTokenTxt.Text = Properties.Settings.Default.AccessToken;
            setupFilePathTxt.Text = Properties.Settings.Default.SetupFilePath;
            releaseDirTxt.Text = Properties.Settings.Default.ReleaseDirPath;

            // Set sftp stuff
            hostTxt.Text = Properties.Settings.Default.SftpHost;
            userTxt.Text = Properties.Settings.Default.SftpUser;
            passTxt.Password = Properties.Settings.Default.SftpPass;
            sftpPathTxt.Text = Properties.Settings.Default.SftpPath;
        }

        

        private readonly BackgroundWorker worker = new BackgroundWorker();
        private bool IsRunning = true;

        private void RunLogger()
        {
            while (IsRunning)
            {
                if (MiniLogger.LogQueue.Count == 0)
                    Thread.Sleep(100);
                else
                    logTb.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() => { logTb.Text += MiniLogger.LogQueue.Dequeue() + Environment.NewLine; }));                    
            }            
        }

        private void PublishBtn_Click(object sender, RoutedEventArgs e)
        {
            publishBtn.IsEnabled = false;
            Dictionary<string, string> inputData = new Dictionary<string, string>
            {
                { "version", versionTxt.Text },
                { "setupFile", setupFilePathTxt.Text },
                { "releaseDir", releaseDirTxt.Text },
                { "accessToken", accessTokenTxt.Text },
                { "host", hostTxt.Text },
                { "user", userTxt.Text },
                { "pass", passTxt.Password.ToString() },
                { "sftpPath", sftpPathTxt.Text },
                { "branch", (string)branchCb.SelectedValue},
                { "changelog", changeLogTxt.Text}

            };
            worker.RunWorkerAsync(inputData);
        }


        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dictionary<string, string> inputData = (Dictionary<string, string>)e.Argument;

            // Validate version
            string version = inputData["version"];
            Regex rVersion = new Regex(@"\d+\.\d+\.\d+\.\d+");
            if (!rVersion.IsMatch(version))
            {
                MiniLogger.LogQueue.Enqueue("Version string is invalid format. Should be x.x.x.x. Cancelling.");
                return;
            }
            worker.ReportProgress(1);

            // validate setup file
            string setupFile = inputData["setupFile"];
            if (!File.Exists(setupFile))
            {
                MiniLogger.LogQueue.Enqueue("Setup file does not exist. Cancelling.");
                return;
            }
            Properties.Settings.Default.SetupFilePath = setupFile;
            worker.ReportProgress(2);

            // validate release dir
            string releaseDir = inputData["releaseDir"];
            if (!Directory.Exists(releaseDir) || !releaseDir.Contains("Release"))
            {
                MiniLogger.LogQueue.Enqueue("Release directory does not exist or does not contain \"Release\". Cancelling.");
                return;
            }
            Properties.Settings.Default.ReleaseDirPath = releaseDir;
            Properties.Settings.Default.Save();
            worker.ReportProgress(3);

            // validate access token & store on change
            string accessToken = inputData["accessToken"];
            if (accessToken == "")
            {
                MiniLogger.LogQueue.Enqueue("Invalid access token");
                return;
            }
            else if (accessToken != Properties.Settings.Default.AccessToken)
            {
                Properties.Settings.Default.AccessToken = accessToken;
                Properties.Settings.Default.Save();
            }
            worker.ReportProgress(5);

            // store ftp info on change
            string host = inputData["host"];
            string user = inputData["user"];
            string pass = inputData["pass"];
            string sftpPath = inputData["sftpPath"];

            // Update changes
            if (Properties.Settings.Default.SftpHost != host)
                Properties.Settings.Default.SftpHost = host;
            if (Properties.Settings.Default.SftpUser != user)
                Properties.Settings.Default.SftpUser = user;
            if (Properties.Settings.Default.SftpPass != pass)
                Properties.Settings.Default.SftpPass = pass;
            if (Properties.Settings.Default.SftpPath != sftpPath)
                Properties.Settings.Default.SftpPath = sftpPath;
            Properties.Settings.Default.Save();
            worker.ReportProgress(7);

            // Validate SFTP
            Sftp sftp = new Sftp(host, user, pass);
            if (!sftp.Test(sftpPath))
            {
                MiniLogger.LogQueue.Enqueue("Problem connecting to SFTP server.");
                return;
            }
            worker.ReportProgress(15);

            // Validate changelog
            if (inputData["changelog"] == "")
            {
                MiniLogger.LogQueue.Enqueue("Changelog is empty. Write some changes!");
                return;
            }

            // Push to DB
            string json = "";
            try
            {
                var values = new Dictionary<string, string>
                {
                    {"version", version},
                    {"branch",  inputData["branch"] },
                    {"changelog",  inputData["changelog"] },
                    {"product_group", "1" },
                    {"access_token", accessToken}
                };
                json = ApiCreateBuild(values).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MiniLogger.LogQueue.Enqueue("Error connecting to API: " + ex.Message);
                return;
            }
            worker.ReportProgress(25);

            // Get version ID
            int versionId = 0;
            try
            {
                dynamic apiResponse = JObject.Parse(json);
                if (apiResponse.errors.Count > 0)
                    throw new Exception("Error in output: " + apiResponse.errors[0]);
                else versionId = apiResponse.result.version_id.ToObject<int>();
                MiniLogger.LogQueue.Enqueue("Created new version in database. ID: " + versionId);
            }
            catch (Exception ex)
            {
                MiniLogger.LogQueue.Enqueue("Error deserializing json or parsing output to int: " + ex.Message);
                return;
            }
            worker.ReportProgress(30);

            // Upload setup file
            try
            {
                MiniLogger.LogQueue.Enqueue("Uploading setup file...");
                sftp.UploadFile(sftpPath + "/setup/", versionId + ".msi", setupFile);
                MiniLogger.LogQueue.Enqueue("Setup file uploaded.");
                worker.ReportProgress(50);
            }
            catch (Exception ex)
            {
                MiniLogger.LogQueue.Enqueue("Failed to upload setup file" + ex.Message);
            }

            // Compress all dll/exe files in release dir
            string zFile = versionId + ".zip";
            try
            {
                using (ZipArchive archive = ZipFile.Open(zFile, ZipArchiveMode.Create))
                {
                    foreach (string fPath in Directory.EnumerateFiles(releaseDir))
                    {
                        string ext = System.IO.Path.GetExtension(fPath);
                        if (ext == ".exe" || ext == ".dll")
                        {
                            archive.CreateEntryFromFile(fPath, System.IO.Path.GetFileName(fPath));
                        }
                    }
                }

                MiniLogger.LogQueue.Enqueue("Created zip file for update files.");
            }
            catch (Exception ex)
            {
                MiniLogger.LogQueue.Enqueue("Error creating zip file: " + ex.Message);
            }
            worker.ReportProgress(60);

            // Upload Update zip
            try
            {
                MiniLogger.LogQueue.Enqueue("Uploading update file...");
                sftp.UploadFile(sftpPath + "/update/", versionId + ".zip", zFile);
                MiniLogger.LogQueue.Enqueue("Setup update uploaded.");
                worker.ReportProgress(95);
            }
            catch (Exception ex)
            {
                MiniLogger.LogQueue.Enqueue("Failed to upload update zip file" + ex.Message);
            }

            // Report success
            MiniLogger.LogQueue.Enqueue("New build was successfully published");
            worker.ReportProgress(100);
        }


        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MiniLogger.LogQueue.Enqueue("Worker Complete.");
            publishBtn.IsEnabled = true;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            statusPb.Value = e.ProgressPercentage;
        }


        private static readonly HttpClient client = new HttpClient();
        private async Task<string> ApiCreateBuild(Dictionary<string, string> values)
        {
            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://multitablepro.com/api/publish_new_version", content);
            return await response.Content.ReadAsStringAsync();
        }

        private void ValidateSftpBtn_Click(object sender, RoutedEventArgs e)
        {
            string host = hostTxt.Text;
            string user = userTxt.Text;
            string pass = passTxt.Password.ToString();
            string sftpPath = sftpPathTxt.Text;
            Sftp sftp = new Sftp(host, user, pass);
            sftp.Test(sftpPath);
        }
    }
}
