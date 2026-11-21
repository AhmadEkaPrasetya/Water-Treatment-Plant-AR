using UnityEngine;
using WTP.Story;

namespace WTP.Gameplay.Rakit
{
    /// <summary>
    /// Isi satu level Bagian 2 (Rakit WTP): anggaran, kartu yang tersedia, dan dialognya.
    /// Aset ada di Assets/WTP/Data/Rakit. Buat baru: Create > WTP > Rakit Level.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Rakit Level", fileName = "Rakit")]
    public class AssemblyLevelData : ScriptableObject
    {
        public string title;
        [Tooltip("Anggaran dalam juta rupiah (10000 = Rp 10 miliar)")]
        public int budget = 10000;
        [Tooltip("Dialog sebelum merakit")]
        public DialogueLine[] intro;
        [Tooltip("Dialog setelah WTP berhasil dirakit")]
        public DialogueLine[] outro;
        [Tooltip("Semua kartu di tangan pemain (diacak saat level mulai). Maksimal 12.")]
        public CardData[] cards;
    }
}
