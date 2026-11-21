using System;

namespace WTP.Progress
{
    /// <summary>
    /// Aturan pemberian badge. Ada dua jenis:
    ///   - badge progres: dicek otomatis dari PlayerData lewat Evaluate() (mis. semua level Belajar selesai)
    ///   - badge kejadian: diberikan langsung lewat Award() saat sesuatu terjadi (mis. Rakit tanpa kartu jebakan)
    /// Nama & keterangan badge ada di BadgeCatalog (Assets/WTP/Data/Badges).
    /// Panggil Evaluate() sebelum SaveSystem.Save() setiap kali progres berubah.
    /// </summary>
    public static class BadgeManager
    {
        // Id badge (harus sama dengan id di BadgeCatalog).
        public const string PenjagaIntake = "penjaga_intake";
        public const string AhliKoagulasi = "ahli_koagulasi";
        public const string OperatorMuda = "operator_muda";
        public const string BintangSempurna = "bintang_sempurna";
        public const string AntiGapura = "anti_gapura";
        public const string ArsitekWTP = "arsitek_wtp";
        public const string DetektifJeli = "detektif_jeli";
        public const string DetektifHulu = "detektif_hulu";
        public const string PenyelamatTirtapura = "penyelamat_tirtapura";

        /// <summary>Dipanggil dengan id badge setiap kali pemain mendapat badge BARU (dipakai BadgeToast).</summary>
        public static event Action<string> BadgeEarned;

        /// <summary>Berikan badge. Return true kalau badge itu baru didapat.</summary>
        public static bool Award(string id)
        {
            if (!SaveSystem.Data.AddBadge(id)) return false;
            BadgeEarned?.Invoke(id);
            return true;
        }

        /// <summary>Cek semua badge progres dan berikan yang syaratnya sudah terpenuhi.</summary>
        public static void Evaluate()
        {
            PlayerData d = SaveSystem.Data;
            if (d.belajarStars[0] == 3) Award(PenjagaIntake);
            if (d.belajarStars[1] == 3) Award(AhliKoagulasi);
            if (d.IsBelajarComplete) Award(OperatorMuda);
            if (d.TotalStars == PlayerData.BelajarLevelCount * 3) Award(BintangSempurna);
            if (d.IsRakitComplete) Award(ArsitekWTP);
            if (Array.IndexOf(d.detektifScores, 100) >= 0) Award(DetektifJeli);
            if (d.IsDetektifComplete) Award(DetektifHulu);
        }
    }
}
