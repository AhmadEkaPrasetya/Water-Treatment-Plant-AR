using System;
using System.Collections.Generic;

namespace WTP.Progress
{
    /// <summary>
    /// Semua data pemain yang disimpan ke HP (nama, bintang tiap level, badge, skor, pengaturan).
    /// Class biasa (bukan MonoBehaviour) supaya bisa diubah ke JSON oleh JsonUtility.
    /// Hanya field public / [SerializeField] yang ikut tersimpan.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public const int BelajarLevelCount = 7;
        public const int RakitLevelCount = 3;
        public const int DetektifCaseCount = 6;

        public string playerName = "";
        public bool prologueSeen;

        /// <summary>Bintang (0-3) per level Bagian 1, urutan: Intake ... Reservoir. 0 = belum selesai.</summary>
        public int[] belajarStars = new int[BelajarLevelCount];

        /// <summary>Bintang (0-3) per level Bagian 2 (Rakit WTP). 0 = belum selesai.</summary>
        public int[] rakitStars = new int[RakitLevelCount];

        /// <summary>Bintang (0-3) dan skor terbaik (0-100) per kasus Bagian 3 (Detektif). Skor dipakai leaderboard.</summary>
        public int[] detektifStars = new int[DetektifCaseCount];
        public int[] detektifScores = new int[DetektifCaseCount];

        public List<string> badges = new List<string>();
        public int bestShiftScore;

        // Pengaturan
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;

        public bool HasName => !string.IsNullOrWhiteSpace(playerName);

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (int s in belajarStars) total += s;
                return total;
            }
        }

        /// <summary>Level 1 selalu terbuka; level berikutnya terbuka kalau level sebelumnya sudah dapat bintang.</summary>
        public bool IsLevelUnlocked(int levelIndex) =>
            levelIndex == 0 || (levelIndex < BelajarLevelCount && belajarStars[levelIndex - 1] > 0);

        /// <summary>Simpan bintang hanya kalau lebih tinggi dari rekor sebelumnya.</summary>
        public void SetStars(int levelIndex, int stars)
        {
            if (stars > belajarStars[levelIndex]) belajarStars[levelIndex] = Math.Clamp(stars, 0, 3);
        }

        /// <summary>Bagian 2 terbuka setelah ke-7 level Belajar selesai.</summary>
        public bool IsBelajarComplete
        {
            get
            {
                foreach (int s in belajarStars)
                    if (s == 0) return false;
                return true;
            }
        }

        public bool IsRakitLevelUnlocked(int levelIndex) =>
            IsBelajarComplete && (levelIndex == 0 || (levelIndex < RakitLevelCount && rakitStars[levelIndex - 1] > 0));

        public bool IsRakitComplete => rakitStars[RakitLevelCount - 1] > 0;

        public void SetRakitStars(int levelIndex, int stars)
        {
            if (stars > rakitStars[levelIndex]) rakitStars[levelIndex] = Math.Clamp(stars, 0, 3);
        }

        public bool IsDetektifCaseUnlocked(int caseIndex) =>
            IsRakitComplete && (caseIndex == 0 || (caseIndex < DetektifCaseCount && detektifStars[caseIndex - 1] > 0));

        public bool IsDetektifComplete => detektifStars[DetektifCaseCount - 1] > 0;

        /// <summary>Total skor terbaik semua kasus Detektif (untuk leaderboard).</summary>
        public int DetektifTotalScore
        {
            get
            {
                int total = 0;
                foreach (int s in detektifScores) total += s;
                return total;
            }
        }

        /// <summary>Simpan bintang & skor kasus, masing-masing hanya kalau lebih tinggi dari rekor.</summary>
        public void SetDetektifResult(int caseIndex, int stars, int score)
        {
            if (stars > detektifStars[caseIndex]) detektifStars[caseIndex] = Math.Clamp(stars, 0, 3);
            if (score > detektifScores[caseIndex]) detektifScores[caseIndex] = score;
        }

        public bool AddBadge(string badgeId)
        {
            if (badges.Contains(badgeId)) return false;
            badges.Add(badgeId);
            return true;
        }

        /// <summary>
        /// Rapikan data hasil muat: file simpanan lama/rusak bisa punya array dengan panjang salah.
        /// </summary>
        public void Validate()
        {
            playerName ??= "";
            badges ??= new List<string>();
            belajarStars = FixLength(belajarStars, BelajarLevelCount);
            rakitStars = FixLength(rakitStars, RakitLevelCount);
            detektifStars = FixLength(detektifStars, DetektifCaseCount);
            detektifScores = FixLength(detektifScores, DetektifCaseCount);
        }

        // File simpanan versi lama belum punya rakitStars -> null; jadikan array kosong dengan panjang benar.
        static int[] FixLength(int[] array, int length)
        {
            if (array != null && array.Length == length) return array;
            var fixedArray = new int[length];
            if (array != null) Array.Copy(array, fixedArray, Math.Min(array.Length, length));
            return fixedArray;
        }
    }
}
