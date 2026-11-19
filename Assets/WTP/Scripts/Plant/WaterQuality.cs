using System;
using UnityEngine;

namespace WTP.Plant
{
    /// <summary>
    /// Kualitas air di satu titik: kekeruhan, pH, dan sisa klor.
    /// Batas aman mengikuti baku mutu air minum Permenkes RI No. 2 Tahun 2023.
    /// Dipakai oleh unit WTP, mini-game, Detektif, dan Operator Shift.
    /// </summary>
    [Serializable]
    public class WaterQuality
    {
        // Baku mutu air minum
        public const float MaxSafeTurbidity = 3f;   // NTU (Nephelometric Turbidity Unit)
        public const float MinSafePH = 6.5f;
        public const float MaxSafePH = 8.5f;
        public const float MinSafeChlorine = 0.2f;  // sisa klor bebas, mg/L
        public const float MaxSafeChlorine = 0.5f;

        /// <summary>Kekeruhan air sungai Tirtapura setelah tercemar abu karhutla (nilai cerita).</summary>
        public const float RiverTurbidity = 150f;

        [Tooltip("Kekeruhan dalam NTU. 0 = sangat jernih")]
        public float turbidity;
        public float pH = 7f;
        [Tooltip("Sisa klor dalam mg/L")]
        public float chlorine;

        public WaterQuality(float turbidity = RiverTurbidity, float pH = 7f, float chlorine = 0f)
        {
            this.turbidity = turbidity;
            this.pH = pH;
            this.chlorine = chlorine;
        }

        public bool IsTurbiditySafe => turbidity < MaxSafeTurbidity;
        public bool IsPHSafe => pH >= MinSafePH && pH <= MaxSafePH;
        public bool IsChlorineSafe => chlorine >= MinSafeChlorine && chlorine <= MaxSafeChlorine;
        public bool IsSafeToDrink => IsTurbiditySafe && IsPHSafe && IsChlorineSafe;

        /// <summary>
        /// Kejernihan 0..1 untuk shader air. Pakai akar kuadrat supaya air yang sudah
        /// cukup bersih (mis. 3 NTU) terlihat jernih (±0.86), bukan setengah keruh.
        /// </summary>
        public float Clarity => 1f - Mathf.Sqrt(Mathf.Clamp01(turbidity / RiverTurbidity));

        public WaterQuality Clone() => new WaterQuality(turbidity, pH, chlorine);

        public override string ToString() =>
            $"Kekeruhan {turbidity:0.#} NTU, pH {pH:0.0}, klor {chlorine:0.00} mg/L";
    }
}
