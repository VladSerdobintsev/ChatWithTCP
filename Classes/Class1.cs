using System;
using System.Collections.Generic;

namespace Classes
{
    public class Correspondence
    {
        public readonly AccountInformation Interlocutor;
        public int NumberOfUnreadMessages;

        public Correspondence(AccountInformation interlocutor)
        {
            Interlocutor = interlocutor;
        }
        public override string ToString()
        {
            string str = Interlocutor.FirstName + " " + Interlocutor.LastName;
            if (NumberOfUnreadMessages > 0)
                str += " (" + NumberOfUnreadMessages + ")";
            return str;
        }
    }

    [Serializable]
    public class GetAllUsers
    {
        public readonly List<AccountInformation> AccountsInformation = new List<AccountInformation>();
    }

    [Serializable]
    public class NewPassword
    {
        public readonly string Email;
        public readonly string PasswordHash;

        public NewPassword(string email, string password)
        {
            Email = email;
            PasswordHash = Functions.GetHash(password);
        }
    }
    [Serializable]
    public class GetCode
    {
        public readonly string Code;
        public GetCode(string code)
        {
            Code = code;
        }
    }
    [Serializable]
    public class RecoverPasswordMessage
    {
        public readonly string Email;

        public RecoverPasswordMessage(string email)
        {
            Email = email;
        }
    }
    [Serializable]
    public abstract class MessagesAbst
    {
        protected int senderID;
        protected int recieverID;
    }
    [Serializable]
    public class GetMessages : MessagesAbst
    {
        public readonly List<Message> Messages = new List<Message>();
        public int RecieverId
        {
            get { return recieverID; }
        }
        public int SenderId
        {
            get { return senderID; }
        }
        public GetMessages(int senderId, int recieverId)
        {
            recieverID = recieverId;
            senderID = senderId;
        }
    }
    [Serializable]
    public class GetUsersOnline
    {
        public readonly List<AccountInformation> AccountsInformation = new List<AccountInformation>();
    }
    [Serializable]
    public class Message:MessagesAbst
    {
        public readonly int Id;
        public DateTime SendingDateTime;
        public bool WhetherRead;
        public int RecieverId
        {
            get { return recieverID; }
        }
        public int SenderId
        {
            get { return senderID; }
        }
        public string TextMessage { get; private set; }

        public Message(int senderId, int recieverId, string textMessage, DateTime sendingDateTime)
        {
            senderID = senderId;
            recieverID = recieverId;
            TextMessage = textMessage;
            SendingDateTime = sendingDateTime;
        }
        public Message(int messageId, int senderId, int recieverId, string textMessage, DateTime sendingDateTime, bool whetherRead)
        {
            Id = messageId;
            senderID = senderId;
            recieverID = recieverId;
            TextMessage = textMessage;
            SendingDateTime = sendingDateTime;
            WhetherRead = whetherRead;
        }
    }
    [Serializable]
    public class UserDisconnect
    {
        public readonly AccountInformation ThisAccountInforamtion;
        public UserDisconnect(AccountInformation thisAccountInforamtion)
        {
            ThisAccountInforamtion = thisAccountInforamtion;
        }
    }
    [Serializable]
    public class Authorization
    {
        public readonly string Email;
        public readonly string HashPassword;
        public Authorization(string email, string password)
        {
            Email = email;
            HashPassword = Functions.GetHash(password);
        }
    }
    [Serializable]
    public class UniquenessEmail
    {
        public readonly string Email;
        public bool YesNo;
        public UniquenessEmail(string email)
        {
            Email = email;
        }
    }
    [Serializable]
    public class SocketDisconnect
    {
        public readonly bool ReuseSocket;
        public SocketDisconnect(bool reuseSocket)
        {
            ReuseSocket = reuseSocket;
        }
    }
    [Serializable]
    public class AccountInformation
    {
        public bool IsNull;
        public readonly int Id;
        public readonly string FirstName;
        public readonly string LastName;
        public DateTime DateOfBirth;
        public readonly string Email;
        public readonly bool ManWoman;
        public readonly string HashPassword;
        public AccountInformation(string firstName, string lastName, DateTime dateOfBirth, string email, bool manWoman, string hashPassword)
        {
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = dateOfBirth;
            Email = email;
            ManWoman = manWoman;
            HashPassword = hashPassword;
        }
        public AccountInformation(int id, string firstName, string lastName, bool manWoman, DateTime dateOfBirth, string email, string hashPassword)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = dateOfBirth;
            Email = email;
            ManWoman = manWoman;
            HashPassword = hashPassword;
        }
        public AccountInformation()
        {
            IsNull = true;
        }
        public override string ToString()
        {
            return FirstName+" "+LastName;
        }
    }
    [Serializable]
    public class MessageFromServer
    {
        public readonly string Message;
        public MessageFromServer(string message)
        {
            Message = message;
        }
    }
    [Serializable]
    public class SendEmailRegistration
    {
        public string Name;
        public string Surname;
        public string Email;
        public string Subject;
        public string Body;
    }
}