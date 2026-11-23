using UnityEngine;
using WTP.Plant;
using WTP.Story;

namespace WTP.Gameplay.Detektif
{
    /// <summary>
    /// Satu kasus Bagian 3 (Detektif): laporan warga, data inspeksi ke-7 unit,
    /// unit penyebab, dan pilihan perbaikan. Aset ada di Assets/WTP/Data/Detektif.
    /// Buat baru: Create > WTP > Detektif Case.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Detektif Case", fileName = "Kasus")]
    public class CaseData : ScriptableObject
    {
        public string title;
        [Tooltip("Nama warga yang melapor")]
        public string reporter;
        [TextArea(2, 4)] public string report;

        [Tooltip("Dialog sebelum penyelidikan (boleh kosong)")]
        public DialogueLine[] intro;
        [Tooltip("Dialog setelah kasus terpecahkan")]
        public DialogueLine[] outro;

        [Header("Penyelidikan")]
        [Tooltip("Unit penyebab masalah (unit paling hulu yang datanya tidak normal)")]
        public UnitType faultyUnit;
        [Tooltip("Data ke-7 unit, urut Intake ... Reservoir")]
        public UnitReport[] units = new UnitReport[7];
        [Tooltip("Kekeruhan air (NTU) di unit penyebab dan sesudahnya, untuk warna air di AR. " +
                 "Isi 2 kalau masalahnya tidak membuat air keruh (mis. klorin berlebih).")]
        public float symptomTurbidity = 100f;
        [Tooltip("Tampilkan hujan abu karhutla di atas WTP AR (kasus yang penyebabnya dari hulu)")]
        public bool ashFall;

        [Header("Perbaikan")]
        [Tooltip("3 pilihan cara memperbaiki (diacak saat tampil)")]
        public string[] fixOptions = new string[3];
        public int correctFix;
        [Tooltip("Penjelasan Tirta setelah perbaikan benar")]
        [TextArea(2, 4)] public string fixExplanation;
    }
}
