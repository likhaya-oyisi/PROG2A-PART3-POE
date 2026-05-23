namespace CybersecurityChatbot.Models
{
    public class ChatUser
    {
        // Part 1 properties
        public string Name { get; set; } = string.Empty;
        public DateTime SessionStart { get; set; }
        public int MessageCount { get; set; }

        // Part 2: memory feature
        public string? FavouriteTopic { get; set; }
        private readonly Dictionary<string, string> _memory = new(StringComparer.OrdinalIgnoreCase);

        public ChatUser(string name)
        {
            Name = name;
            SessionStart = DateTime.Now;
            MessageCount = 0;
        }

        public string GetGreeting() => $"Hello, {Name}! Welcome to the Cybersecurity Awareness Bot.";

        public void IncrementMessageCount() => MessageCount++;

        public void Remember(string key, string value) => _memory[key] = value;

        public string? Recall(string key) =>
            _memory.TryGetValue(key, out string? value) ? value : null;

        public bool HasMemory => _memory.Count > 0;

        public string GetMemoryContext() =>
            FavouriteTopic != null ? $"As someone interested in {FavouriteTopic}, " : string.Empty;
    }
}
