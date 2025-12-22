using System;
using System.Collections.Generic;

namespace VocabularyTestApp.Data
{
    public class Word
    {
        public int Id { get; set; }
        public string English { get; set; } = "";
        public string PartOfSpeech { get; set; } = "";
        public string Chinese { get; set; } = "";
    }

    public class TestResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Date { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public List<int> WrongWordIds { get; set; } = new();
        public List<int> TestedWordIds { get; set; } = new();
        public string Mode { get; set; } = ""; // "EnToCn" or "CnToEn"
        public string GradingMode { get; set; } = "Interactive"; // "Interactive", "Auto", "Manual"
        public string? PaperImageBase64 { get; set; }
    }

    public class UserStats
    {
        public List<TestResult> TestHistory { get; set; } = new();
        public Dictionary<int, int> WordErrorCounts { get; set; } = new();
        public Dictionary<int, int> WordCorrectCounts { get; set; } = new();
        public Dictionary<int, int> WordAttempts { get; set; } = new();
    }
}
