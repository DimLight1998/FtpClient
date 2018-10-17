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
    /// Interaction logic for SingleInput.xaml
    /// </summary>
    public partial class SingleInput : Window
    {
        public SingleInput(string hintText)
        {
            InitializeComponent();
            HintText.Content = hintText;
        }

        public string InputText { get; set; }

        private void SubmitButton_OnClick(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text;
            Close();
        }
    }
}
