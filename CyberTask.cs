namespace CybersecurityChatbot.Models
{
    public class CyberTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Computed display properties for the GUI
        public string ReminderDisplay =>
            ReminderDate.HasValue
                ? $"Reminder: {ReminderDate.Value:dd MMM yyyy}"
                : "No reminder";

        public string StatusDisplay => IsCompleted ? "Completed" : "Pending";

        public string DaysUntilDisplay
        {
            get
            {
                if (!ReminderDate.HasValue) return string.Empty;
                int days = (ReminderDate.Value.Date - DateTime.Now.Date).Days;
                if (days < 0) return $"Overdue by {-days} day(s)";
                if (days == 0) return "Due today";
                if (days == 1) return "Due tomorrow";
                return $"Due in {days} days";
            }
        }
    }
}
