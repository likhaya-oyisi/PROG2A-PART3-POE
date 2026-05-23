namespace CybersecurityChatbot.Helpers
{
    public enum Sentiment { Neutral, Worried, Curious, Frustrated, Confident }

    public static class SentimentDetector
    {
        private static readonly Dictionary<Sentiment, string[]> _keywords = new()
        {
            {
                Sentiment.Worried,
                new[] { "worried", "scared", "afraid", "nervous", "anxious", "concern", "fear",
                        "unsafe", "danger", "dangerous", "hacked", "breached", "stolen", "compromised" }
            },
            {
                Sentiment.Curious,
                new[] { "curious", "wonder", "how does", "tell me", "explain",
                        "learn", "understand", "interested", "want to know", "show me" }
            },
            {
                Sentiment.Frustrated,
                new[] { "frustrated", "annoyed", "confused", "don't understand", "makes no sense",
                        "complicated", "difficult", "hard", "useless", "doesn't work" }
            },
            {
                Sentiment.Confident,
                new[] { "i know", "i understand", "got it", "makes sense", "clear",
                        "thanks", "thank you", "helpful", "great", "awesome", "perfect" }
            }
        };

        public static Sentiment Detect(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Sentiment.Neutral;

            string lower = input.ToLower();
            foreach (var entry in _keywords)
                foreach (string keyword in entry.Value)
                    if (lower.Contains(keyword))
                        return entry.Key;

            return Sentiment.Neutral;
        }

        public static string GetEmpathyPrefix(Sentiment sentiment, string userName)
        {
            return sentiment switch
            {
                Sentiment.Worried =>
                    $"It's completely understandable to feel that way, {userName}. Scammers and cyber threats can be very convincing. Let me share some advice to help you stay safe.\n\n",
                Sentiment.Frustrated =>
                    $"I hear you, {userName} — this can feel overwhelming at times. Let's break it down simply together.\n\n",
                Sentiment.Curious =>
                    $"Great question, {userName}! Curiosity is the first step to staying safe online.\n\n",
                Sentiment.Confident =>
                    $"Glad to hear it, {userName}! You're building great cybersecurity habits.\n\n",
                _ => string.Empty
            };
        }
    }
}
