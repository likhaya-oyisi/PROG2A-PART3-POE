using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CybersecurityChatbot.Database;
using CybersecurityChatbot.Helpers;
using CybersecurityChatbot.Models;
using CybersecurityChatbot.Services;

namespace CybersecurityChatbot.Views
{
    public partial class MainWindow : Window
    {
        // ── Services and engines ─────────────────────────────────────────────
        private readonly ResponseEngine _responseEngine = new();
        private readonly NLPEngine _nlp = new();
        private readonly TaskDatabase _taskDb = new();
        private readonly QuizService _quiz = new();
        private readonly ActivityLog _log = new();

        // ── Session state ────────────────────────────────────────────────────
        private ChatUser? _user;
        private bool _nameCollected = false;
        private bool _awaitingReminderConfirmation = false;
        private CyberTask? _pendingTaskForReminder = null;
        private bool _showFullLog = false;

        // ── Brushes ──────────────────────────────────────────────────────────
        private static readonly SolidColorBrush BrushBgUser   = new(Color.FromRgb(0xF9, 0x4E, 0x15));
        private static readonly SolidColorBrush BrushBgBot    = new(Color.FromRgb(0x1E, 0x21, 0x22));
        private static readonly SolidColorBrush BrushBgCard   = new(Color.FromRgb(0x18, 0x1A, 0x1B));
        private static readonly SolidColorBrush BrushBgTip    = new(Color.FromRgb(0x0D, 0x1F, 0x12));
        private static readonly SolidColorBrush BrushBgTask   = new(Color.FromRgb(0x0D, 0x16, 0x1F));
        private static readonly SolidColorBrush BrushTextPrim = new(Color.FromRgb(0xFA, 0xFA, 0xF7));
        private static readonly SolidColorBrush BrushTextSec  = new(Color.FromRgb(0x8A, 0x9B, 0xA8));
        private static readonly SolidColorBrush BrushGreen    = new(Color.FromRgb(0x39, 0xD3, 0x53));
        private static readonly SolidColorBrush BrushCyan     = new(Color.FromRgb(0x00, 0xD4, 0xFF));
        private static readonly SolidColorBrush BrushYellow   = new(Color.FromRgb(0xF5, 0xC5, 0x18));
        private static readonly SolidColorBrush BrushPurple   = new(Color.FromRgb(0xA3, 0x71, 0xF7));
        private static readonly SolidColorBrush BrushRed      = new(Color.FromRgb(0xE5, 0x48, 0x4C));
        private static readonly SolidColorBrush BrushBorder   = new(Color.FromRgb(0x24, 0x27, 0x29));

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  STARTUP
        // ═══════════════════════════════════════════════════════════════════════
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Voice greeting (from Part 1)
            VoiceGreeting.PlayGreeting();

            // 2. Initialise database
            _taskDb.Initialise();
            UpdateDbStatus();
            _log.Log(ActivityType.System, $"Session started. {_taskDb.ConnectionStatus}");

            // 3. Show welcome and ask for name
            ShowWelcome();
        }

        private void ShowWelcome()
        {
            AddBotBubble(
                "Welcome! I'm CyberBot, your personal cybersecurity awareness assistant.\n" +
                "I can chat with you about cybersecurity, manage your tasks, run a quiz, and keep an activity log.",
                MessageType.System
            );

            AddBotBubble("To get started — what is your name?", MessageType.Normal);
            InputBox.Focus();
        }

        private void UpdateDbStatus()
        {
            DbStatusLabel.Text = _taskDb.ConnectionStatus;
            DbStatusDot.Fill = _taskDb.IsUsingFallback ? BrushYellow : BrushGreen;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  MAIN INPUT HANDLER
        // ═══════════════════════════════════════════════════════════════════════
        private void Send()
        {
            string input = InputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            InputBox.Clear();
            InputPlaceholder.Visibility = Visibility.Visible;

            // Phase 1: name collection
            if (!_nameCollected)
            {
                CollectName(input);
                return;
            }

            // Show user bubble
            AddUserBubble(input);
            _user!.IncrementMessageCount();
            UpdateSidebar();

            // Check for pending reminder confirmation
            if (_awaitingReminderConfirmation && _pendingTaskForReminder != null)
            {
                HandleReminderConfirmation(input);
                return;
            }

            // Sentiment detection
            Sentiment sentiment = SentimentDetector.Detect(input);
            UpdateSentiment(sentiment);

            // NLP: detect intent
            NlpResult nlp = _nlp.Detect(input);
            HandleIntent(nlp, input);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  INTENT HANDLING (NLP-driven routing)
        // ═══════════════════════════════════════════════════════════════════════
        private void HandleIntent(NlpResult nlp, string originalInput)
        {
            switch (nlp.Intent)
            {
                case Intent.AddTask:
                    HandleAddTask(nlp);
                    break;

                case Intent.ShowTasks:
                    HandleShowTasks();
                    break;

                case Intent.CompleteTask:
                    HandleCompleteTask(nlp);
                    break;

                case Intent.DeleteTask:
                    HandleDeleteTask(nlp);
                    break;

                case Intent.StartQuiz:
                    HandleStartQuiz();
                    break;

                case Intent.ShowActivityLog:
                    HandleShowActivityLog();
                    break;

                case Intent.ShowMoreLog:
                    HandleShowMoreLog();
                    break;

                case Intent.Exit:
                    HandleExit();
                    break;

                case Intent.Help:
                case Intent.Topic:
                case Intent.FollowUp:
                case Intent.Greeting:
                case Intent.Unknown:
                default:
                    // Fall through to Part 1/2 response engine
                    HandleTopicResponse(originalInput);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  TASK ASSISTANT (Task 1)
        // ═══════════════════════════════════════════════════════════════════════
        private void HandleAddTask(NlpResult nlp)
        {
            string title = nlp.ExtractedText ?? "Untitled task";

            // Generate a contextual description based on the title
            string description = GenerateTaskDescription(title);

            var task = _taskDb.AddTask(title, description, nlp.ExtractedDate);

            _log.Log(ActivityType.Task, $"Task added: '{task.Title}'" +
                (task.ReminderDate.HasValue ? $" (reminder set for {task.ReminderDate.Value:dd MMM yyyy})" : " (no reminder set)"));

            _log.Log(ActivityType.NLP, $"Recognised 'add task' intent from: \"{nlp.ExtractedText}\"");

            if (task.ReminderDate.HasValue)
            {
                AddBotBubble(
                    $"Task added: '{task.Title}'. {description}\nReminder set for {task.ReminderDate.Value:dd MMM yyyy}.",
                    MessageType.Task
                );
                _log.Log(ActivityType.Reminder, $"Reminder set for '{task.Title}' on {task.ReminderDate.Value:dd MMM yyyy}");
            }
            else
            {
                AddBotBubble(
                    $"Task added with the description \"{description}\". Would you like a reminder?",
                    MessageType.Task
                );
                _awaitingReminderConfirmation = true;
                _pendingTaskForReminder = task;
            }

            RefreshTaskList();
        }

        private void HandleReminderConfirmation(string input)
        {
            string lower = input.ToLower().Trim();

            // User says "no"
            if (lower.Contains("no") || lower.Contains("nope") || lower.Contains("skip"))
            {
                AddBotBubble("No reminder set. The task is saved without a reminder.", MessageType.Task);
                _awaitingReminderConfirmation = false;
                _pendingTaskForReminder = null;
                return;
            }

            // Try to extract a date from user's response (e.g. "yes, remind me in 3 days")
            DateTime? extractedDate = _nlp.ExtractReminderDate(input);

            if (extractedDate.HasValue && _pendingTaskForReminder != null)
            {
                _taskDb.UpdateReminder(_pendingTaskForReminder.Id, extractedDate);
                _pendingTaskForReminder.ReminderDate = extractedDate;

                int days = (extractedDate.Value.Date - DateTime.Now.Date).Days;
                string timeframe = days switch
                {
                    0 => "today",
                    1 => "tomorrow",
                    _ => $"in {days} days"
                };
                AddBotBubble($"Got it! I'll remind you {timeframe}.", MessageType.Task);

                _log.Log(ActivityType.Reminder, $"Reminder set for '{_pendingTaskForReminder.Title}' on {extractedDate.Value:dd MMM yyyy}");
                RefreshTaskList();

                _awaitingReminderConfirmation = false;
                _pendingTaskForReminder = null;
            }
            else if (lower.Contains("yes") || lower.Contains("sure") || lower.Contains("ok"))
            {
                AddBotBubble("Sure! When should I remind you? You can say things like 'tomorrow', 'in 3 days', or 'next week'.", MessageType.Task);
            }
            else
            {
                // Unrelated input - cancel pending and re-process as a fresh command
                _awaitingReminderConfirmation = false;
                _pendingTaskForReminder = null;
                NlpResult nlp = _nlp.Detect(input);
                HandleIntent(nlp, input);
            }
        }

        private void HandleShowTasks()
        {
            var tasks = _taskDb.GetAllTasks();
            _log.Log(ActivityType.Task, "Viewed task list");

            if (tasks.Count == 0)
            {
                AddBotBubble("You don't have any tasks yet. Try saying 'add task to enable 2FA' to get started.", MessageType.Task);
                return;
            }

            string message = $"You have {tasks.Count} task{(tasks.Count == 1 ? "" : "s")}:\n\n";
            int i = 1;
            foreach (var t in tasks.Take(10))
            {
                string status = t.IsCompleted ? "[completed]" : "[pending]";
                string reminder = t.ReminderDate.HasValue
                    ? $" — {t.DaysUntilDisplay}"
                    : "";
                message += $"  {i}. {t.Title} {status}{reminder}\n";
                i++;
            }
            message += "\nSwitch to the Tasks tab to manage them, or say 'complete task 1' or 'delete task 2'.";

            AddBotBubble(message, MessageType.Task);
            SwitchToTasksView();
        }

        private void HandleCompleteTask(NlpResult nlp)
        {
            var tasks = _taskDb.GetAllTasks();
            if (!nlp.ExtractedNumber.HasValue || nlp.ExtractedNumber.Value < 1 || nlp.ExtractedNumber.Value > tasks.Count)
            {
                AddBotBubble("I'm not sure which task you mean. Try 'complete task 1' (use the task number from the list).", MessageType.Task);
                return;
            }

            var task = tasks[nlp.ExtractedNumber.Value - 1];
            _taskDb.MarkComplete(task.Id);
            _log.Log(ActivityType.Task, $"Task completed: '{task.Title}'");

            AddBotBubble($"Done! Marked '{task.Title}' as complete.", MessageType.Task);
            RefreshTaskList();
        }

        private void HandleDeleteTask(NlpResult nlp)
        {
            var tasks = _taskDb.GetAllTasks();
            if (!nlp.ExtractedNumber.HasValue || nlp.ExtractedNumber.Value < 1 || nlp.ExtractedNumber.Value > tasks.Count)
            {
                AddBotBubble("I'm not sure which task you mean. Try 'delete task 1' (use the task number from the list).", MessageType.Task);
                return;
            }

            var task = tasks[nlp.ExtractedNumber.Value - 1];
            _taskDb.DeleteTask(task.Id);
            _log.Log(ActivityType.Task, $"Task deleted: '{task.Title}'");

            AddBotBubble($"Removed task: '{task.Title}'.", MessageType.Task);
            RefreshTaskList();
        }

        // Generates a brief contextual description for the task
        private string GenerateTaskDescription(string title)
        {
            string lower = title.ToLower();
            if (lower.Contains("privacy") || lower.Contains("settings"))
                return "Review account privacy settings to ensure your data is protected.";
            if (lower.Contains("password"))
                return "Update your password to a strong, unique passphrase.";
            if (lower.Contains("2fa") || lower.Contains("two-factor") || lower.Contains("two factor"))
                return "Enable two-factor authentication for an extra layer of security.";
            if (lower.Contains("update") || lower.Contains("software"))
                return "Keep software updated to patch security vulnerabilities.";
            if (lower.Contains("backup") || lower.Contains("back up"))
                return "Back up important data so you can recover from ransomware or hardware failure.";
            return $"Cybersecurity task: {title}.";
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  QUIZ MINI-GAME (Task 2)
        // ═══════════════════════════════════════════════════════════════════════
        private void HandleStartQuiz()
        {
            _log.Log(ActivityType.Quiz, "Quiz started");
            AddBotBubble("Quiz time! Switching to the Quiz tab. Good luck!", MessageType.Quiz);
            SwitchToQuizView();
            StartQuiz();
        }

        private void StartQuizButton_Click(object sender, RoutedEventArgs e)
        {
            StartQuiz();
        }

        private void StartQuiz()
        {
            _quiz.Start();
            _log.Log(ActivityType.Quiz, "Quiz started");
            QuizIntroPanel.Visibility = Visibility.Collapsed;
            QuizResultPanel.Visibility = Visibility.Collapsed;
            QuizQuestionPanel.Visibility = Visibility.Visible;
            ShowCurrentQuestion();
        }

        private void ShowCurrentQuestion()
        {
            var q = _quiz.CurrentQuestion;
            if (q == null)
            {
                ShowQuizResult();
                return;
            }

            QuizProgressLabel.Text = $"Question {_quiz.CurrentQuestionIndex + 1} of {_quiz.TotalQuestions}";
            QuizScoreLabel.Text = $"Score: {_quiz.Score}";
            QuizQuestionText.Text = q.Question;
            QuizFeedbackPanel.Visibility = Visibility.Collapsed;

            // Build option buttons dynamically
            QuizOptionsPanel.Children.Clear();
            for (int i = 0; i < q.Options.Count; i++)
            {
                int optionIdx = i;
                string letter = q.Type == QuestionType.MultipleChoice
                    ? $"{(char)('A' + i)}) "
                    : "";

                var btn = new Button
                {
                    Content = letter + q.Options[i],
                    Style = (Style)FindResource("QuizAnswerBtn"),
                    Tag = optionIdx
                };
                btn.Click += QuizOption_Click;
                QuizOptionsPanel.Children.Add(btn);
            }
        }

        private void QuizOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int optionIdx) return;

            var (isCorrect, explanation) = _quiz.Answer(optionIdx);

            // Disable all option buttons after answering
            foreach (Button optBtn in QuizOptionsPanel.Children.OfType<Button>())
                optBtn.IsEnabled = false;

            // Show feedback
            QuizFeedbackResult.Text = isCorrect ? "✓  Correct!" : "✗  Not quite.";
            QuizFeedbackResult.Foreground = isCorrect ? BrushGreen : BrushRed;
            QuizFeedbackExplanation.Text = explanation;
            QuizFeedbackPanel.Background = isCorrect ? BrushBgTip : BrushBgCard;
            QuizFeedbackPanel.Visibility = Visibility.Visible;
            QuizScoreLabel.Text = $"Score: {_quiz.Score}";

            _log.Log(ActivityType.Quiz, $"Q{_quiz.CurrentQuestionIndex + 1}: {(isCorrect ? "correct" : "incorrect")}");

            // If last question, change button to "Show results"
            if (!_quiz.HasMoreQuestions)
            {
                QuizNextButton.Content = "Show Results →";
            }
        }

        private void QuizNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_quiz.HasMoreQuestions)
            {
                _quiz.NextQuestion();
                ShowCurrentQuestion();
            }
            else
            {
                ShowQuizResult();
            }
        }

        private void ShowQuizResult()
        {
            string feedback = _quiz.GetFinalFeedback();
            QuizFinalScoreText.Text = feedback;
            QuizQuestionPanel.Visibility = Visibility.Collapsed;
            QuizResultPanel.Visibility = Visibility.Visible;
            QuizNextButton.Content = "Next Question →";

            _log.Log(ActivityType.Quiz, $"Quiz completed - {feedback}");
            AddBotBubble("Quiz complete! " + feedback, MessageType.Quiz);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  ACTIVITY LOG (Task 4)
        // ═══════════════════════════════════════════════════════════════════════
        private void HandleShowActivityLog()
        {
            _log.Log(ActivityType.NLP, "User requested activity log");
            SwitchToLogView();
            _showFullLog = false;
            RefreshLogPanel();

            var recent = _log.GetRecent(5);
            string summary = "Here's a summary of recent actions:\n";
            int i = 1;
            foreach (var entry in recent)
            {
                summary += $"  {i}. {entry.Description}\n";
                i++;
            }
            AddBotBubble(summary, MessageType.Log);
        }

        private void HandleShowMoreLog()
        {
            _showFullLog = true;
            RefreshLogPanel();
            AddBotBubble($"Showing the full activity log ({_log.Count} entries).", MessageType.Log);
        }

        private void LogShowMoreButton_Click(object sender, RoutedEventArgs e)
        {
            _showFullLog = !_showFullLog;
            LogShowMoreButton.Content = _showFullLog ? "Show last 5 only" : "Show full log";
            RefreshLogPanel();
        }

        private void RefreshLogPanel()
        {
            LogPanel.Children.Clear();
            var entries = _showFullLog ? _log.GetAll() : _log.GetRecent(5);

            LogSubtitleLabel.Text = _showFullLog
                ? $"Showing all {_log.Count} actions"
                : $"Showing the last {Math.Min(5, _log.Count)} actions";

            if (entries.Count == 0)
            {
                LogPanel.Children.Add(new TextBlock
                {
                    Text = "No activity yet.",
                    Foreground = BrushTextSec,
                    FontSize = 13,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 24, 0, 0)
                });
                return;
            }

            foreach (var entry in entries)
            {
                LogPanel.Children.Add(BuildLogCard(entry));
            }
        }

        private Border BuildLogCard(ActivityLogEntry entry)
        {
            SolidColorBrush typeBrush = entry.Type switch
            {
                ActivityType.Task     => BrushCyan,
                ActivityType.Reminder => BrushYellow,
                ActivityType.Quiz     => BrushPurple,
                ActivityType.NLP      => BrushGreen,
                ActivityType.Memory   => BrushYellow,
                _                     => BrushTextSec
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Time
            grid.Children.Add(new TextBlock
            {
                Text = entry.TimeDisplay,
                Foreground = BrushTextSec,
                FontSize = 12,
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center
            });

            // Type pill
            var pill = new Border
            {
                Background = BrushBgCard,
                BorderBrush = typeBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = entry.TypeLabel,
                    Foreground = typeBrush,
                    FontSize = 9,
                    FontWeight = FontWeights.SemiBold
                }
            };
            Grid.SetColumn(pill, 1);
            grid.Children.Add(pill);

            // Description
            var desc = new TextBlock
            {
                Text = entry.Description,
                Foreground = BrushTextPrim,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(desc, 2);
            grid.Children.Add(desc);

            return new Border
            {
                Background = BrushBgCard,
                BorderBrush = BrushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 6),
                Child = grid
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  TOPIC RESPONSES (Part 1/2 fallback)
        // ═══════════════════════════════════════════════════════════════════════
        private void HandleTopicResponse(string input)
        {
            // Follow-up triggers route to a topic-specific tip
            string lower = input.ToLower();
            bool isFollowUp = lower.Contains("another tip") || lower.Contains("tell me more") ||
                              lower.Contains("explain more") || lower.Contains("more info") ||
                              lower.Contains("continue") || lower.Contains("go on");

            (string response, string? tip, bool shouldExit, MessageType type) result;

            if (isFollowUp)
                result = _responseEngine.HandleFollowUp(_user!);
            else
                result = _responseEngine.GetResponse(input, _user!);

            AddBotBubble(result.response, result.type);
            if (result.tip != null)
                AddBotBubble("TIP: " + result.tip, MessageType.Tip);

            // Update memory display
            if (_user!.FavouriteTopic != null)
            {
                MemoryPanel.Visibility = Visibility.Visible;
                MemoryTopicLabel.Text = $"Interested in: {_user.FavouriteTopic}";
                _log.Log(ActivityType.Memory, $"Remembered interest: {_user.FavouriteTopic}");
            }

            if (result.shouldExit) HandleExit();
        }

        private void HandleExit()
        {
            AddBotBubble(
                $"Stay safe online, {_user?.Name}! Remember — cybersecurity is a habit, not a one-time action. Goodbye!",
                MessageType.System
            );
            InputBox.IsEnabled = false;
            SendButton.IsEnabled = false;
            InputPlaceholder.Text = "Session ended. Thank you for chatting!";
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  NAME COLLECTION
        // ═══════════════════════════════════════════════════════════════════════
        private void CollectName(string input)
        {
            string name = System.Globalization.CultureInfo.CurrentCulture
                              .TextInfo.ToTitleCase(input.ToLower().Trim());

            if (name.Length < 2)
            {
                AddBotBubble("Please enter a valid name (at least 2 characters).", MessageType.Normal);
                return;
            }

            _user = new ChatUser(name);
            _nameCollected = true;
            SidebarUserName.Text = _user.Name;
            UpdateSidebar();

            InputPlaceholder.Text = "Ask me anything, or try 'add task', 'start quiz', 'show activity log'...";

            AddBotBubble(_user.GetGreeting(), MessageType.System);
            AddBotBubble(
                "Here's what I can do:\n" +
                "  • Chat about cybersecurity topics (phishing, passwords, malware, etc.)\n" +
                "  • Manage your cybersecurity tasks (try 'add task to enable 2FA')\n" +
                "  • Run a 13-question cybersecurity quiz (say 'start quiz')\n" +
                "  • Show what I've done for you (say 'show activity log')",
                MessageType.Normal
            );
            AddBotBubble("Type 'bye' when you're ready to leave.", MessageType.Normal);

            _log.Log(ActivityType.System, $"User '{_user.Name}' joined the session");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  TASK LIST UI (Tasks tab)
        // ═══════════════════════════════════════════════════════════════════════
        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a task title.", "Missing title", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string description = TaskDescriptionInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
                description = GenerateTaskDescription(title);

            DateTime? reminder = TaskReminderPicker.SelectedDate;
            var task = _taskDb.AddTask(title, description, reminder);

            _log.Log(ActivityType.Task, $"Task added via form: '{task.Title}'");
            if (reminder.HasValue)
                _log.Log(ActivityType.Reminder, $"Reminder set for '{task.Title}' on {reminder.Value:dd MMM yyyy}");

            // Clear form
            TaskTitleInput.Clear();
            TaskDescriptionInput.Clear();
            TaskReminderPicker.SelectedDate = null;

            RefreshTaskList();
        }

        private void RefreshTaskList()
        {
            TaskListPanel.Children.Clear();
            var tasks = _taskDb.GetAllTasks();

            EmptyTasksLabel.Visibility = tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var task in tasks)
            {
                TaskListPanel.Children.Add(BuildTaskCard(task));
            }
        }

        private Border BuildTaskCard(CyberTask task)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left content - title, description, reminder
            var content = new StackPanel();

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            var titleText = new TextBlock
            {
                Text = task.Title,
                Foreground = BrushTextPrim,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null
            };
            titleRow.Children.Add(titleText);

            if (task.IsCompleted)
            {
                titleRow.Children.Add(new Border
                {
                    Background = BrushBgCard,
                    BorderBrush = BrushGreen,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "DONE",
                        Foreground = BrushGreen,
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold
                    }
                });
            }

            content.Children.Add(titleRow);

            content.Children.Add(new TextBlock
            {
                Text = task.Description,
                Foreground = BrushTextSec,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });

            if (task.ReminderDate.HasValue)
            {
                content.Children.Add(new TextBlock
                {
                    Text = $"{task.ReminderDisplay}  •  {task.DaysUntilDisplay}",
                    Foreground = BrushYellow,
                    FontSize = 11,
                    Margin = new Thickness(0, 6, 0, 0)
                });
            }
            grid.Children.Add(content);

            // Right action buttons
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (!task.IsCompleted)
            {
                var completeBtn = new Button
                {
                    Content = "Complete",
                    Style = (Style)FindResource("ActionBtn"),
                    Tag = task.Id
                };
                completeBtn.Click += (s, e) =>
                {
                    _taskDb.MarkComplete(task.Id);
                    _log.Log(ActivityType.Task, $"Task completed: '{task.Title}'");
                    RefreshTaskList();
                };
                actions.Children.Add(completeBtn);
            }

            var deleteBtn = new Button
            {
                Content = "Delete",
                Style = (Style)FindResource("ActionBtn"),
                Tag = task.Id
            };
            deleteBtn.Click += (s, e) =>
            {
                _taskDb.DeleteTask(task.Id);
                _log.Log(ActivityType.Task, $"Task deleted: '{task.Title}'");
                RefreshTaskList();
            };
            actions.Children.Add(deleteBtn);

            Grid.SetColumn(actions, 1);
            grid.Children.Add(actions);

            return new Border
            {
                Background = BrushBgCard,
                BorderBrush = BrushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                Child = grid
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ═══════════════════════════════════════════════════════════════════════
        private void TabChat_Click(object sender, RoutedEventArgs e) => SwitchToChatView();
        private void TabTasks_Click(object sender, RoutedEventArgs e) { SwitchToTasksView(); RefreshTaskList(); }
        private void TabQuiz_Click(object sender, RoutedEventArgs e) => SwitchToQuizView();
        private void TabLog_Click(object sender, RoutedEventArgs e) { SwitchToLogView(); RefreshLogPanel(); }

        private void SwitchToChatView()
        {
            ChatView.Visibility = Visibility.Visible;
            TasksView.Visibility = Visibility.Collapsed;
            QuizView.Visibility = Visibility.Collapsed;
            LogView.Visibility = Visibility.Collapsed;
            ActivateTab(TabChat);
        }

        private void SwitchToTasksView()
        {
            ChatView.Visibility = Visibility.Collapsed;
            TasksView.Visibility = Visibility.Visible;
            QuizView.Visibility = Visibility.Collapsed;
            LogView.Visibility = Visibility.Collapsed;
            ActivateTab(TabTasks);
            RefreshTaskList();
        }

        private void SwitchToQuizView()
        {
            ChatView.Visibility = Visibility.Collapsed;
            TasksView.Visibility = Visibility.Collapsed;
            QuizView.Visibility = Visibility.Visible;
            LogView.Visibility = Visibility.Collapsed;
            ActivateTab(TabQuiz);
        }

        private void SwitchToLogView()
        {
            ChatView.Visibility = Visibility.Collapsed;
            TasksView.Visibility = Visibility.Collapsed;
            QuizView.Visibility = Visibility.Collapsed;
            LogView.Visibility = Visibility.Visible;
            ActivateTab(TabLog);
            RefreshLogPanel();
        }

        private void ActivateTab(Button activeTab)
        {
            Style normal = (Style)FindResource("TabBtn");
            Style active = (Style)FindResource("TabBtnActive");

            TabChat.Style = (TabChat == activeTab) ? active : normal;
            TabTasks.Style = (TabTasks == activeTab) ? active : normal;
            TabQuiz.Style = (TabQuiz == activeTab) ? active : normal;
            TabLog.Style = (TabLog == activeTab) ? active : normal;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  CHAT BUBBLE RENDERING
        // ═══════════════════════════════════════════════════════════════════════
        private void AddUserBubble(string text)
        {
            var container = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            var bubble = new Border
            {
                Background = BrushBgUser,
                CornerRadius = new CornerRadius(16, 4, 16, 16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 520,
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = BrushTextPrim,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                }
            };
            container.Children.Add(bubble);
            AnimateFadeIn(bubble);
            ChatPanel.Children.Add(container);
            ScrollToBottom();
        }

        private void AddBotBubble(string text, MessageType type)
        {
            SolidColorBrush bgBrush = type switch
            {
                MessageType.Tip    => BrushBgTip,
                MessageType.System => BrushBgCard,
                MessageType.Task   => BrushBgTask,
                MessageType.Quiz   => BrushBgCard,
                MessageType.Log    => BrushBgCard,
                _                  => BrushBgBot
            };

            SolidColorBrush borderBrush = type switch
            {
                MessageType.Tip    => BrushGreen,
                MessageType.System => BrushCyan,
                MessageType.Task   => BrushCyan,
                MessageType.Quiz   => BrushPurple,
                MessageType.Log    => BrushYellow,
                _                  => BrushBorder
            };

            string label = type switch
            {
                MessageType.Tip    => "CyberBot · Tip",
                MessageType.System => "CyberBot · System",
                MessageType.Task   => "CyberBot · Task Assistant",
                MessageType.Quiz   => "CyberBot · Quiz",
                MessageType.Log    => "CyberBot · Activity Log",
                _                  => "CyberBot"
            };

            var header = new TextBlock
            {
                Text = label,
                Foreground = BrushTextSec,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var bubble = new Border
            {
                Background = bgBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 640,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = BrushTextPrim,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                }
            };

            var wrapper = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            wrapper.Children.Add(header);
            wrapper.Children.Add(bubble);

            var container = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            container.Children.Add(wrapper);

            AnimateFadeIn(wrapper);
            ChatPanel.Children.Add(container);
            ScrollToBottom();
        }

        private static void AnimateFadeIn(UIElement element)
        {
            element.Opacity = 0;
            var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            var transform = new TranslateTransform(0, 10);
            element.RenderTransform = transform;
            var slideAnim = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(250));
            element.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            transform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        private void ScrollToBottom()
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                ChatScrollViewer?.ScrollToEnd();
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SIDEBAR UPDATES
        // ═══════════════════════════════════════════════════════════════════════
        private void UpdateSidebar()
        {
            if (_user == null) return;
            SidebarMessageCount.Text = $"{_user.MessageCount} message{(_user.MessageCount == 1 ? "" : "s")} sent";

            if (_user.FavouriteTopic != null)
            {
                MemoryPanel.Visibility = Visibility.Visible;
                MemoryTopicLabel.Text = $"Interested in: {_user.FavouriteTopic}";
            }
        }

        private void UpdateSentiment(Sentiment sentiment)
        {
            SentimentLabel.Text = sentiment switch
            {
                Sentiment.Worried    => "Worried / Anxious",
                Sentiment.Curious    => "Curious / Engaged",
                Sentiment.Frustrated => "Frustrated",
                Sentiment.Confident  => "Confident",
                _                    => "Neutral"
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════════════
        private void SendButton_Click(object sender, RoutedEventArgs e) => Send();

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Send();
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InputPlaceholder.Visibility = string.IsNullOrEmpty(InputBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void QuickTopic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string query && _nameCollected)
            {
                // Make sure we're on the chat view for these quick actions
                SwitchToChatView();
                InputBox.Text = query;
                Send();
            }
        }
    }
}
