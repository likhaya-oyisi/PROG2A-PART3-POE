namespace CybersecurityChatbot.Models
{
    public enum QuestionType { MultipleChoice, TrueFalse }

    public class QuizQuestion
    {
        public string Question { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public List<string> Options { get; set; } = new();
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;

        public string CorrectAnswer => Options[CorrectIndex];
    }
}
