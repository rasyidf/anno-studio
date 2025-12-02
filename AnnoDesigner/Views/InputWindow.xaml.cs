using System.Windows;
using AnnoDesigner.ViewModels;

namespace AnnoDesigner
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow
    {
        public InputWindow()
        {
            InitializeComponent();
        }

        public InputWindow(MainViewModel context, string message, string title, string defaultValue = "") : this()
        {
            InitializeComponent();

            DataContext = context;

            Loaded += new RoutedEventHandler(InputWindow_Loaded);

            this.message.Text = message;
            Title = title;
            input.Text = defaultValue;
        }

        private void InputWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _ = input.Focus();
        }

        public static string Prompt(MainViewModel context, string message, string title, string defaultValue = "")
        {
            var inputWindow = new InputWindow(context, message, title, defaultValue);
            _ = inputWindow.ShowDialog();

            return inputWindow.DialogResult == true ? inputWindow.ResponseText : null;
        }

        public string ResponseText
        {
            get { return input.Text; }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}