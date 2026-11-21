using System;
using System.Collections.Generic;

namespace WTP.Progress
{
    /// <summary>Isi file leaderboard.json: daftar skor per kategori.</summary>
    [Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> detektif = new List<LeaderboardEntry>();
        public List<LeaderboardEntry> shift = new List<LeaderboardEntry>();

        public List<LeaderboardEntry> Get(LeaderboardCategory category) =>
            category == LeaderboardCategory.Detektif ? detektif : shift;
    }
}
