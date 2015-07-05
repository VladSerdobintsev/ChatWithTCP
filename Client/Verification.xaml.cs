using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Classes;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для Verification.xaml
    /// </summary>
    public partial class Verification : Window
    {
        public Verification(Window parent)
        {
            InitializeComponent();
            Owner = parent;
        }
        private void verificationCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (verificationCodeTextBox.Text == (Owner as Registration).Code)
            {
                label.Foreground = Brushes.Green;
                label.Content = "✔";
                Action act = () =>
                {
                    Thread.Sleep(2000);
                    Dispatcher.Invoke(Close);
                    Thread.Sleep(500);
                    Dispatcher.Invoke(() =>
                    {
                        Registration window = Owner as Registration;
                        SendEmailRegistration message = new SendEmailRegistration
                        {
                            Name = window.NameTextBox.Text,
                            Surname = window.SurnameTextBox.Text,
                            Email = window.emailTextBox.Text,
                            Subject = "Ваш аккаунт подтверждён!",
                            Body =
                                window.NameTextBox.Text + ", Вы успешно подтвердили свой аккаунт!" + Environment.NewLine +
                                "В случае потери пароля, Вы можете его восстановить, нажав на ссылку \"Забыли пароль?\" в окне авторизации."
                        };
                        Functions.SerializeAndSend(message, window.socket);
                        window.Close();
                    });
                };
                act.BeginInvoke(null, null);
            }
            else
            {
                label.Foreground = Brushes.Red;
                label.Content = "✖";
            }
        }
    }
}
