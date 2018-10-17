#region

using System.Windows;

#endregion

namespace FtpClientGui
{
    /// <summary>
    ///     Interaction logic for SingleInput.xaml
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