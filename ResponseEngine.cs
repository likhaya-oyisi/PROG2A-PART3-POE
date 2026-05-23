using CybersecurityChatbot.Models;

namespace CybersecurityChatbot.Helpers
{
    /// <summary>
    /// Response engine for cybersecurity topic conversations.
    /// Same structure as Part 2 - keyword matching with random responses, sentiment, memory.
    /// Used by MainWindow when NLP detects a Topic intent.
    /// </summary>
    public class ResponseEngine
    {
        private readonly Random _rng = new();
        private string? _lastTopic = null;

        public delegate string ResponseTransformer(string baseResponse, string empathyPrefix, string memoryContext);

        // Random tip pools - List<string> generic collections
        private static readonly List<string> _phishingTips = new()
        {
            "Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations.",
            "Always check the sender's email address carefully — scammers use addresses like 'support@paypa1.com' (note the '1').",
            "Legitimate banks and companies will NEVER ask for your password or PIN via email or SMS.",
            "If an email creates urgency ('Act now or your account closes!'), slow down — that's a manipulation tactic.",
            "Hover over links before clicking to see the real destination URL. If it looks odd, don't click.",
            "When in doubt, go directly to the company's official website rather than clicking any email link."
        };

        private static readonly List<string> _passwordTips = new()
        {
            "Use a passphrase like 'BlueSky!Runs42Fast' — long, memorable, and hard to crack.",
            "Never reuse passwords. If one site is breached, attackers try those credentials everywhere.",
            "A password manager like Bitwarden or 1Password generates and stores unique passwords for every site.",
            "Avoid obvious substitutions like '@' for 'a' or '3' for 'e' — crackers know all those tricks.",
            "The longer the password, the exponentially harder it is to crack. Aim for 16+ characters.",
            "Enable 2FA alongside a strong password — two layers of security are far better than one."
        };

        private static readonly List<string> _privacyTips = new()
        {
            "Review your social media privacy settings — limit who can see your posts, location, and personal info.",
            "South Africa's POPIA gives you the right to request deletion of your personal data from organisations.",
            "Be selective about which apps you grant permissions to — does that game really need your contacts?",
            "Use private/incognito mode when browsing on shared computers to avoid leaving traces.",
            "Regularly audit which third-party apps have access to your accounts (Google, Facebook, etc.).",
            "Your email address is valuable personal data — consider a separate address for online sign-ups."
        };

        private static readonly List<string> _safeBrowsingTips = new()
        {
            "Always verify the padlock icon AND the domain name before entering any personal information.",
            "Keep your browser updated — security patches close vulnerabilities that attackers actively exploit.",
            "Use a reputable ad-blocker to reduce exposure to malicious advertisements (malvertising).",
            "Avoid downloading software from unofficial sources — always go to the developer's official website.",
            "Public Wi-Fi is an attacker's playground. Use a VPN or avoid sensitive activities on public networks.",
            "Check that a website uses HTTPS — but remember, even fake sites can have HTTPS nowadays."
        };

        // Single-response dictionary (kept from Part 2)
        private static readonly Dictionary<string[], (string Response, string? Tip)> _responses = new()
        {
            {
                new[] { "how are you", "how do you do", "are you okay", "you good" },
                ("I'm fully operational and ready to help you stay safe online! Cybersecurity threats never sleep, so neither does my vigilance.", null)
            },
            {
                new[] { "purpose", "what do you do", "your role" },
                ("I'm your Cybersecurity Awareness Assistant! I can educate you about phishing, passwords, safe browsing, malware, 2FA, social engineering, privacy and VPNs. I can also manage cybersecurity tasks, run a quiz, and show you a log of our session.", null)
            },
            {
                new[] { "malware", "virus", "ransomware", "trojan", "spyware", "adware", "worm" },
                (
                    "Malware is malicious software designed to damage or gain unauthorised access to your system:\n" +
                    "  • Viruses attach to legitimate files and spread when they're opened.\n" +
                    "  • Ransomware encrypts your files and demands payment.\n" +
                    "  • Spyware secretly monitors your activity and steals data.\n" +
                    "  • Trojans disguise themselves as legitimate software.\n" +
                    "  • Keep your OS and software updated to close security gaps.",
                    "Install reputable antivirus software and back up your data frequently!"
                )
            },
            {
                new[] { "two factor", "2fa", "mfa", "multi factor", "authentication", "authenticator" },
                (
                    "Two-Factor Authentication (2FA) adds a critical second layer of security:\n" +
                    "  • Even if someone has your password, they can't access your account without the second factor.\n" +
                    "  • Options include SMS codes, authenticator apps, or hardware keys.\n" +
                    "  • Authenticator apps are more secure than SMS — SIM swapping is a real threat in South Africa.\n" +
                    "  • Enable 2FA on email, banking, and social media accounts.",
                    "Google Authenticator and Microsoft Authenticator are free — enable 2FA today!"
                )
            },
            {
                new[] { "social engineering", "manipulation", "pretexting", "baiting", "vishing", "smishing" },
                (
                    "Social engineering attacks manipulate people rather than systems:\n" +
                    "  • Attackers impersonate IT support, colleagues, or authority figures.\n" +
                    "  • Pretexting — fabricating a scenario to extract information.\n" +
                    "  • Baiting — leaving infected USB drives in public places.\n" +
                    "  • Vishing — phone calls to trick you into revealing sensitive data.\n" +
                    "  • Always verify the identity of anyone requesting sensitive information.",
                    "When in doubt, hang up and call back using an official number from the company's website."
                )
            },
            {
                new[] { "vpn", "virtual private network" },
                (
                    "A VPN (Virtual Private Network) encrypts your internet connection:\n" +
                    "  • Hides your real IP address and location from websites.\n" +
                    "  • Essential when using public Wi-Fi at cafés, airports, or malls.\n" +
                    "  • Choose a reputable paid VPN — free VPNs often sell your data.\n" +
                    "  • A VPN is one layer of protection, not full anonymity.",
                    "Look for VPNs with a strict no-logs policy and independent security audits."
                )
            }
        };

        public (string Response, string? Tip, bool ShouldExit, MessageType Type) GetResponse(string input, ChatUser user)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ("I'm not sure I understand. Can you try rephrasing? Type 'help' to see what I can do.",
                        null, false, MessageType.Normal);

            string lowerInput = input.ToLower().Trim();

            // Memory: remember favourite topic
            UpdateFavouriteTopic(lowerInput, user);

            // Sentiment-aware response transformer (delegate)
            Sentiment sentiment = SentimentDetector.Detect(lowerInput);
            string empathy = SentimentDetector.GetEmpathyPrefix(sentiment, user.Name);
            string memoryCtx = user.GetMemoryContext();

            ResponseTransformer transform = (baseResp, emp, mem) =>
            {
                if (!string.IsNullOrEmpty(emp)) return emp + baseResp;
                if (!string.IsNullOrEmpty(mem)) return mem + char.ToLower(baseResp[0]) + baseResp.Substring(1);
                return baseResp;
            };

            // Random response topics
            if (ContainsAny(lowerInput, "phishing", "phish", "scam email", "fake email", "suspicious email"))
            {
                _lastTopic = "phishing";
                return (transform(_phishingTips[_rng.Next(_phishingTips.Count)], empathy, memoryCtx), null, false, MessageType.Tip);
            }
            if (ContainsAny(lowerInput, "password", "passphrase", "credential"))
            {
                _lastTopic = "passwords";
                return (transform(_passwordTips[_rng.Next(_passwordTips.Count)], empathy, memoryCtx), null, false, MessageType.Tip);
            }
            if (ContainsAny(lowerInput, "privacy", "personal data", "popia", "personal information"))
            {
                _lastTopic = "privacy";
                return (transform(_privacyTips[_rng.Next(_privacyTips.Count)], empathy, memoryCtx), null, false, MessageType.Tip);
            }
            if (ContainsAny(lowerInput, "browsing", "safe browsing", "website", "https", "http"))
            {
                _lastTopic = "browsing";
                return (transform(_safeBrowsingTips[_rng.Next(_safeBrowsingTips.Count)], empathy, memoryCtx), null, false, MessageType.Tip);
            }

            // Dictionary lookup
            foreach (var entry in _responses)
                foreach (string keyword in entry.Key)
                    if (lowerInput.Contains(keyword))
                    {
                        _lastTopic = DetectTopic(lowerInput);
                        return (transform(entry.Value.Response, empathy, memoryCtx), entry.Value.Tip, false, MessageType.Normal);
                    }

            // Default fallback
            return (
                $"I'm not sure I understand, {user.Name}. Can you try rephrasing?\n" +
                "  Try asking about: passwords, phishing, safe browsing, malware, 2FA, privacy,\n" +
                "  or say 'add task', 'start quiz', or 'show activity log'.",
                null, false, MessageType.Normal
            );
        }

        public (string Response, string? Tip, bool ShouldExit, MessageType Type) HandleFollowUp(ChatUser user)
        {
            Sentiment sentiment = SentimentDetector.Detect("");
            string empathy = SentimentDetector.GetEmpathyPrefix(sentiment, user.Name);

            string topic = _lastTopic ?? user.FavouriteTopic ?? "phishing";
            string tip = topic switch
            {
                "phishing"  => _phishingTips[_rng.Next(_phishingTips.Count)],
                "passwords" => _passwordTips[_rng.Next(_passwordTips.Count)],
                "privacy"   => _privacyTips[_rng.Next(_privacyTips.Count)],
                "browsing"  => _safeBrowsingTips[_rng.Next(_safeBrowsingTips.Count)],
                _           => _phishingTips[_rng.Next(_phishingTips.Count)]
            };

            string prefix = string.IsNullOrEmpty(empathy)
                ? $"Here's another tip on {topic}, {user.Name}:\n\n"
                : empathy;

            return (prefix + tip, null, false, MessageType.Tip);
        }

        private static readonly Dictionary<string, string[]> _topicKeywords = new()
        {
            { "phishing",  new[] { "phish", "scam", "fake email" } },
            { "passwords", new[] { "password", "passphrase", "credential" } },
            { "privacy",   new[] { "privacy", "personal data", "popia" } },
            { "malware",   new[] { "malware", "virus", "ransomware" } },
            { "2FA",       new[] { "two factor", "2fa", "authenticator" } },
            { "browsing",  new[] { "browsing", "browser", "website", "https" } },
        };

        private void UpdateFavouriteTopic(string lower, ChatUser user)
        {
            foreach (var topicEntry in _topicKeywords)
                foreach (string kw in topicEntry.Value)
                    if (lower.Contains(kw) && user.FavouriteTopic == null)
                    {
                        user.FavouriteTopic = topicEntry.Key;
                        user.Remember("favourite_topic", topicEntry.Key);
                    }
        }

        private static string? DetectTopic(string lower)
        {
            if (lower.Contains("phish") || lower.Contains("scam")) return "phishing";
            if (lower.Contains("password")) return "passwords";
            if (lower.Contains("privacy") || lower.Contains("popia")) return "privacy";
            if (lower.Contains("brows")) return "browsing";
            if (lower.Contains("malware") || lower.Contains("virus")) return "malware";
            if (lower.Contains("2fa") || lower.Contains("two factor")) return "2FA";
            return null;
        }

        private static bool ContainsAny(string source, params string[] keywords)
        {
            foreach (string kw in keywords) if (source.Contains(kw)) return true;
            return false;
        }
    }
}
