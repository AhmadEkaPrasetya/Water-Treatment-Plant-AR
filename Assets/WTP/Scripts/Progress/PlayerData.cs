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

        public string playerName = "";

        /// <summary>Bintang (0-3) per level Bagian 1, urutan: Intake ... Reservoir. 0 = belum selesai.</summary>
        public int[] belajarStars = new int[BelajarLevelCount];

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
            if (belajarStars == null || belajarStars.Length != BelajarLevelCount)
            {
                var fixedStars = new int[BelajarLevelCount];
                if (belajarStars != null)
                    Array.Copy(belajarStars, fixedStars, Math.Min(belajarStars.Length, BelajarLevelCount));
                belajarStars = fixedStars;
            }
        }
    }
}
