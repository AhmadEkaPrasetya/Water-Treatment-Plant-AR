using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WTP.Progress
{
    /// <summary>
    /// Leaderboard LOKAL (tersimpan di HP, file leaderboard.json, terpisah dari save pemain).
    /// Setiap nama pemain punya satu baris per kategori berisi skor terbaiknya, jadi kalau
    /// beberapa orang bergantian main di HP yang sama dengan nama berbeda, semuanya masuk papan.
    /// Papan diisi beberapa tokoh bawaan (NPC) supaya tidak kosong di awal.
    /// Nanti bisa diganti layanan online tanpa mengubah pemanggilnya (Submit / GetTop).
    /// </summary>
    public static class LeaderboardManager
    {
        const string FileName = "leaderboard.json";

        static LeaderboardData data;

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        static LeaderboardData Data => data ??= Load();

        /// <summary>Catat skor. Hanya disimpan kalau lebih tinggi dari skor terbaik nama itu.</summary>
        public static void Submit(LeaderboardCategory category, string playerName, int score)
        {
            if (string.IsNullOrWhiteSpace(playerName) || score <= 0) return;
            List<LeaderboardEntry> list = Data.Get(category);
            LeaderboardEntry entry = list.Find(e => !e.npc && e.name == playerName);
            if (entry == null) list.Add(new LeaderboardEntry { name = playerName, score = score });
            else if (score > entry.score) entry.score = score;
            else return;
            Save();
        }

        /// <summary>Skor tertinggi dulu. Kalau skor sama, pemain asli di atas NPC.</summary>
        public static List<LeaderboardEntry> GetTop(LeaderboardCategory category, int count)
        {
            var sorted = new List<LeaderboardEntry>(Data.Get(category));
            sorted.Sort((a, b) => a.score != b.score ? b.score.CompareTo(a.score) : a.npc.CompareTo(b.npc));
            if (sorted.Count > count) sorted.RemoveRange(count, sorted.Count - count);
            return sorted;
        }

        static LeaderboardData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<LeaderboardData>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.detektif ??= new List<LeaderboardEntry>();
                        loaded.shift ??= new List<LeaderboardEntry>();
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Leaderboard] Gagal memuat, pakai papan baru. " + e.Message);
            }
            return CreateWithNpcs();
        }

        // Tokoh bawaan. Pak Wacana sengaja paling bawah (satire: sibuk seremoni, bukan kerja).
        static LeaderboardData CreateWithNpcs()
        {
            var d = new LeaderboardData();
            d.detektif.Add(Npc("Tirta", 520));
            d.detektif.Add(Npc("Operator Rina", 430));
            d.detektif.Add(Npc("Operator Dimas", 310));
            d.detektif.Add(Npc("Pak Wacana", 15));
            d.shift.Add(Npc("Tirta", 900));
            d.shift.Add(Npc("Operator Rina", 650));
            d.shift.Add(Npc("Operator Dimas", 400));
            d.shift.Add(Npc("Pak Wacana", 10));
            return d;
        }

        static LeaderboardEntry Npc(string name, int score) => new LeaderboardEntry { name = name, score = score, npc = true };

        static void Save()
        {
            try
            {
                string tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(Data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tempPath, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Leaderboard] Gagal menyimpan: " + e.Message);
            }
        }

        /// <summary>Hapus papan (untuk testing).</summary>
        public static void Clear()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            data = null;
        }
    }
}
