using CybersecurityChatbot.Models;

namespace CybersecurityChatbot.Services
{
    /// <summary>
    /// Manages the cybersecurity quiz mini-game state and questions.
    /// </summary>
    public class QuizService
    {
        // 12 questions covering phishing, passwords, browsing, social engineering, malware, 2FA
        private static readonly List<QuizQuestion> _questions = new()
        {
            new QuizQuestion
            {
                Question = "What should you do if you receive an email asking for your password?",
                Type = QuestionType.MultipleChoice,
                Options = new List<string> { "Reply with your password", "Delete the email", "Report the email as phishing", "Ignore it" },
                CorrectIndex = 2,
                Explanation = "Correct! Reporting phishing emails helps prevent scams and protects others from being targeted."
            },
            new QuizQuestion
            {
                Question = "True or False: Using the same password across multiple sites is safe if the password is strong.",
                Type = QuestionType.TrueFalse,
                Options = new List<string> { "True", "False" },
                CorrectIndex = 1,
                Explanation = "False. If one site is breached, attackers will try your credentials on other popular sites — this is called credential stuffing."
            },
            new QuizQuestion
            {
                Question = "Which of these is the strongest password?",
                Type = QuestionType.MultipleChoice,
                Options = new List<string> { "password123", "P@ssw0rd!", "BlueSky!Runs42Fast", "qwerty" },
                CorrectIndex = 2,
                Explanation = "Long passphrases like 'BlueSky!Runs42Fast' are the strongest — length matters more than complexity, and they're easier to remember."
            },
            new QuizQuestion
            {
                Question = "True or False: A padlock icon in your browser means the website is 100% safe.",
                Type = QuestionType.TrueFalse,
                Options = new List<string> { "True", "False" },
                CorrectIndex = 1,
                Explanation = "False. The padlock only means the connection is encrypted. Fake phishing sites can also have HTTPS — always verify the domain too."
            },
            new QuizQuestion
            {
                Question = "What is two-factor authentication (2FA)?",
                Type = QuestionType.MultipleChoice,
                Options = new List<string>
                {
                    "Using two passwords",
                    "An extra verification step beyond your password",
                    "Logging in from two devices",
                    "Two security questions"
                },
                CorrectIndex = 1,
                Explanation = "Correct! 2FA adds a second layer such as a code from your phone, making accounts much harder to break into."
            },
            new QuizQuestion
            {
                Question = "True or False: Public Wi-Fi is safe to use for online banking.",
                Type = QuestionType.TrueFalse,
                Options = new List<string> { "True", "False" },
                CorrectIndex = 1,
                Explanation = "False. Public Wi-Fi can be intercepted by attackers. Always use a VPN or mobile data for sensitive activities."
            },
            new QuizQuestion
            {
                Question = "What is 'social engineering' in cybersecurity?",
                Type = QuestionType.MultipleChoice,
                Options = new List<string>
                {
                    "Hacking through code vulnerabilities",
                    "Manipulating people into revealing information",
                    "Network engineering for social media",
                    "Building secure systems"
                },
                CorrectIndex = 1,
                Explanation = "Correct! Social engineering exploits human psychology rather than technical flaws — phishing, vishing, and pretexting are common forms."
            },
            new QuizQuestion
            {
                Question = "What is ransomware?",
                Type = QuestionType.MultipleChoice,
                Options = new List<string>
                {
                    "A type of antivirus software",
                    "Malware that encrypts your files and demands payment",
                    "A secure backup system",
                    "A type of firewall"
                },
                CorrectIndex = 1,
                Explanation = "Correct! Ransomware locks your files and demands payment. Always keep offline backups so you never have to pay."
            },
            new QuizQuestion
            {
                Question = "True or False: You should click links in unexpected emails to verify if they're legitimate.",
                Type = QuestionType.TrueFalse,
                Options = new List<string> { "True", "False" },
                CorrectIndex = 1,
                Explanation = "False. Never click suspicious links — go directly to the official website by typing the URL yourself."
            },
            new QuizQuestion
            {
                Question = "What does POPIA stand for?",
                Type = QuestionType.MultipleChoice,
                Options = new List<string>
                {
                    "Public Online Privacy Information Act",
                    "Protection of Personal Information Act",
                    "Personal Online Protection and Information Authority",
                    "Privacy of Public Information Agreement"
                },
                CorrectIndex = 1,
                Explanation = "Correct! POPIA is South Africa's Protection of Personal Information Act, protecting your data rights."
            },
            new QuizQuestion
            {
                Question = "True or False: It's safe to download software from any website if it's free.",
                Type = QuestionType.TrueFalse,
                Options = new List<string> { "True", "False" },
                CorrectIndex = 1,
                Explanation = "False. Always download software from official sources — unofficial downloads often contain malware bundled in."
            },
            new QuizQuestion
            {
                Question = "Which is a sign of a phishing email?",
                Type = QuestionType.MultipleChoice,
                Options = new List<string>
                {
                    "Personalised greeting using your real name",
                    "Sender's email matches the official domain",
                    "Urgent language and threats of account closure",
                    "Professional formatting and grammar"
                },
                CorrectIndex = 2,
                Explanation = "Correct! Urgency is a classic phishing tactic designed to make you act before you think. Always pause and verify."
            },
            new QuizQuestion
            {
                Question = "True or False: SIM swapping is a real threat where attackers take over your phone number.",
                Type = QuestionType.TrueFalse,
                Options = new List<string> { "True", "False" },
                CorrectIndex = 0,
                Explanation = "True. SIM swapping is common in South Africa. This is why authenticator apps are more secure than SMS for 2FA."
            }
        };

        public int CurrentQuestionIndex { get; private set; }
        public int Score { get; private set; }
        public bool IsActive { get; private set; }
        public int TotalQuestions => _questions.Count;

        private List<QuizQuestion> _shuffledQuestions = new();

        public void Start()
        {
            IsActive = true;
            Score = 0;
            CurrentQuestionIndex = 0;
            // Shuffle questions for variety on each playthrough
            var rng = new Random();
            _shuffledQuestions = _questions.OrderBy(_ => rng.Next()).ToList();
        }

        public QuizQuestion? CurrentQuestion =>
            (IsActive && CurrentQuestionIndex < _shuffledQuestions.Count)
                ? _shuffledQuestions[CurrentQuestionIndex]
                : null;

        public bool HasMoreQuestions => CurrentQuestionIndex < _shuffledQuestions.Count - 1;

        /// <summary>
        /// Answers the current question. Returns (isCorrect, explanation).
        /// </summary>
        public (bool IsCorrect, string Explanation) Answer(int optionIndex)
        {
            if (CurrentQuestion == null)
                return (false, "No active question.");

            bool isCorrect = optionIndex == CurrentQuestion.CorrectIndex;
            string explanation = isCorrect
                ? CurrentQuestion.Explanation
                : $"Not quite. The correct answer was: '{CurrentQuestion.CorrectAnswer}'. {CurrentQuestion.Explanation}";

            if (isCorrect) Score++;
            return (isCorrect, explanation);
        }

        public void NextQuestion() => CurrentQuestionIndex++;

        public string GetFinalFeedback()
        {
            double percent = (double)Score / _shuffledQuestions.Count;
            string grade = percent switch
            {
                >= 0.9 => $"Great job, you're a cybersecurity pro! You scored {Score}/{_shuffledQuestions.Count}.",
                >= 0.7 => $"Well done! You scored {Score}/{_shuffledQuestions.Count}. You have solid cybersecurity awareness.",
                >= 0.5 => $"Not bad! You scored {Score}/{_shuffledQuestions.Count}. Keep learning to sharpen your skills.",
                _      => $"You scored {Score}/{_shuffledQuestions.Count}. Keep learning to stay safe online!"
            };
            IsActive = false;
            return grade;
        }

        public void Stop()
        {
            IsActive = false;
            CurrentQuestionIndex = 0;
            Score = 0;
        }
    }
}
