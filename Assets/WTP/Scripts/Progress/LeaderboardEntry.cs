using System;

namespace WTP.Progress
{
    /// <summary>Satu baris papan peringkat.</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
        public bool npc;   // tokoh bawaan game (Tirta, Pak Wacana, ...) supaya papan tidak kosong
    }
}
