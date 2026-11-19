using System;
using System.IO;
using UnityEngine;

namespace WTP.Progress
{
    /// <summary>
    /// Simpan / muat PlayerData sebagai file JSON di penyimpanan internal HP
    /// (Application.persistentDataPath, tidak hilang saat game ditutup).
    ///
    /// Pemakaian:
    ///   SaveSystem.Data.playerName = "Budi";
    ///   SaveSystem.Save();
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";

        static PlayerData data;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Data pemain saat ini. Otomatis dimuat dari file saat pertama kali dipakai.</summary>
        public static PlayerData Data => data ??= Load();

        public static PlayerData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<PlayerData>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.Validate();
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                // File rusak: jangan bikin game crash, mulai dari data baru saja.
                Debug.LogWarning("[SaveSystem] Gagal memuat simpanan, pakai data baru. " + e.Message);
            }
            return new PlayerData();
        }

        public static void Save()
        {
            try
            {
                // Tulis ke file sementara dulu, baru ganti file asli. Kalau HP mati di tengah
                // penulisan, file simpanan lama tetap utuh.
                string tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(Data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tempPath, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] Gagal menyimpan: " + e.Message);
            }
        }

        /// <summary>Hapus semua progres (untuk tombol reset / testing).</summary>
        public static void DeleteSave()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            data = new PlayerData();
        }
    }
}
