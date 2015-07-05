using System.ComponentModel;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Classes;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для RegistrationAndAuthorization.xaml
    /// </summary>
    public partial class RegistrationAndAuthorization : Window
    {
        bool _whetherToCreate = false;
        public RegistrationAndAuthorization()
        {
            InitializeComponent();
        }
        AccountInformation _ai; Loading _loading;
        public Socket ConnectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private void RegistrationAndAuthorizationWindow_Closing(object sender, CancelEventArgs cancelEventArgs)
        {
            if (!_whetherToCreate) return;
            _loading.Close();
            new MainWindow(ConnectionSocket, _ai).Show();
        }
        private void HyperLink_MouseLeave(object sender, MouseEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            if (hyperlink != null)
                hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
        }

        private void HyperLink_MouseEnter(object sender, MouseEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            if (hyperlink != null) hyperlink.Foreground = Brushes.Blue;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Functions.WorkWithEmailBox(MainGrid, LoginTextBox, e, 3);
        }
        private void signInButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoginTextBox.Text == "" || PasswordPasswordBox.Password == "") return; 
            _loading = new Loading(this);
            _loading.Show();

            ConnectionSocket = Functions.ReturnNewSocket(ConnectionSocket);
            Functions.SerializeAndSend(new Authorization(LoginTextBox.Text, PasswordPasswordBox.Password), ConnectionSocket);

            object obj;
            {
                byte[] byteBuffer = new byte[1024];
                if (ConnectionSocket.Receive(byteBuffer) == 0)
                {
                    MessageBox.Show("Получено 0 байт");
                }
                obj = Functions.Deserialize(byteBuffer);
            }
            if (!(obj is AccountInformation)) return;
            AccountInformation accountInf = obj as AccountInformation;
            if(accountInf.IsNull)
            {
                MessageBox.Show("Ошибка в логине или пароле!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                _loading.Close();
                LoginTextBox.Focus();
            }
            else
            {
                _whetherToCreate = true;
                _ai = accountInf;
                Close();
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            new Registration(this).ShowDialog();
        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {
            new RecoverPassword(this).ShowDialog();
        }
    }
}