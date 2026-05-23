namespace CybersecurityChatbot.Models
{
    public enum ActivityType { Task, Reminder, Quiz, NLP, System, Memory }

    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ActivityType Type { get; set; }
        public string Description { get; set; } = string.Empty;

        public string TimeDisplay => Timestamp.ToString("HH:mm:ss");
        public string DateDisplay => Timestamp.ToString("dd MMM");

        public string TypeLabel => Type switch
        {
            ActivityType.Task     => "TASK",
            ActivityType.Reminder => "REMINDER",
            ActivityType.Quiz     => "QUIZ",
            ActivityType.NLP      => "NLP",
            ActivityType.Memory   => "MEMORY",
            _                     => "SYSTEM"
        };
    }
}
