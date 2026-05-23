using System.Text.RegularExpressions;

namespace CybersecurityChatbot.Helpers
{
    public enum Intent
    {
        Unknown,
        // Task assistant intents
        AddTask,
        ShowTasks,
        CompleteTask,
        DeleteTask,
        SetReminder,
        // Quiz intents
        StartQuiz,
        // Activity log intents
        ShowActivityLog,
        ShowMoreLog,
        // Conversational intents (Part 1/2)
        Greeting,
        Help,
        Exit,
        Topic,
        FollowUp
    }

    public record NlpResult(Intent Intent, string? ExtractedText = null, DateTime? ExtractedDate = null, int? ExtractedNumber = null);

    /// <summary>
    /// Natural Language Processing engine using keyword detection,
    /// string manipulation, and regex pattern matching to interpret user input.
    /// </summary>
    public class NLPEngine
    {
        // ── Intent keyword maps (each maps to phrasings users might say) ──────
        private static readonly Dictionary<Intent, string[]> _intentKeywords = new()
        {
            {
                Intent.AddTask,
                new[] { "add task", "add a task", "new task", "create task", "create a task",
                        "remind me to", "set a reminder to", "set reminder to",
                        "i need to", "help me remember to", "can you remind me to",
                        "make a task", "add reminder" }
            },
            {
                Intent.ShowTasks,
                new[] { "show tasks", "show my tasks", "list tasks", "view tasks",
                        "what tasks", "my tasks", "see tasks", "all tasks", "show task list" }
            },
            {
                Intent.CompleteTask,
                new[] { "complete task", "mark task", "mark as complete", "finish task",
                        "task done", "completed task", "task is done", "mark complete" }
            },
            {
                Intent.DeleteTask,
                new[] { "delete task", "remove task", "cancel task", "drop task" }
            },
            {
                Intent.StartQuiz,
                new[] { "start quiz", "start the quiz", "begin quiz", "play quiz",
                        "start game", "play game", "mini game", "test me", "quiz me",
                        "cybersecurity quiz", "let's play" }
            },
            {
                Intent.ShowActivityLog,
                new[] { "show activity", "show log", "activity log", "show activity log",
                        "what have you done", "what have you done for me", "recent actions",
                        "show recent", "show history", "what did you do" }
            },
            {
                Intent.ShowMoreLog,
                new[] { "show more log", "show more", "show full log", "show all activity", "full log" }
            },
            {
                Intent.Exit,
                new[] { "bye", "exit", "quit", "goodbye", "see you", "farewell" }
            },
            {
                Intent.Help,
                new[] { "help", "menu", "what can i ask", "topics", "options",
                        "what can you do", "commands" }
            },
            {
                Intent.FollowUp,
                new[] { "another tip", "tell me more", "explain more", "more info",
                        "give me more", "continue", "go on" }
            }
        };

        // Time-related regex patterns for reminder extraction
        private static readonly Regex _inDaysPattern = new(@"in\s+(\d+)\s+days?", RegexOptions.IgnoreCase);
        private static readonly Regex _inWeeksPattern = new(@"in\s+(\d+)\s+weeks?", RegexOptions.IgnoreCase);
        private static readonly Regex _tomorrowPattern = new(@"\btomorrow\b", RegexOptions.IgnoreCase);
        private static readonly Regex _todayPattern = new(@"\btoday\b", RegexOptions.IgnoreCase);
        private static readonly Regex _nextWeekPattern = new(@"\bnext week\b", RegexOptions.IgnoreCase);

        /// <summary>
        /// Detects the user's intent from input, extracting entities where relevant.
        /// </summary>
        public NlpResult Detect(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new NlpResult(Intent.Unknown);

            string lower = input.ToLower().Trim();

            // ── Check for task management intents first (most specific) ────
            // Note: order matters - AddTask before SetReminder because "remind me to X" is also a task
            foreach (Intent intent in new[] { Intent.AddTask, Intent.ShowTasks, Intent.CompleteTask,
                                              Intent.DeleteTask, Intent.StartQuiz,
                                              Intent.ShowMoreLog, Intent.ShowActivityLog,
                                              Intent.Exit, Intent.Help, Intent.FollowUp })
            {
                if (_intentKeywords.TryGetValue(intent, out var keywords))
                {
                    foreach (string kw in keywords)
                    {
                        if (lower.Contains(kw))
                        {
                            return BuildResult(intent, input, lower);
                        }
                    }
                }
            }

            // ── Cybersecurity topic detection (delegated to ResponseEngine later) ──
            if (ContainsAny(lower, "phish", "scam", "password", "passphrase", "privacy", "popia",
                            "malware", "virus", "ransomware", "2fa", "two factor", "authenticator",
                            "social engineering", "vpn", "brows", "https", "purpose", "how are you"))
            {
                return new NlpResult(Intent.Topic);
            }

            return new NlpResult(Intent.Unknown);
        }

        // ── Build result with entity extraction based on intent ───────────────
        private NlpResult BuildResult(Intent intent, string originalInput, string lower)
        {
            switch (intent)
            {
                case Intent.AddTask:
                    {
                        string extractedTask = ExtractTaskTitle(originalInput, lower);
                        DateTime? reminderDate = ExtractReminderDate(lower);
                        return new NlpResult(intent, extractedTask, reminderDate);
                    }

                case Intent.CompleteTask:
                case Intent.DeleteTask:
                    {
                        int? taskNumber = ExtractTaskNumber(lower);
                        return new NlpResult(intent, ExtractedNumber: taskNumber);
                    }

                default:
                    return new NlpResult(intent);
            }
        }

        // ── Extract task title from natural-language input ────────────────────
        private string ExtractTaskTitle(string original, string lower)
        {
            // Strip leading command phrases like "add task -", "remind me to", etc.
            string[] prefixes = {
                "add task to", "add a task to", "add task -", "add task:",
                "add a task", "add task", "new task", "create task", "create a task",
                "remind me to", "set a reminder to", "set reminder to",
                "can you remind me to", "help me remember to", "i need to",
                "make a task", "add reminder for", "add reminder to", "add reminder"
            };

            string cleaned = original.Trim();
            string cleanedLower = lower;

            foreach (string prefix in prefixes.OrderByDescending(p => p.Length))
            {
                int idx = cleanedLower.IndexOf(prefix);
                if (idx >= 0)
                {
                    cleaned = cleaned.Substring(idx + prefix.Length).Trim();
                    break;
                }
            }

            // Remove time phrases from the task title so they don't get included
            cleaned = _inDaysPattern.Replace(cleaned, "").Trim();
            cleaned = _inWeeksPattern.Replace(cleaned, "").Trim();
            cleaned = _tomorrowPattern.Replace(cleaned, "").Trim();
            cleaned = _todayPattern.Replace(cleaned, "").Trim();
            cleaned = _nextWeekPattern.Replace(cleaned, "").Trim();

            // Clean up trailing/leading punctuation
            cleaned = cleaned.Trim('.', ',', '-', ':', ';', ' ');

            // Capitalise first letter for nicer display
            if (cleaned.Length > 0)
                cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);

            return string.IsNullOrEmpty(cleaned) ? "Untitled task" : cleaned;
        }

        // ── Extract reminder date from natural-language time expressions ──────
        public DateTime? ExtractReminderDate(string input)
        {
            string lower = input.ToLower();

            // "in X days"
            Match m = _inDaysPattern.Match(lower);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int days))
                return DateTime.Now.AddDays(days);

            // "in X weeks"
            m = _inWeeksPattern.Match(lower);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int weeks))
                return DateTime.Now.AddDays(weeks * 7);

            // "tomorrow"
            if (_tomorrowPattern.IsMatch(lower))
                return DateTime.Now.AddDays(1);

            // "today"
            if (_todayPattern.IsMatch(lower))
                return DateTime.Now;

            // "next week"
            if (_nextWeekPattern.IsMatch(lower))
                return DateTime.Now.AddDays(7);

            return null;
        }

        // ── Extract task number for complete/delete commands ──────────────────
        private int? ExtractTaskNumber(string input)
        {
            var match = Regex.Match(input, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int num))
                return num;
            return null;
        }

        private static bool ContainsAny(string source, params string[] keywords)
        {
            foreach (string k in keywords)
                if (source.Contains(k)) return true;
            return false;
        }
    }
}
