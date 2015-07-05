using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Classes;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AccountInformation AccountInformation;
        readonly Action _getUsersOnline;
        public MainWindow(Socket connection_socket, AccountInformation information)
        {
            InitializeComponent();
            _getUsersOnline = () =>
            {
                Functions.SerializeAndSend(new GetUsersOnline(), connection_socket);
                Console.WriteLine("Отправлен запрос на получение списка пользователей");
            };
            new Thread(Listen_server).Start(connection_socket);
            _writeToComboBox = new ComboBox();
            _writeToComboBox.DropDownClosed += writeToComboBox_DropDownClosed;
            Grid.SetRow(_writeToComboBox, Grid.GetRow(writeToButton));
            _writeToComboBox.Margin = new Thickness(0, 5, 5, 5);
            _writeToComboBox.Items.Add("Написать всем");
            _writeToComboBox.Items.Add("Написать девушкам");
            _writeToComboBox.Items.Add("Написать парням");
            _writeToComboBox.Items.Add("Написать группе");
            _connectionSocket = connection_socket;
            AccountInformation = information;
            Title = AccountInformation + " — Чат";
            _getUsersOnline.BeginInvoke(null, null);
        }
        void Listen_server(object socket)
        {
            Socket connectionSocket = socket as Socket;
            while (true)
            {
                try
                {
                    object serverMessage;
                    {
                        byte[] serverData = new byte[10000];
                        if (connectionSocket.Receive(serverData) == 0)
                        {
                            Console.WriteLine("00000000");
                            return;
                        }
                        serverMessage = Functions.Deserialize(serverData);
                        Console.WriteLine("Получен ответ");
                    }
                    if (serverMessage is MessageFromServer)
                    {
                        MessageFromServer message = (serverMessage as MessageFromServer);
                        if (message.Message.IndexOf("Пользователь") != -1)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (!OnlineAndAllCheckBox.IsChecked.Value)
                                    _getUsersOnline.BeginInvoke(null, null);
                            });
                        }
                        else
                        {
                            MessageBox.Show(message.Message);
                        }
                        Console.WriteLine("Пришло простое сообщение от сервера");
                    }
                    if(serverMessage is GetUsersOnline)
                    {
                        GetUsersOnline usersInform = serverMessage as GetUsersOnline;
                        Console.WriteLine("Пришёл список пользователей");
                        Dispatcher.Invoke(() =>
                        {
                            if (OnlineAndAllCheckBox.IsChecked.Value) return;
                            UsersListBox.Items.Clear();
                            foreach (AccountInformation t in usersInform.AccountsInformation)
                                UsersListBox.Items.Add(new Correspondence(t));
                        });
                    }
                    if(serverMessage is GetMessages)
                    {
                        Console.WriteLine("Пришла переписка с пользователем");
                        Dispatcher.Invoke(() =>
                        {
                            listMessagesStackPanel.Children.Clear();
                            foreach (Message message in (serverMessage as GetMessages).Messages)
                            {
                                DrawingMessage(message);
                                message.WhetherRead = true;
                                Functions.SerializeAndSend(message, _connectionSocket);
                            }
                        });
                    }
                    if (serverMessage is Message)
                    {
                        Message message = (serverMessage as Message);
                        Console.WriteLine("Сообщение: "+message.TextMessage);
                        Dispatcher.Invoke(() =>
                        {
                            Correspondence correspondence1 = UsersListBox.SelectedItem as Correspondence;
                            if (correspondence1 != null && correspondence1.Interlocutor.Id == message.SenderId)
                            {
                                DrawingMessage(message);
                                //прочитано сообщение
                            }
                            else
                            {
                                for (int index = 0; index < UsersListBox.Items.Count; index++)
                                {
                                    Correspondence correspondence = (UsersListBox.Items[index] as Correspondence);
                                    if (correspondence.Interlocutor.Id != message.SenderId) continue;
                                    correspondence.NumberOfUnreadMessages++;
                                    //usersOnlineListBox.Items.Insert(index, correspondence);
                                }
                                UsersListBox.UpdateLayout();
                            }
                        });
                    }
                    if (serverMessage is GetAllUsers)
                    {
                        GetAllUsers usersInform = (serverMessage as GetAllUsers);
                        Console.WriteLine("Пришёл список пользователей");
                        Dispatcher.Invoke(() =>
                        {
                            UsersListBox.Items.Clear();
                            foreach (AccountInformation t in usersInform.AccountsInformation)
                                UsersListBox.Items.Add(new Correspondence(t));
                        });
                    }
                    if (serverMessage is UserDisconnect)
                    {
                        UserDisconnect ud = (serverMessage as UserDisconnect);
                        Dispatcher.Invoke(() =>
                        {
                            if (!OnlineAndAllCheckBox.IsChecked.Value)
                            {
                                for (int i = 0; i < UsersListBox.Items.Count; i++)
                                {
                                    if ((UsersListBox.Items[i] as Correspondence).Interlocutor.Id ==
                                        ud.ThisAccountInforamtion.Id)
                                    {
                                        UsersListBox.Items.RemoveAt(i);
                                    }
                                }
                            }
                        });
                    }
                }
                catch(Exception ex)
                {
                    //connectionSocket.Disconnect(false);
                    Console.WriteLine(ex.Message+"\n"+ex.StackTrace);
                    return;
                }
            }
        }
        Socket _connectionSocket;
        ComboBox _writeToComboBox;
        private void SendMessageButtonClick(object sender, RoutedEventArgs e)
        {
            if (fieldTypingMessageTextBox.Text == string.Empty)
                return;
            if (UsersListBox.SelectedIndex == -1)
                return;
            Message message = new Message(AccountInformation.Id,
                (UsersListBox.SelectedItem as Correspondence).Interlocutor.Id, fieldTypingMessageTextBox.Text, DateTime.Now);
            DrawingMessage(message);
            Functions.SerializeAndSend(message, _connectionSocket);
            fieldTypingMessageTextBox.Text = String.Empty;
            fieldTypingMessageTextBox.Focus();
        }
        private void writeToButton_Click(object sender, RoutedEventArgs e)
        {
            _writeToComboBox.SelectedIndex = -1;
            writeToButton.Visibility = Visibility.Collapsed;
            mainGrid.Children.Add(_writeToComboBox);
            _writeToComboBox.IsDropDownOpen = true;
        }
        private void writeToComboBox_DropDownClosed(object sender, EventArgs e)
        {
            mainGrid.Children.Remove(_writeToComboBox);
            writeToButton.Visibility = Visibility.Visible;
        }
        private void mainWindow_Closed(object sender, EventArgs e)
        {
           Functions.SerializeAndSend(new UserDisconnect(AccountInformation), _connectionSocket);
            //connection_socket.Close();
        }
        private void fieldTypingMessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                fieldTypingMessageTextBox.Text += Environment.NewLine;
                fieldTypingMessageTextBox.SelectionStart = fieldTypingMessageTextBox.SelectionLength = fieldTypingMessageTextBox.Text.Length;
                return;
            }
            if (e.Key == Key.Enter)
                SendMessageButtonClick(null, null);
        }
        private void usersOnlineListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersListBox.SelectedIndex == -1) return;
            Functions.SerializeAndSend(
                new GetMessages(AccountInformation.Id, (UsersListBox.SelectedItem as Correspondence).Interlocutor.Id),
                _connectionSocket);
            fieldTypingMessageTextBox.Focus();
        }
        /*
        <Border Background="LightGray" BorderThickness="0" CornerRadius="10" Padding="5" Width="280" Height="Auto" HorizontalAlignment="Right" Margin="0,5,-1,0"><Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="5"/>
                        <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock FontWeight="Bold" Text="Влад"/>
                        <TextBlock Grid.Row="1" Text="wvlejnbeihnbtinbeoibnjidfnjbeineibnibnreijnribnibrneibnreibneribnebienebnibrninrernbienbeinbrinb" TextWrapping="Wrap"/>
                        <TextBlock Grid.Row="3" FontStyle="Italic" Text="cev" FontFamily="Times New Roman" FontSize="10"/>
                    </Grid>
       </Border>
         */
        void DrawingMessage(Message m)
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            //grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            TextBlock textBlock = new TextBlock { FontWeight = FontWeights.Bold };
            textBlock.Text = m.SenderId == AccountInformation.Id ? AccountInformation.ToString() : (UsersListBox.SelectedItem as Correspondence).Interlocutor.ToString();
            grid.Children.Add(textBlock);
            textBlock = new TextBlock { Text=m.TextMessage, TextWrapping=TextWrapping.Wrap };
            Grid.SetRow(textBlock, 1);
            grid.Children.Add(textBlock);
            textBlock = new TextBlock
            {
                FontStyle = FontStyles.Italic,
                FontFamily = new FontFamily("Times New Roman"),
                FontSize = 10,
                Text = m.SendingDateTime.ToShortTimeString() + ", " + m.SendingDateTime.ToShortDateString()
            };
            Grid.SetRow(textBlock, 2);
            grid.Children.Add(textBlock);
            Border b = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(5),
                Width = listMessagesStackPanel.ActualWidth / 2 - 5
            };
            if (m.SenderId == AccountInformation.Id)
            {
                b.Background = Brushes.DeepSkyBlue;
                b.HorizontalAlignment = HorizontalAlignment.Right;
                b.Margin = new Thickness(0, 5, -1, 0);
            }
            else
            {
                b.Background = Brushes.LightGray;
                b.HorizontalAlignment = HorizontalAlignment.Left;
                b.Margin = new Thickness(-1, 5, 0, 0);
            }
            b.Child = grid;
            listMessagesStackPanel.Children.Add(b);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (OnlineAndAllCheckBox.IsChecked.Value)
            {
                Functions.SerializeAndSend(new GetAllUsers(), _connectionSocket);
            }
            else
            {
                _getUsersOnline();
            }
        }
    }
}