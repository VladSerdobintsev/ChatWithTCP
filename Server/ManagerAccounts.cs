using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using Classes;

namespace Server
{
    class ManagerAccounts
    {
        readonly Dictionary<AccountInformation, Socket> _accountsOnline = new Dictionary<AccountInformation, Socket>();
        readonly List<Message> _messages = new List<Message>();
        public List<Message> Messages
        {
            get { return _messages; }
        }

        readonly SqlConnection _sqlConnection;
        public ManagerAccounts(SqlConnection connection)
        {
            _sqlConnection = connection;
        }
        public Dictionary<AccountInformation, Socket> AccountsOnline
        {
            get { return _accountsOnline; }
        }
        public void SendMessage(Message m)
        {
            if (m.SenderId != m.RecieverId)
            {
                AccountInformation account = GetAccountById(m.RecieverId);
                if (account != null)
                {
                    Functions.SerializeAndSend(m, AccountsOnline[account]);
                    m.SendingDateTime = DateTime.Now;
                    Messages.Add(m);
                }
                else
                {
                    SaveMessage(m);
                }
            }

            if (Messages.Count > 5 * _accountsOnline.Count)
                SaveMessagesToDataBase();
        }
        AccountInformation GetAccountById(int id)
        {
            foreach (AccountInformation accountInformation in AccountsOnline.Keys)
            {
                if (accountInformation.Id == id)
                    return accountInformation;
            }
            return null;
        }

        /// <summary>
        /// Функция поиска сообщения в коллекции сообщений
        /// </summary>
        /// <param name="message">искомое сообщение</param>
        /// <returns>Возвращает позицию сообщения в коллекции. В случае неудачи — возвращает значение -1</returns>
        public int GetIdObjectMessageByHashCode(Message message)
        {
            for (int i = 0; i < Messages.Count; i++)
            {
                if (Messages[i].GetHashCode() != message.GetHashCode()) continue;
                if (Messages[i].Id != message.Id) continue;
                if (Messages[i].SenderId != message.SenderId) continue;
                if (Messages[i].RecieverId != message.RecieverId) continue;
                if (String.CompareOrdinal(Messages[i].TextMessage, message.TextMessage) != 0) continue;
                if (DateTime.Compare(Messages[i].SendingDateTime, message.SendingDateTime) != 0) continue;
                return i;
            }
            return -1;
        }

        public void SaveMessagesToDataBase()
        {
            if (_messages.Count == 0) return;
            if (_sqlConnection.State == ConnectionState.Closed) _sqlConnection.Open();
            foreach (Message message in Messages)
            {
                if (message.Id != 0) continue;
                SaveMessage(message);
            }
            _sqlConnection.Close();
            Program.ShowMessage("В базу данных были сохранены сообщения",
                ConsoleColor.Cyan);
            _messages.Clear();
        }

        void SaveMessage(Message message)
        {
            String query = "INSERT INTO [Messages] VALUES (" + message.SenderId + ", " + message.RecieverId +
                               ",'" + message.TextMessage + "', dbo.make_date(" + message.SendingDateTime.Day + ", " +
                               message.SendingDateTime.Month + ", " + message.SendingDateTime.Year + ", " +
                               message.SendingDateTime.Hour + ", " + message.SendingDateTime.Minute + "), ";
            if (message.WhetherRead)
            {
                query += "1";
            }
            else query += "0";
            query += ")";
            if (_sqlConnection.State == ConnectionState.Closed) _sqlConnection.Open();
            SqlCommand command = new SqlCommand(query, _sqlConnection);
            command.ExecuteNonQuery();
        }
        public void AddUser(AccountInformation accountInformation, Socket socket)
        {
            if (!_accountsOnline.ContainsKey(accountInformation))
            {

                foreach (Socket sockeT in _accountsOnline.Values)
                {
                    Functions.SerializeAndSend(
                        new MessageFromServer("Пользователь " + accountInformation.FirstName + " " +
                                              accountInformation.LastName + " зашёл в чат"), sockeT);
                }
                _accountsOnline.Add(accountInformation, socket);
                Program.SecureWriting(() =>
                {
                    ConsoleColor colorTemp = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.Write("Пользователь");
                    Console.BackgroundColor = colorTemp;
                    Console.Write(" ");
                    Console.BackgroundColor = ConsoleColor.Gray;
                    ConsoleColor tmpForeg = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(accountInformation.FirstName);
                    Console.BackgroundColor = colorTemp;
                    Console.Write(" ");
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.Write(accountInformation.LastName);
                    Console.ForegroundColor = tmpForeg;
                    Console.BackgroundColor = colorTemp;
                    Console.Write(" ");
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.WriteLine("зашёл в чат");
                    Console.BackgroundColor = colorTemp;
                });
            }
        }
        AccountInformation SearchAccount(AccountInformation ai)
        {
            return AccountsOnline.Keys.FirstOrDefault(acInf => String.CompareOrdinal(acInf.Email, ai.Email) == 0);
        }

        public void DeleteUser(AccountInformation accountInformation)
        {
            AccountInformation account = SearchAccount(accountInformation);
            if (account != null)
            {
                _accountsOnline[account].Disconnect(false);
                Program.SecureWriting(() =>
                {
                    ConsoleColor colorTemp = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write("Пользователь");
                    Console.BackgroundColor = colorTemp;
                    Console.Write(" ");
                    Console.BackgroundColor = ConsoleColor.Gray;
                    ConsoleColor tmpForeg = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(accountInformation.FirstName);
                    Console.BackgroundColor = colorTemp;
                    Console.Write(" ");
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.Write(accountInformation.LastName);
                    Console.ForegroundColor = tmpForeg;
                    Console.BackgroundColor = colorTemp;
                    Console.Write(" ");
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("вышел из чата");
                    Console.BackgroundColor = colorTemp;
                });
                _accountsOnline.Remove(account);
            }
        }
    }
}