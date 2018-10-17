using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FtpClientGui
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private MainWindow _mainWindow;

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
