using System.Text.Json;

namespace VocabularyTestApp.Data
{
    public class WordService
    {
        private readonly string _wordsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "words.json");
        private readonly string _statsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "stats.json");
        
        public List<Word> AllWords { get; private set; } = new();
        public UserStats Stats { get; private set; } = new();

        public WordService()
        {
            LoadData();
        }

        private void LoadData()
        {
            if (File.Exists(_wordsFilePath))
            {
                var json = File.ReadAllText(_wordsFilePath);
                AllWords = JsonSerializer.Deserialize<List<Word>>(json) ?? new List<Word>();
            }

            if (File.Exists(_statsFilePath))
            {
                var json = File.ReadAllText(_statsFilePath);
                Stats = JsonSerializer.Deserialize<UserStats>(json) ?? new UserStats();
            }
        }

        public void SaveStats()
        {
            var json = JsonSerializer.Serialize(Stats, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_statsFilePath, json);
        }

        public void SaveWords()
        {
            var json = JsonSerializer.Serialize(AllWords, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_wordsFilePath, json);
        }

        public void AddOrUpdateWord(Word w)
        {
            if (w == null) return;
            if (w.Id <= 0) throw new ArgumentException("Word Id 必须为正整数");
            var existing = AllWords.FirstOrDefault(x => x.Id == w.Id);
            if (existing != null)
            {
                existing.English = w.English;
                existing.PartOfSpeech = w.PartOfSpeech;
                existing.Chinese = w.Chinese;
            }
            else
            {
                AllWords.Add(new Word
                {
                    Id = w.Id,
                    English = w.English,
                    PartOfSpeech = w.PartOfSpeech,
                    Chinese = w.Chinese
                });
            }
            SaveWords();
        }

        public void DeleteWordById(int id)
        {
            var idx = AllWords.FindIndex(x => x.Id == id);
            if (idx >= 0)
            {
                AllWords.RemoveAt(idx);
                SaveWords();
            }
        }

        public List<Word> GetWordsByRange(int startId, int endId)
        {
            return AllWords.Where(w => w.Id >= startId && w.Id <= endId).ToList();
        }

        public List<Word> GetRandomWords(int count, int? startId = null, int? endId = null)
        {
            var pool = AllWords;
            if (startId.HasValue && endId.HasValue)
            {
                pool = GetWordsByRange(startId.Value, endId.Value);
            }

            if (count >= pool.Count) return pool;

            var random = new Random();
            return pool.OrderBy(x => random.Next()).Take(count).ToList();
        }

        public void RecordTestResult(TestResult result)
        {
            Stats.TestHistory.Add(result);
            
            // If TestedWordIds is populated, we can update stats here directly if we wanted to
            // But currently TestSession calls UpdateWordStats separately.
            // We should ideally consolidate, but for now let's leave it to avoid breaking changes.
            // Wait, if I add DeleteTestResult, I need to be sure how stats were added.
            // The current flow is: TestSession calls RecordTestResult -> then calls UpdateWordStats.
            // This is fine.
        }

        public void DeleteTestResult(Guid resultId)
        {
            var result = Stats.TestHistory.FirstOrDefault(r => r.Id == resultId);
            if (result == null) return;

            Stats.TestHistory.Remove(result);
            
            // Full recalculation is safer than incremental updates
            RecalculateStats();
        }
        
        public void UpdateWordStats(List<int> testedWordIds, List<int> wrongWordIds)
        {
            foreach (var id in testedWordIds)
            {
                if (!Stats.WordAttempts.ContainsKey(id)) Stats.WordAttempts[id] = 0;
                Stats.WordAttempts[id]++;

                if (wrongWordIds.Contains(id))
                {
                    if (!Stats.WordErrorCounts.ContainsKey(id)) Stats.WordErrorCounts[id] = 0;
                    Stats.WordErrorCounts[id]++;
                }
                else
                {
                    if (!Stats.WordCorrectCounts.ContainsKey(id)) Stats.WordCorrectCounts[id] = 0;
                    Stats.WordCorrectCounts[id]++;
                }
            }
            SaveStats();
        }

        public void RecalculateStats()
        {
            Stats.WordAttempts.Clear();
            Stats.WordErrorCounts.Clear();
            Stats.WordCorrectCounts.Clear();

            foreach (var result in Stats.TestHistory)
            {
                // If we have TestedWordIds, we can fully reconstruct
                if (result.TestedWordIds != null && result.TestedWordIds.Any())
                {
                    // We don't call UpdateWordStats directly to avoid multiple saves, 
                    // but logic is same.
                    foreach (var id in result.TestedWordIds)
                    {
                        if (!Stats.WordAttempts.ContainsKey(id)) Stats.WordAttempts[id] = 0;
                        Stats.WordAttempts[id]++;

                        if (result.WrongWordIds.Contains(id))
                        {
                            if (!Stats.WordErrorCounts.ContainsKey(id)) Stats.WordErrorCounts[id] = 0;
                            Stats.WordErrorCounts[id]++;
                        }
                        else
                        {
                            if (!Stats.WordCorrectCounts.ContainsKey(id)) Stats.WordCorrectCounts[id] = 0;
                            Stats.WordCorrectCounts[id]++;
                        }
                    }
                }
                else
                {
                    // Fallback for old records
                    // We only know about WrongWordIds. We assume they were attempted.
                    foreach (var id in result.WrongWordIds)
                    {
                        if (!Stats.WordAttempts.ContainsKey(id)) Stats.WordAttempts[id] = 0;
                        Stats.WordAttempts[id]++;

                        if (!Stats.WordErrorCounts.ContainsKey(id)) Stats.WordErrorCounts[id] = 0;
                        Stats.WordErrorCounts[id]++;
                    }
                    // Correct words from old records are unfortunately lost from aggregate stats
                    // if they weren't captured in TestedWordIds.
                }
            }
            SaveStats();
        }
    }
}
