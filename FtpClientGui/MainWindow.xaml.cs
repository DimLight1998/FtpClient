#region

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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

        private readonly List<(string FileName, bool IsPending)> _tasks = new List<(string FileName, bool IsPending)>();
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
            Task.Run(() =>
            {
                try
                {
                    _connection = new Connection(
                        host, port,
                        ss => Dispatcher.Invoke(() =>
                        {
                            LogFlow.Blocks.Add(new Paragraph(new Run(ss))
                            {
                                Margin = new Thickness(0),
                                Foreground = new SolidColorBrush(Colors.DarkRed)
                            });
                        }),
                        sr => Dispatcher.Invoke(() =>
                        {
                            LogFlow.Blocks.Add(new Paragraph(new Run(sr))
                            {
                                Margin = new Thickness(0),
                                Foreground = new SolidColorBrush(Colors.DarkBlue)
                            });
                        }));
                    _connection.Establish(username, password);

                    Dispatcher.Invoke(() =>
                    {
                        if (ToggleActiveButton.IsChecked ?? false) _connection.Performer.ActiveMode = true;
                        else _connection.Performer.ActiveMode = false;
                    });

                    Dispatcher.Invoke(RefreshRemoteView);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => { MessageBox.Show(ex.Message); });
                    return;
                }

                Dispatcher.Invoke(() => { RemoteConnectDisconnect.Content = "Disconnect"; });
            });
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
            DisableNetworkInputs();
            Task.Run(() =>
            {
                _connection.Performer.ChangeDirectory(entryName);

                Dispatcher.Invoke(() =>
                {
                    RefreshRemoteView();
                    EnableNetworkInputs();
                });
            });
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
            DisableNetworkInputs();
            Task.Run(() =>
            {
                var currentPath = _connection.Performer.GetCurrentDirectory();
                var fileList = _connection.Performer.ListFiles(currentPath);

                Dispatcher.Invoke(() =>
                {
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

                    EnableNetworkInputs();
                });
            });
        }

        private void RefreshTaskList()
        {
            TasksListView.Items.Clear();
            foreach (var (fileName, isPending) in _tasks)
                TasksListView.Items.Add(new TextBlock
                {
                    Text = (isPending ? "[Pending]" : "[Done]") + " " + fileName,
                    Foreground = new SolidColorBrush(isPending ? Colors.DarkRed : Colors.DarkGreen)
                });
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

            if (_localCurrentPath != LocalRootPath)
                _localCurrentPath = Path.GetFullPath(_localCurrentPath);
            RefreshLocalView();
        }

        private void RemoteConnectDisconnect_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
                new Login(this).ShowDialog();
            else
                Task.Run(() =>
                {
                    _connection.Destory();

                    Dispatcher.Invoke(() =>
                    {
                        RemoteConnectDisconnect.Content = "Connect";
                        RemotePathTextBox.Text = "";
                        RemoteFileList.Items.Clear();

                        LogFlow.Blocks.Clear();
                        _tasks.Clear();
                        TasksListView.Items.Clear();
                    });
                });
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
            DisableNetworkInputs();

            var inputWindow = new SingleInput("Name of the new folder?");
            inputWindow.ShowDialog();
            var inputText = inputWindow.InputText;

            Task.Run(() =>
            {
                try
                {
                    _connection.Performer.MakeDirectory(inputText);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(ex.Message); 
                        EnableNetworkInputs();
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    RefreshRemoteView();
                    EnableNetworkInputs();
                });
            });
        }

        private void RemoteRemoveFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
            {
                MessageBox.Show("Not connected");
                return;
            }
            DisableNetworkInputs();

            var selectedTexts = new List<string>();
            foreach (var selectedFile in RemoteFileList.SelectedItems)
                selectedTexts.Add(((TextBlock) selectedFile).Text);

            Task.Run(() =>
            {
                foreach (var text in selectedTexts)
                    try
                    {
                        _connection.Performer.RemoveDirectory(text);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(ex.Message); 
                            EnableNetworkInputs();
                        });
                    }

                Dispatcher.Invoke(() =>
                {
                    RefreshRemoteView();
                    EnableNetworkInputs();
                });
            });
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
                DisableNetworkInputs();
                var inputWindow = new SingleInput("New name of the folder?");
                inputWindow.ShowDialog();
                var inputText = inputWindow.InputText;

                var oldName = ((TextBlock) selectedFiles[0]).Text;

                Task.Run(() =>
                {
                    try
                    {
                        _connection.Performer.RenameFile(oldName, inputText);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(ex.Message); 
                            EnableNetworkInputs();
                        });
                    }

                    Dispatcher.Invoke(() =>
                    {
                        RefreshRemoteView();
                        EnableNetworkInputs();
                    });
                });
            }
        }

        private void RemoteGo_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.Connected == false)
            {
                MessageBox.Show("Not connected");
                return;
            }

            DisableNetworkInputs();
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
            EnableNetworkInputs();
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

            DisableNetworkInputs();
            foreach (var selecetedItem in LocalFileList.SelectedItems)
            {
                var fileName = ((TextBlock) selecetedItem).Text;
                AddNewTask(fileName);
            }

            var fileNames = new List<string>();
            var filePaths = new List<string>();
            foreach (var selecetedItem in LocalFileList.SelectedItems)
            {
                var fileName = ((TextBlock) selecetedItem).Text;
                fileNames.Add(fileName);
                filePaths.Add(Path.Combine(_localCurrentPath, fileName));
            }

            Task.Run(() =>
            {
                var remotePath = _connection.Performer.GetCurrentDirectory();
                for (var i = 0; i < fileNames.Count; i++)
                {
                    var fileName = fileNames[i];
                    var filePath = filePaths[i];
                    try
                    {
                        _connection.Performer.UploadFile(remotePath + '/' + fileName, filePath);
                        Dispatcher.Invoke(() => { RemoveTask(fileName); });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => { MessageBox.Show(ex.Message); });
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    RefreshRemoteView();
                    EnableNetworkInputs();
                });
            });
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

            DisableNetworkInputs();
            foreach (var selecetedItem in RemoteFileList.SelectedItems)
            {
                var fileName = ((TextBlock) selecetedItem).Text;
                AddNewTask(fileName);
            }

            var remotePath = _connection.Performer.GetCurrentDirectory();

            var fileNames = new List<string>();
            foreach (var selecetedItem in RemoteFileList.SelectedItems)
            {
                var fileName = ((TextBlock) selecetedItem).Text;
                fileNames.Add(fileName);
            }

            Task.Run(() =>
            {
                foreach (var fileName in fileNames)
                    try
                    {
                        var filePath = remotePath + '/' + fileName;
                        _connection.Performer.DownloadFile(filePath, Path.Combine(_localCurrentPath, fileName));
                        Dispatcher.Invoke(() => { RemoveTask(fileName); });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => { MessageBox.Show(ex.Message); });
                    }

                Dispatcher.Invoke(() =>
                {
                    RefreshLocalView();
                    EnableNetworkInputs();
                });
            });
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange > 0)
                LogScrollViewer.ScrollToBottom();
        }

        private void AddNewTask(string name)
        {
            _tasks.Add((name, true));
            RefreshTaskList();
        }

        private void RemoveTask(string name)
        {
            for (var i = 0; i < _tasks.Count; i++)
            {
                if (_tasks[i].FileName != name || !_tasks[i].IsPending) continue;
                _tasks[i] = (_tasks[i].FileName, false);
                RefreshTaskList();
                return;
            }

            RefreshTaskList();
        }

        private void DisableNetworkInputs()
        {
            RemoteConnectDisconnect.IsEnabled = false;
            RemoteGoUp.IsEnabled = false;
            RemoteRefresh.IsEnabled = false;
            RemoteNewFolder.IsEnabled = false;
            RemoteRemoveFolder.IsEnabled = false;
            RemoteRenameFile.IsEnabled = false;
            ConfirmUpload.IsEnabled = false;
            ConfirmDownload.IsEnabled = false;
            RemoteGo.IsEnabled = false;
        }

        private void EnableNetworkInputs()
        {
            RemoteConnectDisconnect.IsEnabled = true;
            RemoteGoUp.IsEnabled = true;
            RemoteRefresh.IsEnabled = true;
            RemoteNewFolder.IsEnabled = true;
            RemoteRemoveFolder.IsEnabled = true;
            RemoteRenameFile.IsEnabled = true;
            ConfirmUpload.IsEnabled = true;
            ConfirmDownload.IsEnabled = true;
            RemoteGo.IsEnabled = true;
        }
    }
}