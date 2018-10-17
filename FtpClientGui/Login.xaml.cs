#region

using System;
using System.Windows;

#endregion

namespace FtpClientGui
{
    /// <summary>
    ///     Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private readonly MainWindow _mainWindow;

        public Login(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Connect_OnClick(object sender, RoutedEventArgs e)
        {
            int port;
            try
            {
                port = int.Parse(PortInput.Text);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                MessageBox.Show("Invalid port");
                return;
            }

            _mainWindow.ConnectToServer(HostnameInput.Text, port, UserNameInput.Text, PasswordInput.Password);
            Close();
        }
    }
}