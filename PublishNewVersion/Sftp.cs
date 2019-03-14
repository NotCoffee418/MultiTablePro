using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublishNewVersion
{
    class Sftp
    {
        public Sftp(string host, string user, string pass)
        {
            Host = host;
            User = user;
            Pass = pass;
        }

        private string Host { get; set; }
        private string User { get; set; }
        private string Pass { get; set; }

        // path should be valid directory
        internal bool Test(string remotePath)
        {
            try
            {
                using (var client = new SftpClient(Host, 22, User, Pass))
                {
                    client.Connect();
                    client.ChangeDirectory(remotePath);
                    client.ListDirectory(remotePath);
                    client.Disconnect();
                }
                MiniLogger.LogQueue.Enqueue("Successfully connected to SFTP");
                return true;
            }
            catch (Exception ex)
            {
                MiniLogger.LogQueue.Enqueue("Failed to connect to SFTP: " + ex.Message);
                return false;
            }
            
        }

        internal bool UploadFile(string remoteDir, string remoteFileName, string localFile)
        {
            try
            {
                using (var client = new SftpClient(Host, 22, User, Pass))
                {
                    client.Connect();
                    client.ChangeDirectory(remoteDir);

                    using (var fileStream = new FileStream(localFile, FileMode.Open))
                    {
                        Console.WriteLine("Uploading {0} ({1:N0} bytes)", localFile, fileStream.Length);
                        client.BufferSize = 4 * 1024; // bypass Payload error large files
                        client.UploadFile(fileStream, remoteFileName);
                    }

                    client.Disconnect();
                }
                MiniLogger.LogQueue.Enqueue("File successfully uploaded: " + Path.GetFileName(localFile));
                return true;
            }
            catch (Exception ex)
            {
                MiniLogger.LogQueue.Enqueue("Failed to upload file. " + ex.Message);
                return false;
            }
        }
    }
}
