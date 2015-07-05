using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Classes;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public string Code = "";
        Loading loading;
        public Registration(Window parent)
        {
            InitializeComponent();
            Loaded += (wv, ve) => { loading = new Loading(this); };
            Owner = parent;
            dateDatePicker.SelectedDate = DateTime.Now;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //registrationButton.IsEnabled = false;
            if (NameTextBox.Text == "" || NameTextBox.Text == "" || emailTextBox.Text == "" || dateDatePicker.SelectedDate.Value == DateTime.Now || passwordPasswordBox.Password == "" || repeatPasswordPasswordBox.Password == "")
            {
                MessageBox.Show("Заполните все поля!", "Недочёт", MessageBoxButton.OK, MessageBoxImage.Warning);
                //registrationButton.IsEnabled = true;
                return;
            }
            if (String.CompareOrdinal(passwordPasswordBox.Password, repeatPasswordPasswordBox.Password) != 0)
            {
                passwordPasswordBox.Password = repeatPasswordPasswordBox.Password = string.Empty;
                MessageBox.Show("Введенные Вами пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                passwordPasswordBox.Focus();
                //registrationButton.IsEnabled = true;
                return;
            }
            if (manRadioButton.IsChecked == womanRadioButton.IsChecked == true)
            {
                MessageBox.Show("Выберите свой пол!", "Недочёт", MessageBoxButton.OK, MessageBoxImage.Warning);
                //registrationButton.IsEnabled = true;
                return;
            }
            loading.Show();
            try
            {
                socket = Functions.ReturnNewSocket(socket);
                Functions.SerializeAndSend(new UniquenessEmail(emailTextBox.Text), socket);
                {
                    byte[] bytefuffer = new byte[400];
                    if (socket.Receive(bytefuffer) == 0)
                    {
                        return;
                    }
                    else
                    {
                        object obj = Functions.Deserialize(bytefuffer);
                        if (obj is UniquenessEmail)
                        {
                            if ((obj as UniquenessEmail).YesNo)
                            {
                                emailTextBox.Text = string.Empty;
                                MessageBox.Show("Введенный Вами email уже зарегистрирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                emailTextBox.Focus();
                                loading.Close();
                                //registrationButton.IsEnabled = true;
                                return;
                            }
                        }
                    }
                }
                Action sendData = () =>
                {
                    Functions.SerializeAndSend(new AccountInformation(NameTextBox.Text, SurnameTextBox.Text, dateDatePicker.SelectedDate.Value,
                            emailTextBox.Text, manRadioButton.IsChecked.Value,
                            Functions.GetHash(passwordPasswordBox.Password)), socket);
                };
                Action act = () =>
                {
                    byte[] response = new byte[1024];
                    //stream=new MemoryStream();
                    //socket.Send(stream.ToArray());
                    while (true)
                    {
                        try
                        {
                            if (socket.Receive(response) == 0)
                            {
                                MessageBox.Show("Получено ноль байт!");
                                /*registrationButton.Dispatcher.Invoke(new Action(() =>
                                {
                                    registrationButton.IsEnabled = true;
                                }));*/
                                return;
                            }
                            if (Functions.Deserialize(response) is MessageFromServer)
                            {
                                loading.Dispatcher.Invoke(() => { loading.Close(); });
                                MessageBox.Show("Проверьте Вашу электронную почту! На Ваш адрес отправлен код подтвержденния.", "Ответ от сервера");
                                Dispatcher.Invoke(() =>
                                {
                                    Verification verifi = new Verification(this);
                                    verifi.Closing += (a, r) =>
                                    {
                                        sendData();
                                    };
                                    verifi.ShowDialog();
                                });
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace);
                        }
                    }
                };
                {
                    SendEmailRegistration message = new SendEmailRegistration
                    {
                        Name = NameTextBox.Text,
                        Surname = SurnameTextBox.Text,
                        Email = emailTextBox.Text,
                        Subject = "Подтверждение регистрации"
                    };
                    Code = Functions.GetRandomCode();
                    message.Body = NameTextBox.Text + ", Вы зарегестрировались в чате!" + Environment.NewLine + Environment.NewLine + "Вот код подтверждения: " + Code;
                    Functions.SerializeAndSend(message, socket);
                }
                act.BeginInvoke(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace);
            }
            //registrationButton.IsEnabled = true;
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            ComboBox comboBox = Functions.WorkWithEmailBox(miniGrid, emailTextBox, e, 2);
            if (comboBox != null)
            {
                Grid.SetColumn(comboBox, 2);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (socket.IsBound)
            {
                Functions.SerializeAndSend(new SocketDisconnect(false), socket);
            }
        }
    }
}