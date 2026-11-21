using System;
using UnityEngine;
using WTP.Plant;

namespace WTP.Gameplay.Rakit
{
    /// <summary>Satu kartu di tangan pemain (disimpan di AssemblyLevelData).</summary>
    [Serializable]
    public class CardData
    {
        public string displayName;
        public CardKind kind;
        [Tooltip("Hanya dipakai kalau kind = Unit")]
        public UnitType unit;
        [Tooltip("Harga dalam juta rupiah (1000 = Rp 1 miliar)")]
        public int cost;
        [Tooltip("Keterangan saat kartu dipilih")]
        [TextArea(2, 4)] public string description;
        [Tooltip("Komentar saat kartu dipasang ke slot (mis. celetukan Pak Wacana untuk kartu jebakan). Boleh kosong.")]
        [TextArea(1, 3)] public string placedLine;
        [Tooltip("Kartu Trap/Decoy: apa yang terjadi saat air dialirkan lewat kartu ini")]
        [TextArea(1, 3)] public string simulationNote;
    }
}
