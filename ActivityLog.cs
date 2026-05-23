using CybersecurityChatbot.Models;

namespace CybersecurityChatbot.Services
{
    /// <summary>
    /// Tracks all significant chatbot actions for the Activity Log feature.
    /// Stores entries in a List collection with timestamps.
    /// </summary>
    public class ActivityLog
    {
        private readonly List<ActivityLogEntry> _entries = new();

        public IReadOnlyList<ActivityLogEntry> AllEntries => _entries.AsReadOnly();

        public int Count => _entries.Count;

        public void Log(ActivityType type, string description)
        {
            _entries.Add(new ActivityLogEntry
            {
                Type = type,
                Description = description,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Returns the most recent entries (default 5, configurable).
        /// </summary>
        public List<ActivityLogEntry> GetRecent(int count = 5)
        {
            return _entries
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Returns all entries (used for "Show more").
        /// </summary>
        public List<ActivityLogEntry> GetAll()
        {
            return _entries
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }

        public void Clear() => _entries.Clear();
    }
}
