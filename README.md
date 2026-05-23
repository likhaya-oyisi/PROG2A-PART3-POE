# prog2A-POE
# Cybersecurity Awareness Chatbot — Part 3 / POE

The final part of the Cybersecurity Awareness Chatbot, integrating all features from Parts 1 and 2 with advanced functionality: **Task Assistant with MySQL database**, **Cybersecurity Quiz Mini-Game**, **NLP Simulation**, and **Activity Log**.

## Features by Part

### Carried over from Part 1
- Voice greeting on startup (System.Speech.Synthesis text-to-speech)
- ASCII art logo in the sidebar
- Personalised user interaction (asks for name)
- Cybersecurity topic responses (phishing, passwords, browsing, malware, 2FA, etc.)
- Input validation and graceful error handling

### Carried over from Part 2
- WPF Graphical User Interface with dark theme
- Keyword recognition with dictionary-based matching
- Random tips for variety (Lists of phishing/password/privacy/browsing tips)
- Conversation flow ("Give me another tip", "Tell me more")
- Memory & recall (remembers user's favourite topic)
- Sentiment detection (worried, curious, frustrated, confident)
- Delegates for response transformation

### New in Part 3
| Task | Feature | Key Files |
|---|---|---|
| **1** | Task Assistant with MySQL Database | `Database/TaskDatabase.cs`, `Models/CyberTask.cs` |
| **2** | Cybersecurity Quiz Mini-Game (13 questions, mixed types) | `Services/QuizService.cs`, `Models/QuizQuestion.cs` |
| **3** | NLP Simulation (intent detection, entity extraction) | `Helpers/NLPEngine.cs` |
| **4** | Activity Log (tracks all bot actions with timestamps) | `Services/ActivityLog.cs`, `Models/ActivityLogEntry.cs` |

## Project Structure

```
CybersecurityChatbot/
├── App.xaml / App.xaml.cs
├── CybersecurityChatbot.csproj
├── Views/
│   ├── MainWindow.xaml                 # 4-tab GUI (Chat / Tasks / Quiz / Activity Log)
│   └── MainWindow.xaml.cs              # Main controller
├── Models/
│   ├── ChatUser.cs                     # User with memory feature (Part 2)
│   ├── ChatMessage.cs                  # Message model
│   ├── CyberTask.cs                    # Task model (NEW Part 3)
│   ├── QuizQuestion.cs                 # Quiz question model (NEW Part 3)
│   └── ActivityLogEntry.cs             # Log entry model (NEW Part 3)
├── Helpers/
│   ├── ResponseEngine.cs               # Topic responses (extended)
│   ├── SentimentDetector.cs            # Part 2 sentiment analysis
│   ├── VoiceGreeting.cs                # Part 1 TTS greeting
│   └── NLPEngine.cs                    # NLP intent detection (NEW Part 3)
├── Services/
│   ├── QuizService.cs                  # Quiz state machine (NEW Part 3)
│   └── ActivityLog.cs                  # Action logger (NEW Part 3)
├── Database/
│   └── TaskDatabase.cs                 # MySQL with in-memory fallback (NEW Part 3)
└── .github/workflows/dotnet.yml
```

## How to Run

### Prerequisites
- Windows OS (WPF is Windows-only)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- (Optional) MySQL Server 8.x — see Database Setup below

### Steps
```bash
git clone <your-repo-url>
cd CybersecurityChatbot
dotnet restore
dotnet run
```

Or open `CybersecurityChatbot.csproj` in Visual Studio 2022 and press **F5**.

## Database Setup (MySQL)

The app uses MySQL to persist tasks across sessions. **If MySQL isn't installed, the app automatically falls back to in-memory storage**, so it still runs fully — tasks just won't persist between launches.

### To enable MySQL persistence:

1. Install MySQL Server 8.x and start the service.
2. The default connection string in `Database/TaskDatabase.cs` expects:
   ```
   Server=localhost; Uid=root; Pwd=root;
   ```
   If your MySQL setup differs, update the `ConnectionString` constant in that file.
3. The app **automatically creates** the `cyberbot` database and `tasks` table on first launch — no manual SQL setup required.

### Schema
```sql
CREATE TABLE tasks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    reminder_date DATETIME NULL,
    is_completed BOOLEAN DEFAULT FALSE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

The sidebar shows a database status indicator — green if connected to MySQL, yellow if using the in-memory fallback.

## Feature Walkthrough

### Task Assistant
Users can manage cybersecurity tasks through the GUI (Tasks tab) or via natural language in chat:

- `add task - Review privacy settings` → adds task with auto-generated description
- `remind me to update my password tomorrow` → adds task + reminder via NLP
- `add a task to enable 2FA in 7 days` → NLP extracts both title and timeframe
- `show my tasks` → lists all tasks
- `complete task 1` / `delete task 2` → manages tasks by number

The Tasks tab also has a manual form with title, description, and date picker.

### Cybersecurity Quiz (Mini-Game)
- 13 questions covering phishing, passwords, browsing, malware, social engineering, 2FA, POPIA
- Mixed question types: multiple-choice and true/false
- Questions shuffled each playthrough
- Immediate feedback with explanations after each answer
- Final score with grade-based feedback messages
- Access via "Quiz" tab or by saying "start quiz" in chat

### NLP Simulation
The `NLPEngine` recognises user intent even when phrased differently:

| User says | Detected intent | Extracted entities |
|---|---|---|
| "Remind me to update my password tomorrow" | AddTask | Title="Update my password", Date=tomorrow |
| "Add a task to enable 2FA in 7 days" | AddTask | Title="Enable 2FA", Date=in 7 days |
| "What have you done for me?" | ShowActivityLog | — |
| "Can you start the quiz?" | StartQuiz | — |
| "show my tasks" | ShowTasks | — |
| "complete task 2" | CompleteTask | Number=2 |

Uses keyword detection, string manipulation (`string.Contains`), and regex patterns to extract dates ("tomorrow", "in 3 days", "next week").

### Activity Log
Every significant action is logged with a timestamp:
- Tasks added, completed, deleted
- Reminders set
- Quiz started, questions answered, quiz completed
- NLP interpretations
- Memory updates

Access via the "Activity Log" tab or by saying "show activity log" / "what have you done for me?".

By default shows the last 5 entries; click "Show full log" to see all history.

## Example Interactions

```
User: "Remind me to update my password tomorrow"
CyberBot: Task added: 'Update my password'. 
         Update your password to a strong, unique passphrase.
         Reminder set for [tomorrow's date].

User: "Add a task to enable two-factor authentication"
CyberBot: Task added with the description "Enable two-factor authentication 
         for an extra layer of security." Would you like a reminder?

User: "Yes, remind me in 3 days"
CyberBot: Got it! I'll remind you in 3 days.

User: "What have you done for me?"
CyberBot: Here's a summary of recent actions:
  1. Reminder set for 'Enable two-factor authentication' on [date]
  2. Task added: 'Enable two-factor authentication'
  3. Reminder set for 'Update my password' on [date]
  4. Task added: 'Update my password'
  5. User joined the session
```

## CI Workflow

GitHub Actions runs on every push and pull request, building the WPF project on `windows-latest`.

### CI Screenshot
<!-- Add screenshot of successful GitHub Actions run here -->
![CI Build Status](ci-screenshot.png)

## Commit History (suggested - minimum 6 required)

1. `Add CyberTask, QuizQuestion, ActivityLogEntry models for Part 3`
2. `Implement TaskDatabase with MySQL integration and in-memory fallback`
3. `Build QuizService with 13 questions and scoring`
4. `Add NLPEngine for intent detection and entity extraction`
5. `Implement ActivityLog service with timestamped entries`
6. `Wire up MainWindow with 4-tab layout (Chat / Tasks / Quiz / Log)`

## Tags (minimum 3 required)

- `v1.0.0` — Part 1 release: console chatbot with voice greeting and ASCII art
- `v2.0.0` — Part 2 release: WPF GUI with sentiment detection, memory, dynamic responses
- `v3.0.0` — Part 3 (POE) release: task assistant, quiz, NLP, activity log

## References

Pieterse, H. 2021. *The Cyber Threat Landscape in South Africa: A 10-Year Review*. The African Journal of Information and Communication, 28(28). doi: https://doi.org/10.23962/10539/32213
