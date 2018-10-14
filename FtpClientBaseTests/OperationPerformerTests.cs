#region

using System;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace FtpClientBase.Tests
{
    [TestClass]
    public class OperationPerformerTests
    {
        private const string Host = "144.208.69.31";
        private const int Port = 21;
        private const string Username = "dlpuser@dlptest.com";
        private const string Password = "e73jzTRTNqCN9PYAAjjn";

        [TestMethod]
        public void LogInTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void LogOutTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void QuerySystemTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            Console.WriteLine(operationPerformer.QuerySystem());
            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void SetToBinaryTypeTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            operationPerformer.SetToBinaryType();
            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void ListFilesTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            operationPerformer.ActiveMode = false;
            operationPerformer.ListFiles("/").ForEach(x => Console.WriteLine(x));
            operationPerformer.ActiveMode = true;
            operationPerformer.ListFiles("/").ForEach(x => Console.WriteLine(x));
            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DownloadFileTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            operationPerformer.ActiveMode = false;
            operationPerformer.DownloadFile("FTP.txt", "G:\\hp\\Desktop\\test1.txt");
            operationPerformer.ActiveMode = true;
            operationPerformer.DownloadFile("FTP.txt", "G:\\hp\\Desktop\\test2.txt");
            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void UploadFileTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);

            operationPerformer.ActiveMode = false;
            operationPerformer.UploadFile("haha.txt", "G:\\hp\\Desktop\\test1.txt");
            var fileList = operationPerformer.ListFiles("/");
            Assert.IsTrue(fileList.FindIndex(x => x.Name == "haha.txt") >= 0);

            operationPerformer.ActiveMode = true;
            operationPerformer.UploadFile("hehe.txt", "G:\\hp\\Desktop\\test1.txt");
            fileList = operationPerformer.ListFiles("/");
            Assert.IsTrue(fileList.FindIndex(x => x.Name == "hehe.txt") >= 0);

            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void ChangeDirectoryTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            operationPerformer.ChangeDirectory("..");
            operationPerformer.ChangeDirectory(".");
            operationPerformer.ChangeDirectory(".settings");
            operationPerformer.ChangeDirectory(".././../.settings/../.");
            operationPerformer.LogOut();

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetCurrentDirectoryTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);
            Assert.IsTrue(operationPerformer.GetCurrentDirectory() == "/");
            operationPerformer.ChangeDirectory(".settings");
            Assert.IsTrue(operationPerformer.GetCurrentDirectory() == "/.settings");
            operationPerformer.ChangeDirectory("../.././../.settings/../.");
            Assert.IsTrue(operationPerformer.GetCurrentDirectory() == "/");
            operationPerformer.LogOut();

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void MakeDirectoryTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);

            try
            {
                operationPerformer.RemoveDirectory("testDir/testR");
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                operationPerformer.RemoveDirectory("testDir");
            }
            catch (Exception)
            {
                // ignored
            }

            operationPerformer.MakeDirectory("testDir");
            var fileList = operationPerformer.ListFiles();
            Assert.IsTrue(fileList.FindIndex(x => x.Name == "testDir" && x.IsDir) != -1);

            operationPerformer.MakeDirectory("testDir/testR");
            operationPerformer.ChangeDirectory("testDir");
            fileList = operationPerformer.ListFiles();
            Assert.IsTrue(fileList.FindIndex(x => x.Name == "testR" && x.IsDir) != -1);

            operationPerformer.ChangeDirectory("..");

            try
            {
                operationPerformer.RemoveDirectory("testDir/testR");
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                operationPerformer.RemoveDirectory("testDir");
            }
            catch (Exception)
            {
                // ignored
            }

            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void RemoveDirectoryTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(Host, Port);
            var operationPerformer = new OperationPerformer(socket);
            operationPerformer.OmitWelcomeResponse();
            operationPerformer.LogIn(Username, Password);

            try
            {
                operationPerformer.RemoveDirectory("testDir/testR");
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                operationPerformer.RemoveDirectory("testDir");
            }
            catch (Exception)
            {
                // ignored
            }

            operationPerformer.MakeDirectory("testDir");
            operationPerformer.MakeDirectory("testDir/testR");
            operationPerformer.ChangeDirectory("testDir");
            operationPerformer.RemoveDirectory("testR");
            Assert.IsTrue(operationPerformer.ListFiles().Count == 2);
            operationPerformer.ChangeDirectory("..");
            operationPerformer.RemoveDirectory("testDir");
            Assert.IsTrue(operationPerformer.ListFiles().FindIndex(x => x.Name == "testDir") == -1);

            operationPerformer.LogOut();
            Assert.IsTrue(true);
        }
    }
}