namespace CybersecurityChatbot.Helpers
{
    public static class VoiceGreeting
    {
        public static void PlayGreeting()
        {
            if (!OperatingSystem.IsWindows()) return;

            try { PlayTextToSpeechGreeting(); }
            catch { /* fail silently - voice greeting is enhancement */ }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void PlayTextToSpeechGreeting()
        {
            Task.Run(() =>
            {
                using var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
                synthesizer.Rate = -1;
                synthesizer.Speak("Welcome to the Cybersecurity Awareness Bot. I'm CyberBot, your personal cybersecurity assistant. I'm here to help you stay safe in the digital world.");
            });
        }
    }
}
