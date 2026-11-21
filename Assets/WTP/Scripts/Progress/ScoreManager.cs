using UnityEngine;

namespace WTP.Progress
{
    /// <summary>
    /// Aturan skor dan bintang satu level Bagian 1 (Belajar).
    ///   Skor  = skor mini-game (0-100) + 50 per jawaban kuis benar (3 soal) -> maksimal 250
    ///   Bintang: >= 90% skor maksimal = 3, >= 60% = 2, selain itu 1.
    /// Menyelesaikan level selalu memberi minimal 1 bintang supaya level berikutnya terbuka.
    /// </summary>
    public static class ScoreManager
    {
        public const int MaxMiniGameScore = 100;
        public const int PointsPerCorrectAnswer = 50;
        public const int QuizQuestionsPerLevel = 3;
        public const int MaxLevelScore = MaxMiniGameScore + PointsPerCorrectAnswer * QuizQuestionsPerLevel;

        public static int LevelScore(int miniGameScore, int correctAnswers) =>
            Mathf.Clamp(miniGameScore, 0, MaxMiniGameScore)
            + Mathf.Clamp(correctAnswers, 0, QuizQuestionsPerLevel) * PointsPerCorrectAnswer;

        public static int StarsFor(int levelScore) => StarsForRatio((float)levelScore / MaxLevelScore);

        /// <summary>Bintang dari persentase skor (0..1). Dipakai semua bagian supaya aturannya sama.</summary>
        public static int StarsForRatio(float ratio)
        {
            if (ratio >= 0.9f) return 3;
            if (ratio >= 0.6f) return 2;
            return 1;
        }
    }
}
