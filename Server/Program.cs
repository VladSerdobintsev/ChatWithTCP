using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;
using Classes;
using Authorization = Classes.Authorization;

namespace Server
{
    internal static class Program
    {
        private static readonly SqlConnection SqlConnection =
            new SqlConnection(ConfigurationManager.ConnectionStrings["MessengerDataBaseConnection"].ConnectionString);

        private static readonly ManagerAccounts ManagerAccountsOnline = new ManagerAccounts(SqlConnection);
        private static bool _whetherCanWrite = true;

        
        private static void Listen_client(object socket)
        {
            Socket connection_socket = socket as Socket;
            while (true)
            {
                try
                {
                    object clientMessage;
                    {
                        byte[] clientData = new byte[10000];
                        if (connection_socket.Receive(clientData) == 0) continue;
                        clientMessage = Functions.Deserialize(clientData);
                        //ShowMessage("Получены некие данные", ConsoleColor.Red);
                    }
                    if (clientMessage is Message)
                    {
                        Message message = (clientMessage as Message);
                        if (message.WhetherRead)
                        {
                            int idObject = ManagerAccountsOnline.GetIdObjectMessageByHashCode(message);
                            if (idObject > -1)
                            {
                                ManagerAccountsOnline.Messages[idObject]
                                    .WhetherRead = true;
                            }
                        }
                        else
                        {
                            ManagerAccountsOnline.SendMessage(message);
                        }
                    }
                    if (clientMessage is SendEmailRegistration)
                    {
                        SendEmailRegistration message = clientMessage as SendEmailRegistration;
                        SendMessageToEmail(message.Subject, message.Body, message.Email,
                            message.Name + " " + message.Surname, connection_socket);
                    }
                    if (clientMessage is UserDisconnect)
                    {
                        UserDisconnect ud = (clientMessage as UserDisconnect);
                        ManagerAccountsOnline.DeleteUser(ud.ThisAccountInforamtion);
                        connection_socket.Shutdown(SocketShutdown.Both);
                        foreach (Socket sockeT in ManagerAccountsOnline.AccountsOnline.Values)
                        {
                            Functions.SerializeAndSend(ud, sockeT);
                        }
                        return;
                    }
                    if (clientMessage is SocketDisconnect)
                    {
                        connection_socket.Disconnect((clientMessage as SocketDisconnect).ReuseSocket);
                        connection_socket.Shutdown(SocketShutdown.Both);
                        return;
                    }
                    if (clientMessage is AccountInformation)
                    {
                        AccountInformation ai = clientMessage as AccountInformation;
                        if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
                        int sex = 1;
                        if (!ai.ManWoman) sex = 0;
                        string query = "INSERT INTO Accounts VALUES ('" + ai.FirstName + "','" + ai.LastName + "'," +
                                       sex + ", '" + ai.DateOfBirth.Year + "-" + ai.DateOfBirth.Month + "-" +
                                       ai.DateOfBirth.Day + "', '" + ai.Email + "','" + ai.HashPassword + "')";
                        SqlCommand command = new SqlCommand(query, SqlConnection);
                        command.ExecuteNonQuery();
                        command.Dispose();
                        ShowMessage("В базу данных был добавлен новый аккаунт", ConsoleColor.White);
                        SqlConnection.Close();
                    }
                    if (clientMessage is Authorization)
                    {
                        //Thread.Sleep(4000);
                        Authorization authorization = clientMessage as Authorization;
                        string query = "select * from Accounts where Email='" + authorization.Email +
                                       "' and HashPassword='" + authorization.HashPassword + "'";
                        if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
                        SqlCommand command = new SqlCommand(query, SqlConnection);
                        SqlDataReader reader = command.ExecuteReader();
                        reader.Read();
                        if (reader.HasRows)
                        {
                            bool manWoman = true;
                            if (reader.GetInt32(3) == 0) manWoman = false;
                            AccountInformation ai = new AccountInformation(reader.GetInt32(0),
                                reader.GetString(1), reader.GetString(2), manWoman, reader.GetDateTime(4),
                                reader.GetString(5), reader.GetString(6));
                            Functions.SerializeAndSend(ai, connection_socket);
                            ManagerAccountsOnline.AddUser(ai, connection_socket);
                        }
                        else
                        {
                            AccountInformation accountInformation = new AccountInformation
                            {
                                IsNull = true
                            };
                            Functions.SerializeAndSend(accountInformation, connection_socket);
                        }
                        reader.Close();
                        SqlConnection.Close();
                    }
                    if (clientMessage is UniquenessEmail)
                    {
                        UniquenessEmail uniquenessEmail = (clientMessage as UniquenessEmail);
                        string query = "select Email from Accounts where Email='" + uniquenessEmail.Email + "'";
                        if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
                        SqlCommand command = new SqlCommand(query, SqlConnection);
                        SqlDataReader reader = command.ExecuteReader();
                        reader.Read();
                        uniquenessEmail.YesNo = reader.HasRows;
                        reader.Close();
                        SqlConnection.Close();
                        Functions.SerializeAndSend(uniquenessEmail, connection_socket);
                    }
                    if (clientMessage is GetUsersOnline)
                    {
                        //ShowMessage("Пришёл запрос на получение списка пользователей", ConsoleColor.Red);
                        GetUsersOnline guo = (clientMessage as GetUsersOnline);
                        foreach (AccountInformation ar in ManagerAccountsOnline.AccountsOnline.Keys)
                        {
                            guo.AccountsInformation.Add(ar);
                        }
                        Functions.SerializeAndSend(guo, connection_socket);
                    }
                    if (clientMessage is GetAllUsers)
                    {
                        GetAllUsers gao = (clientMessage as GetAllUsers);
                        string query = "select * from Accounts";
                        if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
                        SqlCommand command = new SqlCommand(query, SqlConnection);
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                bool manWoman = true;
                                if (reader.GetInt32(3) == 0) manWoman = false;
                                gao.AccountsInformation.Add(new AccountInformation(reader.GetInt32(0),
                                reader.GetString(1), reader.GetString(2), manWoman, reader.GetDateTime(4),
                                reader.GetString(5), reader.GetString(6)));
                            }
                        }
                        reader.Close();
                        SqlConnection.Close();
                        Functions.SerializeAndSend(gao, connection_socket);
                    }
                    if (clientMessage is GetMessages)
                    {
                        GetMessages gm = clientMessage as GetMessages;
                        string query = "select * from [Messages] where SenderAccountID=" + gm.SenderId +
                                       " and ReceiverAccountID=" + gm.RecieverId + " or SenderAccountID=" +
                                       gm.RecieverId + " and ReceiverAccountID=" + gm.SenderId + "  order by DateTimeSent";
                        if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
                        SqlCommand command = new SqlCommand(query, SqlConnection);
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            Message message = new Message(reader.GetInt32(0), reader.GetInt32(1),
                                reader.GetInt32(2), reader.GetString(3), reader.GetDateTime(4), reader.GetBoolean(5));
                            gm.Messages.Add(message);
                        }
                        Functions.SerializeAndSend(gm, connection_socket);
                        reader.Close();
                        SqlConnection.Close();
                    }
                    if (clientMessage is RecoverPasswordMessage)
                    {
                        RecoverPasswordMessage message = (clientMessage as RecoverPasswordMessage);
                        string query = "select * from Accounts where Email='" + message.Email + "'";
                        if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
                        SqlCommand command = new SqlCommand(query, SqlConnection);
                        SqlDataReader reader = command.ExecuteReader();
                        reader.Read();
                        if (reader.HasRows)
                        {
                            string randomCode = Functions.GetRandomCode();
                            SendMessageToEmail("Изменение пароля",
                                "Для продолжения введите в программе этот код подтверждения: " + randomCode,
                                message.Email, reader[1] + " " + reader[2], null);

                            Functions.SerializeAndSend(new GetCode(randomCode), connection_socket);
                        }
                        reader.Close();
                        SqlConnection.Close();
                    }
                    if (clientMessage is NewPassword)
                    {
                        NewPassword newPassword = (clientMessage as NewPassword);
                        string query = "update Accounts set HashPassword='" + newPassword.PasswordHash +
                                       "' where Email='" + newPassword.Email + "'";
                        if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
                        SqlCommand command = new SqlCommand(query, SqlConnection);
                        try
                        {
                            command.ExecuteNonQuery();
                            command.Dispose();
                            Functions.SerializeAndSend(new MessageFromServer("Пароль был успешно изменён!"),
                                connection_socket);
                        }
                        catch (Exception exception)
                        {
                            ShowMessage(exception.Message, ConsoleColor.Red);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    ShowMessage(ex.Message, ConsoleColor.Red);
                    connection_socket.Disconnect(false);
                    return;
                    //connection_socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.ToString(), ConsoleColor.Red);
                    return;
                }
            }
        }

        private static void SendMessageToEmail(string subject, string body, string email, string username, Socket socket)
        {
            try
            {
                MailMessage message = new MailMessage
                {
                    Body = body,
                    Subject = subject
                };
                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };
                message.From = new MailAddress("best.messenger.pro@gmail.com", "Мессенджер ПРО");
                message.To.Add(new MailAddress(email, username));
                smtp.Credentials = new NetworkCredential("best.messenger.pro@gmail.com", "bestmessenge");
                string response = string.Format("Отправлено сообщение на адрес {0}", email);
                smtp.SendCompleted += (s, d) =>
                {
                    ShowMessage(response, ConsoleColor.Green);
                    if (socket == null) return;
                    Functions.SerializeAndSend(new MessageFromServer(response), socket);
                };
                smtp.SendAsync(message, null);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, ConsoleColor.Red);
            }
        }

        public static void ShowMessage(string message, ConsoleColor backgroundColor)
        {
            SecureWriting(() =>
            {
                ConsoleColor c = Console.BackgroundColor;
                Console.BackgroundColor = backgroundColor;
                Console.WriteLine(message);
                Console.BackgroundColor = c;
            });
        }

        public static void SecureWriting(Action action)
        {
            if (action == null) return;
            do
            {
                Thread.Sleep(10);
            } while (!_whetherCanWrite);
            _whetherCanWrite = false;
            action();
            _whetherCanWrite = true;
        }

        private static void Main()
        {
            {
                bool isCreate;
                new Mutex(true, "server", out isCreate);
                if (!isCreate) return;
            }

            Console.Title = "Сервер";
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Black;

            ShowMessage("Сервер начал свою работу", ConsoleColor.Yellow);
            Socket serverConnectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverConnectionSocket.Bind(new IPEndPoint(IPAddress.Any, 9293));
            serverConnectionSocket.Listen(5);
            Action recieve = () =>
            {
                for (;;)
                {
                    Socket socket = serverConnectionSocket.Accept();
                    if (socket == null)
                    {
                        continue;
                    }
                    Thread t = new Thread(Listen_client) {IsBackground = true};
                    t.Start(socket);
                }
            };
            recieve.BeginInvoke(null, null);
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            ManagerAccountsOnline.SaveMessagesToDataBase();
            ShowMessage("Сервер завершил свою работу", ConsoleColor.Red);
            Thread.Sleep(1500);
        }
    }
}