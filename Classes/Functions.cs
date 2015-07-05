using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Classes
{
    public static class Functions
    {
        public static ComboBox WorkWithEmailBox(Grid grid, TextBox emailTextBox, TextCompositionEventArgs e, int columnSpanEmailBox)
        {
            ComboBox comboBox = new ComboBox();
            const string val = "@";
            if (e.Text != val) return comboBox;
            if (emailTextBox.Text.Length < 1) return null;
            if (emailTextBox.Text.IndexOf(val, StringComparison.Ordinal) != -1) return comboBox;
            Grid.SetColumnSpan(emailTextBox, columnSpanEmailBox);
     
            comboBox.DropDownClosed += (a, r) =>
            {
                if (comboBox.SelectedIndex == -1) return;
                grid.Children.Remove(comboBox);
                Grid.SetColumnSpan(emailTextBox, columnSpanEmailBox+2);
                if (comboBox.SelectedIndex == 0)
                {
                    emailTextBox.Text += val;
                    emailTextBox.Focus();
                }
                else
                {
                    emailTextBox.Text += comboBox.SelectedItem.ToString();
                }
                emailTextBox.SelectionStart = emailTextBox.Text.Length;
                emailTextBox.Focus();
            };
            Grid.SetRow(comboBox, 2);
            comboBox.Items.Add("написать другой");
            comboBox.Items.Add(val + "gmail.com");
            comboBox.Items.Add(val + "mail.ru");
            comboBox.Items.Add(val + "yandex.ua");
            comboBox.Items.Add(val + "rambler.ru");
            comboBox.Items.Add(val + "hotmail.com");
            comboBox.Items.Add(val + "yahoo.com");
            comboBox.Items.Add(val + "inbox.ru");
            comboBox.Items.Add(val + "list.ru");
            comboBox.Items.Add(val + "bk.ru");
            comboBox.Items.Add(val + "mail.ua");
            comboBox.Height = emailTextBox.ActualHeight;
            Grid.SetColumn(comboBox, 4);
            Grid.SetColumnSpan(comboBox, 2);
            grid.Children.Add(comboBox);
            comboBox.IsDropDownOpen = true;
            comboBox.Focus();
            return comboBox;
        }
        public static Socket ReturnNewSocket(Socket thisSocket)
        {
            if (thisSocket.IsBound) return thisSocket;
            thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPHostEntry ipList = Dns.Resolve("127.0.0.1");
            IPAddress ip = ipList.AddressList[0];
            IPEndPoint endpoint = new IPEndPoint(ip, 9293);
            thisSocket.Connect(endpoint);
            return thisSocket;
        }

        public static string GetHash(string str)
        {
            StringBuilder sBuilder = new StringBuilder();
            byte[] data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(str));
            foreach (byte t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static string GetRandomCode()
        {
            string temp = Path.GetRandomFileName() + new Random().Next(100000000, 999999999) + Path.GetRandomFileName();
            return temp.Where(t => t != '.').Aggregate("", (current, t) => current + t);
        }

        public static void SerializeAndSend(object obj, Socket connectionSocket)
        {
            MemoryStream memoryStreamStream = new MemoryStream();
            new BinaryFormatter().Serialize(memoryStreamStream, obj);
            connectionSocket.Send(memoryStreamStream.ToArray());
        }

        public static object Deserialize(byte[] bytes)
        {
            return new BinaryFormatter().Deserialize(new MemoryStream(bytes));
        }
    }
}