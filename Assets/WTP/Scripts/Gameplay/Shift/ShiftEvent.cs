using UnityEngine;

namespace WTP.Gameplay.Shift
{
    /// <summary>
    /// Satu kejadian di Operator Shift (hujan abu, kabut asap, kemarau, banjir lumpur, inspeksi).
    /// Aset ada di Assets/WTP/Data/Shift. Buat baru: Create > WTP > Shift Event.
    /// Efeknya berupa pengali kondisi WTP selama 'duration' detik.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Shift Event", fileName = "Event")]
    public class ShiftEvent : ScriptableObject
    {
        public string title;
        [Tooltip("Peringatan yang muncul saat event mulai")]
        [TextArea(2, 4)] public string warning;
        public float duration = 25f;

        [Header("Efek ke WTP")]
        [Tooltip("Pengali kekeruhan air baku (3 = tiga kali lebih keruh)")]
        public float turbidityMultiplier = 1f;
        [Tooltip("Pengali kebutuhan air kota")]
        public float demandMultiplier = 1f;
        [Tooltip("Pengali kemampuan pompa intake (0.7 = debit sungai turun)")]
        public float pumpCapMultiplier = 1f;
        [Tooltip("Penurunan kepuasan warga per detik selama event")]
        public float satisfactionDrain;
        [Tooltip("Sensor kekeruhan & klorin tidak terbaca (pemain harus melihat warna air di AR)")]
        public bool hideReadings;
        [Tooltip("Kabut asap: layar & dunia AR berkabut")]
        public bool smoke;
        [Tooltip("Hujan abu: partikel abu turun di atas WTP AR")]
        public bool ash;

        [Header("Inspeksi (pilihan pemain)")]
        public bool isInspection;
        [TextArea(2, 4)] public string inspectionText;
        public string acceptLabel = "Turuti";
        public string refuseLabel = "Tolak";
        [TextArea(1, 3)] public string acceptResult;
        [TextArea(1, 3)] public string refuseResult;
        [Tooltip("Kalau dituruti: pompa mati berapa detik")]
        public float acceptPumpOffSeconds = 15f;
        public float acceptSatisfaction = -10f;
        public float refuseSatisfaction = 5f;
    }
}
