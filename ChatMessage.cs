namespace CybersecurityChatbot.Models
{
    public enum MessageSender { User, Bot }
    public enum MessageType { Normal, Tip, Warning, System, Task, Quiz, Log }

    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public MessageSender Sender { get; set; }
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsUser => Sender == MessageSender.User;
        public bool IsBot => Sender == MessageSender.Bot;
    }
}
