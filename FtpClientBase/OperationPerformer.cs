#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#endregion

namespace FtpClientBase
{
    public class OperationPerformer
    {
        private readonly CommandSender _commandSender;

        public OperationPerformer(Socket socket, CommandSender.OnLogGenerated onSend = null,
            CommandSender.OnLogGenerated onReceive = null)
        {
            _commandSender = new CommandSender(socket, onSend, onReceive);
        }

        public bool ActiveMode { set; get; } = false;

        public void OmitWelcomeResponse()
        {
            var (code, response) = _commandSender.GetNextResponse();
            if (code != 220) throw new Exception(response);
        }

        public void LogIn(string username, string password)
        {
            var (code, response) = _commandSender.User(username);
            if (code != 331) throw new Exception(response);
            (code, response) = _commandSender.Pass(password);
            if (code != 230) throw new Exception(response);

            // we only support binary mode, ensure it
            SetToBinaryType();
        }

        public void LogOut()
        {
            var (code, response) = _commandSender.Quit();
            if (code != 221) throw new Exception(response);
        }

        public string QuerySystem()
        {
            var (code, response) = _commandSender.Syst();
            if (code != 215) throw new Exception(response);
            return response;
        }

        public void SetToBinaryType()
        {
            var (code, response) = _commandSender.Type();
            if (code != 200) throw new Exception(response);
        }

        public void DownloadFile(string remoteFilepath, string localFilepath)
        {
            if (ActiveMode)
            {
                var tcpSocket = PrepareActiveMode();

                var buffer = new byte[1024];
                Socket socket = null;
                var downloadingTask = Task.Run(() =>
                {
                    socket = tcpSocket.Accept();
                    using (var file = File.OpenWrite(localFilepath))
                    {
                        while (NetworkUtils.IsSocketConnected(socket))
                        {
                            var size = socket.Receive(buffer);
                            file.Write(buffer, 0, size);
                        }
                    }

                    socket.Disconnect(false);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                });

                var (code, response) = _commandSender.Retr(remoteFilepath);
                if (code != 150) throw new Exception(response);

                (code, response) = _commandSender.GetNextResponse();
                if (code != 226) throw new Exception(response);

                downloadingTask.Wait();
            }
            else
            {
                var tcpSocket = PreparePassiveMode();

                // receive file on `tcpSocket`
                var buffer = new byte[1024];
                var downloadingTask = Task.Run(() =>
                {
                    using (var file = File.OpenWrite(localFilepath))
                    {
                        while (NetworkUtils.IsSocketConnected(tcpSocket))
                        {
                            var size = tcpSocket.Receive(buffer);
                            file.Write(buffer, 0, size);
                        }
                    }

                    tcpSocket.Disconnect(false);
                    tcpSocket.Shutdown(SocketShutdown.Both);
                    tcpSocket.Close();
                });

                var (code, response) = _commandSender.Retr(remoteFilepath);
                if (code != 150) throw new Exception(response);

                (code, response) = _commandSender.GetNextResponse();
                if (code != 226) throw new Exception(response);

                downloadingTask.Wait();
            }
        }

        public void UploadFile(string remoteFilepath, string localFilepath)
        {
            if (ActiveMode)
            {
                var tcpSocket = PrepareActiveMode();

                var uploadingTask = Task.Run(() =>
                {
                    var socket = tcpSocket.Accept();
                    socket.Send(File.ReadAllBytes(localFilepath));
                    socket.Disconnect(false);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    tcpSocket.Close();
                });

                var (code, response) = _commandSender.Stor(remoteFilepath);
                if (code != 150) throw new Exception(response);

                (code, response) = _commandSender.GetNextResponse();
                if (code != 226) throw new Exception(response);
                uploadingTask.Wait();
            }
            else
            {
                var tcpSocket = PreparePassiveMode();

                // send file using `tcpSocket`
                var uploadingTask = Task.Run(() =>
                {
                    tcpSocket.Send(File.ReadAllBytes(localFilepath));
                    tcpSocket.Disconnect(false);
                    tcpSocket.Shutdown(SocketShutdown.Both);
                    tcpSocket.Close();
                });

                var (code, response) = _commandSender.Stor(remoteFilepath);
                if (code != 150) throw new Exception(response);

                (code, response) = _commandSender.GetNextResponse();
                if (code != 226) throw new Exception(response);
                uploadingTask.Wait();
            }
        }

        public void ChangeDirectory(string pathname)
        {
            var (code, response) = _commandSender.Cwd(pathname);
            if (code != 250) throw new Exception(response);
        }

        public string GetCurrentDirectory()
        {
            var (code, response) = _commandSender.Pwd();
            if (code != 257) throw new Exception(response);
            var match = Regex.Match(response, "\\d\\d\\d \"(.*?)\".*");
            return match.Groups[1].Value;
        }

        public void MakeDirectory(string pathname)
        {
            var (code, response) = _commandSender.Mkd(pathname);
            if (code != 257) throw new Exception(response);
        }

        public void RemoveDirectory(string pathname)
        {
            var (code, response) = _commandSender.Rmd(pathname);
            if (code != 250) throw new Exception(response);
        }

        public List<(bool IsDir, long Size, string LastModificationTime, string Name)> ListFiles(string filepath = null)
        {
            var fileList = "";
            if (ActiveMode)
            {
                var tcpSocket = PrepareActiveMode();

                var buffer = new byte[1024];
                Socket socket = null;
                var retrivalTask = Task.Run(() =>
                {
                    socket = tcpSocket.Accept();
                    while (NetworkUtils.IsSocketConnected(socket))
                    {
                        var size = socket.Receive(buffer);
                        fileList += Encoding.ASCII.GetString(buffer, 0, size);
                    }

                    socket.Disconnect(false);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                });

                var (code, response) = _commandSender.List(filepath);
                if (code != 150) throw new Exception(response);

                (code, response) = _commandSender.GetNextResponse();
                if (code != 226) throw new Exception(response);

                tcpSocket.Close();
                retrivalTask.Wait();
            }
            else
            {
                var tcpSocket = PreparePassiveMode();

                // receive list on `tcpSocket`
                var buffer = new byte[1024];

                var retrivalTask = Task.Run(() =>
                {
                    while (NetworkUtils.IsSocketConnected(tcpSocket))
                    {
                        var size = tcpSocket.Receive(buffer);
                        fileList += Encoding.ASCII.GetString(buffer, 0, size);
                    }

                    tcpSocket.Disconnect(false);
                    tcpSocket.Shutdown(SocketShutdown.Both);
                    tcpSocket.Close();
                });

                var (code, response) = _commandSender.List(filepath);
                if (code != 150) throw new Exception(response);

                (code, response) = _commandSender.GetNextResponse();
                if (code != 226) throw new Exception(response);

                retrivalTask.Wait();
            }

            var slices = fileList.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            var ret = new List<(bool IsDir, long Size, string LastModificationTime, string Name)>();
            foreach (var slice in slices)
            {
                var match = Regex.Match(slice.Trim(),
                    @"^(.{10})\s+\d+\s+.*?\s+.*?\s+(\d+)\s+(.*?\s+.*?\s+.*?)\s+(.*)$");
                var isDir = match.Groups[1].Value.StartsWith("d");
                var size = long.Parse(match.Groups[2].Value);
                var lastModificationTime = match.Groups[3].Value;
                var name = match.Groups[4].Value;
                ret.Add((isDir, size, lastModificationTime, name));
            }

            return ret;
        }

        public void RenameFile(string oldName, string newName)
        {
            var (code, response) = _commandSender.Rnfr(oldName);
            if (code != 350) throw new Exception(response);
            (code, response) = _commandSender.Rnto(newName);
            if (code != 250) throw new Exception(response);
        }

        /// <summary>This method will send a passive command to the server, and create a tcp socket connect to it</summary>
        /// <returns>The socket connected. You can receive and send data through this socket.</returns>
        private Socket PreparePassiveMode()
        {
            var (code, response) = _commandSender.Pasv();
            if (code != 227) throw new Exception(response);
            var epStringSlices = Regex.Match(response, @"\((.*?)\)").Groups[1].Value.Split(',');
            var endPoint = new IPEndPoint(new IPAddress(new[]
                {
                    byte.Parse(epStringSlices[0]),
                    byte.Parse(epStringSlices[1]),
                    byte.Parse(epStringSlices[2]),
                    byte.Parse(epStringSlices[3])
                }), int.Parse(epStringSlices[4]) * 256 + int.Parse(epStringSlices[5])
            );

            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.Connect(endPoint);

            return tcpSocket;
        }

        /// <summary>
        ///     This method will send an active command to the server, and create a tcp socket listening for incoming
        ///     connection.
        /// </summary>
        /// <returns>A tcp socket listening for incoming connection.</returns>
        private Socket PrepareActiveMode()
        {
            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            tcpSocket.Listen(4);

            var localIp = NetworkUtils.GetLocalIpAddress();
            var localPort = ((IPEndPoint) tcpSocket.LocalEndPoint).Port;
            var (code, response) = _commandSender.Port(new IPEndPoint(localIp, localPort));
            if (code != 200) throw new Exception(response);

            return tcpSocket;
        }
    }
}