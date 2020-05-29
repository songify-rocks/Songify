using System;

namespace Songify.Models
{
    public enum MessageType
    {
        Normal,
        Warning,
        Error,
        Debug
    }

    class LogMessage
    {
        public MessageType MessageType { get; set; }
        public string Text { get; set; }
        public DateTime TimeStamp { get; set; }

        public LogMessage(DateTime timeStamp, MessageType messageType, string text)
        {
            TimeStamp = timeStamp;
            MessageType = messageType;
            Text = text;
        }

        public override string ToString() => $"{TimeStamp} | {MessageType}: {Text}";
    }
}
