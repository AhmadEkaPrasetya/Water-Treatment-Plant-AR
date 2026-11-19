using System.Collections.Generic;
using UnityEngine;

namespace WTP.Plant
{
    /// <summary>
    /// Root WTP di dunia AR: memegang ke-7 unit (urutan aliran air) dan mengatur
    /// kejernihan air di sepanjang proses. Dipasang di prefab WTP_Plant oleh WTPPlantBuilder.
    /// </summary>
    public class PlantController : MonoBehaviour
    {
        /// <summary>
        /// Kekeruhan (NTU) air setelah keluar dari tiap unit kalau unit itu bekerja normal.
        /// Koagulasi & flokulasi belum menjernihkan (kotoran baru digumpalkan),
        /// air baru jernih setelah sedimentasi (gumpalan mengendap) dan filtrasi (disaring).
        /// </summary>
        static readonly float[] TurbidityAfterUnit = { 150f, 140f, 110f, 20f, 2f, 2f, 2f };

        [Tooltip("Urutan: Intake, Koagulasi, Flokulasi, Sedimentasi, Filtrasi, Desinfeksi, Reservoir")]
        [SerializeField] WTPUnit[] units = new WTPUnit[7];
        [Tooltip("Lampu rumah di kota, menyala sesuai jumlah unit yang berfungsi")]
        [SerializeField] CityLights cityLights;

        public IReadOnlyList<WTPUnit> Units => units;

        public WTPUnit GetUnit(UnitType type) => units[(int)type];

        /// <summary>
        /// Tampilkan hasil proses sesuai jumlah unit yang sudah berfungsi (level Belajar selesai).
        /// Unit yang belum berfungsi dianggap rusak: air yang lewat tidak dijernihkan,
        /// jadi unit-unit sesudahnya juga ikut keruh.
        /// </summary>
        public void SetWorkingUnitCount(int workingCount, bool instant = false)
        {
            float turbidity = WaterQuality.RiverTurbidity;
            for (int i = 0; i < units.Length; i++)
            {
                bool working = i < workingCount;
                if (working) turbidity = Mathf.Min(turbidity, TurbidityAfterUnit[i]);

                units[i].SetBroken(!working);
                units[i].SetClarity(new WaterQuality(turbidity).Clarity, instant);
            }
            // Reservoir terisi kalau semua unit sebelumnya bekerja.
            GetUnit(UnitType.Reservoir).SetWaterLevel(workingCount >= units.Length ? 1f : 0.15f);
            if (cityLights != null) cityLights.SetWorkingUnits(workingCount, units.Length, instant);
        }
    }
}
