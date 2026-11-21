using System;
using UnityEngine;

namespace WTP.Gameplay.Detektif
{
    /// <summary>Satu baris data hasil inspeksi, mis. "Dosis tawas: 0 mg/L (normal: 20-40 mg/L)".</summary>
    [Serializable]
    public class Measurement
    {
        public string label;
        public string value;
        [Tooltip("Batas normal yang ditampilkan ke pemain sebagai pembanding")]
        public string normalRange;
        [Tooltip("Centang kalau nilai ini di luar batas normal. Tidak ditampilkan ke pemain; " +
                 "dipakai untuk petunjuk Tirta saat pemain salah menuduh.")]
        public bool abnormal;
    }
}
