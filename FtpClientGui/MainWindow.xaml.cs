#region

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FtpClientBase;

#endregion

namespace FtpClientGui
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    [SuppressMessage("ReSharper", "EmptyGeneralCatchClause")]
    public partial class MainWindow : Window
    {
        private const string LocalRootPath = "<root>";

        private Connection _connection;

        /// <summary>
        ///     <c>_localRootPath</c> represent viewing disks.
        /// </summary>
        private string _localCurrentPath = LocalRootPath;

        public MainWindow()
        {
            InitializeComponent();

            RefreshLocalView();
        }

        public void ConnectToServer(string host, int port, string username, string password)
        {
            try
            {
                _connection = new Connection(host, port);
                _connection.Establish(username, password);

                if (ToggleActiveButton.IsChecked ?? false) _connection.Performer.ActiveMode = true;
                else _connection.Performer.ActiveMode = false;

                RefreshRemoteView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            RemoteConnectDisconnect.Content = "Disconnect";
        }

        /// <summary>
        ///     Change local path by <c>_localRootPath</c> or entry name, then refresh local view.
        /// </summary>
        private void ChangeLocalPath(string entryName)
        {
            _localCurrentPath = _localCurrentPath == LocalRootPath
                ? entryName
                : Path.Combine(_localCurrentPath, entryName);
            RefreshLocalView();
        }

        private void ChangeRemotePath(string entryName)
        {
            _connection.Performer.ChangeDirectory(entryName);
            RefreshRemoteView();
        }

        /// <summary>
        ///     <c>_localCurrentPath</c> should be valid.
        /// </summary>
        private void RefreshLocalView()
        {
            // logics
            List<string> directories;
            var files = new List<string>();
            if (_localCurrentPath == LocalRootPath)
                directories = (from driveInfo in DriveInfo.GetDrives() select driveInfo.Name).ToList();
            else
                try
                {
                    directories = Directory.GetDirectories(_localCurrentPath).ToList();
                    files = Directory.GetFiles(_localCurrentPath).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    directories = new List<string>();
                    files = new List<string>();
                }

            // views
            LocalTextPathTextBox.Text = _localCurrentPath;
            LocalFileList.Items.Clear();
            foreach (var directory in directories)
            {
                var textBlock = new TextBlock
                {
                    FontWeight = FontWeights.Bold,
                    Text = _localCurrentPath == LocalRootPath ? directory : Path.GetFileName(directory)
                };
                textBlock.MouseLeftButtonDown += (sender, args) =>
                {
                    if (args.ClickCount == 2) ChangeLocalPath(directory);
                };
                LocalFileList.Items.Add(textBlock);
            }

            files.ForEach(f => LocalFileList.Items.Add(
                new TextBlock {Text = Path.GetFileName(f)}
            ));
        }

        private void RefreshRemoteView()
        {
            var currentPath = _connection.Performer.GetCurrentDirectory();
            var fileList = _connection.Performer.ListFiles(currentPath);

            RemotePathTextBox.Text = currentPath;
            RemoteFileList.Items.Clear();
            foreach (var file in fileList)
                if (file.IsDir)
                {
                    var textBlock = new TextBlock {FontWeight = FontWeights.Bold, Text = file.Name};
                    textBlock.MouseLeftButtonDown += (sender, args) =>
                    {
                        if (args.ClickCount == 2) ChangeRemotePath(file.Name);
                    };
                    RemoteFileList.Items.Add(textBlock);
                }
                else
                {
                    RemoteFileList.Items.Add(new TextBlock {Text = file.Name});
                }
        }

        private void LocalRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            RefreshLocalView();
        }

        private void LocalGoUp_OnClick(object sender, RoutedEventArgs e)
        {
            if (_localCurrentPath == LocalRootPath)
            {
                RefreshLocalView();
                return;
            }

            if (Path.GetFullPath(Path.Combine(_localCurrentPath, "..")) == _localCurrentPath)
            {
                _localCurrentPath = LocalRootPath;
                RefreshLocalView();
                return;
            }

            _localCurrentPath = Path.GetFullPath(Path.Combine(_localCurrentPath, ".."));
            RefreshLocalView();
        }

        private void LocalViewDisks_OnClick(object sender, RoutedEventArgs e)
        {
            _localCurrentPath = LocalRootPath;
            RefreshLocalView();
        }

        private void LocalGo_OnClick(object sender, RoutedEventArgs e)
        {
            if (LocalTextPathTextBox.Text == LocalRootPath)
                _localCurrentPath = LocalRootPath;
            else if (Directory.Exists(LocalTextPathTextBox.Text))
                _localCurrentPath = LocalTextPathTextBox.Text;
            else
                LocalTextPathTextBox.Text = _localCurrentPath;
            _localCurrentPath = Path.GetFullPath(_localCurrentPath);
            RefreshLocalView();
        }

        private void RemoteConnectDisconnect_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
            {
                new Login(this).ShowDialog();
            }
            else
            {
                _connection.Destory();
                RemoteConnectDisconnect.Content = "Connect";
                RemotePathTextBox.Text = "";
                RemoteFileList.Items.Clear();
            }
        }

        private void RemoteGoUp_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
                MessageBox.Show("Not connected");
            else
                ChangeRemotePath("..");
        }

        private void RemoteRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
                MessageBox.Show("Not connected");
            else
                RefreshRemoteView();
        }

        private void RemoteNewFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
            {
                MessageBox.Show("Not connected");
                return;
            }

            var inputWindow = new SingleInput("Name of the new folder?");
            inputWindow.ShowDialog();
            var inputText = inputWindow.InputText;
            try
            {
                _connection.Performer.MakeDirectory(inputText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            RefreshRemoteView();
        }

        private void RemoteRemoveFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
            {
                MessageBox.Show("Not connected");
                return;
            }

            var selectedFiles = RemoteFileList.SelectedItems;
            foreach (var selectedFile in selectedFiles)
            {
                var text = ((TextBlock) selectedFile).Text;
                try
                {
                    _connection.Performer.RemoveDirectory(text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            RefreshRemoteView();
        }

        private void RemoteRenameFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
            {
                MessageBox.Show("Not connected");
                return;
            }

            var selectedFiles = RemoteFileList.SelectedItems;
            if (selectedFiles.Count != 1)
            {
                MessageBox.Show("You can only rename ONE file as a time.");
            }
            else
            {
                var inputWindow = new SingleInput("New name of the folder?");
                inputWindow.ShowDialog();
                var inputText = inputWindow.InputText;
                try
                {
                    _connection.Performer.RenameFile(((TextBlock) selectedFiles[0]).Text, inputText);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                RefreshRemoteView();
            }
        }

        private void RemoteGo_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
            {
                MessageBox.Show("Not connected");
                return;
            }

            var targetPath = RemotePathTextBox.Text;
            var currentPath = _connection.Performer.GetCurrentDirectory();
            ChangeRemotePath("/");
            try
            {
                ChangeRemotePath(targetPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ChangeRemotePath(currentPath);
            }
        }

        private void ToggleActiveButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_connection != null && _connection.Connected) _connection.Performer.ActiveMode = true;
        }

        private void TogglePassiveButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_connection != null && _connection.Connected) _connection.Performer.ActiveMode = false;
        }

        private void ConfirmUpload_OnClick(object sender, RoutedEventArgs e)
        {
            string errorMessage = null;
            if (_connection == null || _connection.Connected == false)
                errorMessage = "Not connected.";
            else if (_localCurrentPath == LocalRootPath)
                errorMessage = "You can't upload from <root>.";
            if (errorMessage != null)
            {
                MessageBox.Show(errorMessage);
                return;
            }

            var remotePath = _connection.Performer.GetCurrentDirectory();
            foreach (var selecetedItem in LocalFileList.SelectedItems)
            {
                try
                {
                    var fileName = ((TextBlock) selecetedItem).Text;
                    var filePath = Path.Combine(_localCurrentPath, fileName);
                    _connection.Performer.UploadFile(remotePath + '/' + fileName, filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            RefreshRemoteView();
        }

        private void ConfirmDownload_OnClick(object sender, RoutedEventArgs e)
        {
            string errorMessage = null;
            if (_connection == null || _connection.Connected == false)
                errorMessage = "Not connected.";
            else if (_localCurrentPath == LocalRootPath)
                errorMessage = "You can't download to <root>.";
            if (errorMessage != null)
            {
                MessageBox.Show(errorMessage);
                return;
            }

            var remotePath = _connection.Performer.GetCurrentDirectory();
            foreach (var selecetedItem in RemoteFileList.SelectedItems)
            {
                try
                {
                    var fileName = ((TextBlock) selecetedItem).Text;
                    var filePath = remotePath + '/' + fileName;
                    _connection.Performer.DownloadFile(filePath, Path.Combine(_localCurrentPath, fileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            RefreshLocalView();
        }
    }
}