using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Classes;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для RecoverPassword.xaml
    /// </summary>
    public partial class RecoverPassword : Window
    {
        public RecoverPassword(Window parent)
        {
            InitializeComponent();
            Owner = parent;
        }

        private Socket connectionSocket;
        private string code;
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Functions.WorkWithEmailBox(MiniGrid, LoginTextBox, e, 4);
        }

        private void LoginTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            connectionSocket = (Owner as RegistrationAndAuthorization).ConnectionSocket;
            connectionSocket = Functions.ReturnNewSocket(connectionSocket);
            Functions.SerializeAndSend(new UniquenessEmail(LoginTextBox.Text), connectionSocket);
            byte[] answerServer = new byte[1024];
            {
                if (connectionSocket.Receive(answerServer) != 0)
                {
                    object obj = Functions.Deserialize(answerServer);
                    if (obj is UniquenessEmail)
                    {
                        UniquenessEmail message = (obj as UniquenessEmail);
                        if (message.YesNo)
                        {
                            MessageBox.Show(
                                "На Ваш электронный адрес будет отправлено сообщение с кодом для подтверждения.",
                                "Успех!", MessageBoxButton.OK, MessageBoxImage.Information);
                            Functions.SerializeAndSend(new RecoverPasswordMessage(LoginTextBox.Text), connectionSocket);
                            new Thread(Listen_server).Start(connectionSocket);
                        }
                        else
                        {
                            MessageBox.Show("Указанный Вами email адрес отсутствует в базе данных!", "Ошибка!",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void Listen_server(object socket)
        {
            Socket connectionSocket = socket as Socket;
            while (true)
            {
                try
                {
                    byte[] serverData = new byte[1024];
                    if (connectionSocket.Receive(serverData) == 0)
                    {
                        continue;
                    }
                    object serverMessage = Functions.Deserialize(serverData);
                    if (serverMessage is GetCode)
                    {
                        code = (serverMessage as GetCode).Code;
                        Dispatcher.Invoke(CreateControlsForRecover);
                        return;
                    }
                }
                catch (Exception exception)
                {
                    connectionSocket.Disconnect(false);
                    Console.WriteLine(exception.Message+"\n"+exception.StackTrace);
                    return;
                }
            }
        }

        private void CreateControlsForRecover()
        {
            Label label = new Label
            {
                Content = "Введите код:",
                Margin = new Thickness(0, 10, 10, 10)
            };

            TextBox codeBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            codeBox.KeyDown += (a, s) =>
            {
                if (s.Key == Key.Enter)
                {
                    if (String.CompareOrdinal(code, codeBox.Text) != 0)
                    {
                        MessageBox.Show(
                            "Введенный Вами код не соответствует тому, который был отправлен на Вашу электронную почту!",
                            "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    LoginTextBox.IsEnabled = false;
                    Label newPasswordTextLabel=new Label
                    {
                        Content = "Введите новый пароль:",
                        Margin = new Thickness(0, 10, 10, 10)
                    };
                    Grid.SetRow(newPasswordTextLabel, 4);
                    Grid.SetColumnSpan(newPasswordTextLabel, 2);

                    PasswordBox password1=new PasswordBox
                    {
                        HorizontalAlignment=HorizontalAlignment.Stretch,
                        VerticalAlignment =VerticalAlignment.Stretch,
                        VerticalContentAlignment=VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(10)
                    };
                    Grid.SetRow(password1, 4);
                    Grid.SetColumn(password1, 2);
                    Grid.SetColumnSpan(password1, 3);

                    Label repeatPasswordTextLabel=new Label
                    {
                        Content = "Повторите пароль:",
                        Margin = new Thickness(0, 10, 10, 10)
                    };
                    Grid.SetRow(repeatPasswordTextLabel, 5);
                    Grid.SetColumnSpan(repeatPasswordTextLabel, 2);

                    PasswordBox password2 = new PasswordBox
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(10)
                    };
                    Grid.SetRow(password2, 5);
                    Grid.SetColumn(password2, 2);
                    Grid.SetColumnSpan(password2, 3);

                    Button changePassword=new Button
                    {
                        Content="Изменить пароль",
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Margin = new Thickness(10)
                    };
                    changePassword.Click += (aS, sa) =>
                    {
                        if (password1.Password.Length == 0 || password2.Password.Length == 0)
                        {
                            MessageBox.Show("Введите данные!", "Ошибка!", MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                        else
                        {
                            if (String.CompareOrdinal(password1.Password, password2.Password) != 0)
                            {
                                MessageBox.Show("Пароли не совпадают!", "Ошибка!", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                            else
                            {
                                Functions.SerializeAndSend(new NewPassword(LoginTextBox.Text, password1.Password),
                                    connectionSocket);
                                byte[] bufferBytes=new byte[1024];
                                if (connectionSocket.Receive(bufferBytes) != 0)
                                {
                                    object answer = Functions.Deserialize(bufferBytes);
                                    if (answer is MessageFromServer)
                                        MessageBox.Show((answer as MessageFromServer).Message);
                                }
                            }
                        }
                    };
                    Grid.SetRow(changePassword, 6);
                    Grid.SetColumnSpan(changePassword, 5);

                    MainGrid.Children.Add(newPasswordTextLabel);
                    MainGrid.Children.Add(password1);
                    MainGrid.Children.Add(repeatPasswordTextLabel);
                    MainGrid.Children.Add(password2);
                    MainGrid.Children.Add(changePassword);
                }
            };

            Grid.SetRow(label, 3);
            Grid.SetColumnSpan(label, 2);

            Grid.SetRow(codeBox, 3);
            Grid.SetColumn(codeBox, 2);
            Grid.SetColumnSpan(codeBox, 3);

            
            MainGrid.Children.Add(label);
            MainGrid.Children.Add(codeBox);
        }
    }
}