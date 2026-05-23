using CybersecurityChatbot.Models;
using MySql.Data.MySqlClient;

namespace CybersecurityChatbot.Database
{
    /// <summary>
    /// MySQL database layer for task storage.
    /// Falls back to in-memory storage if MySQL is unavailable, so the app
    /// always runs even when MySQL Server isn't installed locally.
    /// </summary>
    public class TaskDatabase
    {
        // Connection string - update these for your local MySQL setup
        private const string ConnectionString =
            "Server=localhost;Database=cyberbot;Uid=root;Pwd=root;SslMode=none;";

        private readonly List<CyberTask> _inMemoryTasks = new();
        private int _nextInMemoryId = 1;
        private bool _useFallback = false;

        public bool IsUsingFallback => _useFallback;
        public string ConnectionStatus { get; private set; } = "Not initialised";

        // ── Initialisation ────────────────────────────────────────────────────
        public void Initialise()
        {
            try
            {
                EnsureDatabaseAndTable();
                ConnectionStatus = "Connected to MySQL";
                _useFallback = false;
            }
            catch (Exception ex)
            {
                _useFallback = true;
                ConnectionStatus = $"Using in-memory storage (MySQL not available: {ex.Message.Split('\n')[0]})";
            }
        }

        private void EnsureDatabaseAndTable()
        {
            // Create the database if it doesn't exist
            string serverOnlyConn = "Server=localhost;Uid=root;Pwd=root;SslMode=none;";
            using (var conn = new MySqlConnection(serverOnlyConn))
            {
                conn.Open();
                using var cmd = new MySqlCommand("CREATE DATABASE IF NOT EXISTS cyberbot;", conn);
                cmd.ExecuteNonQuery();
            }

            // Create the tasks table if it doesn't exist
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string createSql = @"
                    CREATE TABLE IF NOT EXISTS tasks (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        title VARCHAR(255) NOT NULL,
                        description TEXT,
                        reminder_date DATETIME NULL,
                        is_completed BOOLEAN DEFAULT FALSE,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );";
                using var cmd = new MySqlCommand(createSql, conn);
                cmd.ExecuteNonQuery();
            }
        }

        // ── CRUD operations ───────────────────────────────────────────────────
        public CyberTask AddTask(string title, string description, DateTime? reminderDate)
        {
            var task = new CyberTask
            {
                Title = title,
                Description = description,
                ReminderDate = reminderDate,
                IsCompleted = false,
                CreatedAt = DateTime.Now
            };

            if (_useFallback)
            {
                task.Id = _nextInMemoryId++;
                _inMemoryTasks.Add(task);
                return task;
            }

            try
            {
                using var conn = new MySqlConnection(ConnectionString);
                conn.Open();
                string sql = @"INSERT INTO tasks (title, description, reminder_date, is_completed, created_at)
                               VALUES (@title, @description, @reminder, @completed, @created);
                               SELECT LAST_INSERT_ID();";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@reminder", (object?)reminderDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@completed", false);
                cmd.Parameters.AddWithValue("@created", task.CreatedAt);

                task.Id = Convert.ToInt32(cmd.ExecuteScalar());
                return task;
            }
            catch
            {
                // If DB fails mid-session, fallback gracefully
                _useFallback = true;
                task.Id = _nextInMemoryId++;
                _inMemoryTasks.Add(task);
                return task;
            }
        }

        public List<CyberTask> GetAllTasks()
        {
            if (_useFallback)
                return _inMemoryTasks.OrderByDescending(t => t.CreatedAt).ToList();

            try
            {
                var tasks = new List<CyberTask>();
                using var conn = new MySqlConnection(ConnectionString);
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT id, title, description, reminder_date, is_completed, created_at FROM tasks ORDER BY created_at DESC;",
                    conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tasks.Add(new CyberTask
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        ReminderDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                        IsCompleted = reader.GetBoolean(4),
                        CreatedAt = reader.GetDateTime(5)
                    });
                }
                return tasks;
            }
            catch
            {
                _useFallback = true;
                return _inMemoryTasks.OrderByDescending(t => t.CreatedAt).ToList();
            }
        }

        public bool MarkComplete(int taskId)
        {
            if (_useFallback)
            {
                var task = _inMemoryTasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return false;
                task.IsCompleted = true;
                return true;
            }

            try
            {
                using var conn = new MySqlConnection(ConnectionString);
                conn.Open();
                using var cmd = new MySqlCommand("UPDATE tasks SET is_completed = TRUE WHERE id = @id;", conn);
                cmd.Parameters.AddWithValue("@id", taskId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                _useFallback = true;
                return false;
            }
        }

        public bool DeleteTask(int taskId)
        {
            if (_useFallback)
            {
                var task = _inMemoryTasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return false;
                _inMemoryTasks.Remove(task);
                return true;
            }

            try
            {
                using var conn = new MySqlConnection(ConnectionString);
                conn.Open();
                using var cmd = new MySqlCommand("DELETE FROM tasks WHERE id = @id;", conn);
                cmd.Parameters.AddWithValue("@id", taskId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                _useFallback = true;
                return false;
            }
        }

        public bool UpdateReminder(int taskId, DateTime? reminderDate)
        {
            if (_useFallback)
            {
                var task = _inMemoryTasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return false;
                task.ReminderDate = reminderDate;
                return true;
            }

            try
            {
                using var conn = new MySqlConnection(ConnectionString);
                conn.Open();
                using var cmd = new MySqlCommand("UPDATE tasks SET reminder_date = @reminder WHERE id = @id;", conn);
                cmd.Parameters.AddWithValue("@reminder", (object?)reminderDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", taskId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                _useFallback = true;
                return false;
            }
        }
    }
}
