using UnityEngine;
using WTP.Plant;

namespace WTP.Gameplay.Shift
{
    /// <summary>
    /// Model sederhana WTP untuk Operator Shift (class biasa, tanpa tampilan). Dipanggil tiap frame lewat Step().
    ///
    /// Kendali pemain (input):
    ///   alumDose     dosis tawas (mg/L). Dosis yang pas tergantung kekeruhan air baku.
    ///   chlorineDose dosis klorin (mg/L). Sebagian habis bereaksi dengan kotoran (makin keruh, makin banyak).
    ///   pump         bukaan pompa intake 0..1. Mengisi reservoir; kota terus memakai air dari reservoir.
    ///
    /// Hasil (state):
    ///   Turbidity  kekeruhan air olahan (NTU)  -> aman kalau < 3
    ///   Chlorine   sisa klorin (mg/L)          -> aman kalau 0,2 - 0,5
    ///   Reservoir  tinggi air reservoir (%)    -> aman kalau 20 - 95
    ///   Satisfaction kepuasan warga 0..100     -> 0 = shift gagal
    /// Event mengubah kondisi lewat pengali (turbidityMultiplier, demandMultiplier, ...).
    /// </summary>
    public class ShiftSimulation
    {
        public const float BaseRawTurbidity = 150f;      // NTU, air sungai musim kemarau biasa
        public const float MinSafeReservoir = 20f;
        public const float MaxSafeReservoir = 95f;
        const float FullPumpRate = 2f;                   // % reservoir per detik saat pompa 100%
        const float BaseDemandRate = 1.2f;               // % reservoir per detik dipakai kota

        // ---- input pemain
        public float alumDose = 30f;
        public float chlorineDose = 1f;
        public float pump = 0.6f;

        // ---- pengaruh event (diatur ShiftManager tiap frame)
        public float turbidityMultiplier = 1f;
        public float demandMultiplier = 1f;
        public float pumpCapMultiplier = 1f;
        public float extraSatisfactionDrain;             // mis. kabut asap membuat warga sesak
        public bool pumpForcedOff;                       // mis. pompa dimatikan demi foto Pak Wacana

        // ---- state
        public float RawTurbidity { get; private set; } = BaseRawTurbidity;
        public float Turbidity { get; private set; } = 1f;
        public float Chlorine { get; private set; } = 0.32f;
        public float Reservoir { get; private set; } = 60f;
        public float Satisfaction { get; private set; } = 80f;

        public bool TurbiditySafe => Turbidity < WaterQuality.MaxSafeTurbidity;
        public bool ChlorineSafe => Chlorine >= WaterQuality.MinSafeChlorine && Chlorine <= WaterQuality.MaxSafeChlorine;
        public bool ReservoirSafe => Reservoir >= MinSafeReservoir && Reservoir <= MaxSafeReservoir;
        public int SafeCount => (TurbiditySafe ? 1 : 0) + (ChlorineSafe ? 1 : 0) + (ReservoirSafe ? 1 : 0);

        /// <summary>Dosis tawas yang pas untuk air baku saat ini (seperti hasil jar test).</summary>
        public float NeededAlum => 10f + RawTurbidity * 0.13f;

        /// <summary>Klorin yang habis bereaksi dengan kotoran sebelum sempat membunuh kuman.</summary>
        public float ChlorineDemand => 0.6f + Turbidity * 0.08f;

        /// <summary>Poin per detik: semua aman = 5, dua aman = 2, selain itu 0.</summary>
        public float PointsPerSecond => SafeCount == 3 ? 5f : SafeCount == 2 ? 2f : 0f;

        public void Step(float dt)
        {
            // 1. Air baku berubah pelan-pelan menuju kondisi event (abu/lumpur terbawa arus).
            float rawTarget = BaseRawTurbidity * turbidityMultiplier;
            RawTurbidity = Mathf.MoveTowards(RawTurbidity, rawTarget, 20f * dt);   // +-15 dtk sampai puncak

            // 2. Koagulasi-flokulasi-sedimentasi-filtrasi: makin jauh dosis tawas dari yang pas,
            //    makin buruk penjernihannya. Toleransi +-30% dari dosis pas masih bekerja penuh.
            float error = Mathf.Abs(alumDose - NeededAlum) / NeededAlum;
            float effectiveness = Mathf.Clamp01(1f - Mathf.Max(0f, error - 0.3f) * 1.5f);
            float turbidityTarget = 1f + (1f - effectiveness) * RawTurbidity * 0.3f;
            Turbidity = Mathf.Lerp(Turbidity, turbidityTarget, dt / 2.5f);   // butuh beberapa detik sampai terasa

            // 3. Desinfeksi: sisa klorin = dosis - yang habis bereaksi dengan kotoran.
            float chlorineTarget = Mathf.Max(0f, chlorineDose - ChlorineDemand);
            Chlorine = Mathf.Lerp(Chlorine, chlorineTarget, dt / 2f);

            // 4. Reservoir: diisi pompa, dipakai kota.
            float inflow = pumpForcedOff ? 0f : pump * FullPumpRate * pumpCapMultiplier;
            float outflow = BaseDemandRate * demandMultiplier;
            Reservoir = Mathf.Clamp(Reservoir + (inflow - outflow) * dt, 0f, 100f);

            // 5. Kepuasan warga.
            float change = SafeCount == 3 ? 1.5f : 0f;
            if (!TurbiditySafe) change -= Turbidity > 10f ? 3f : 1.5f;  // air keruh
            if (!ChlorineSafe) change -= 1f;                           // bau kaporit / ada kuman
            if (Reservoir < 15f) change -= 3f;                          // air mati di rumah warga
            else if (!ReservoirSafe && Reservoir > MaxSafeReservoir) change -= 0.5f;   // air terbuang
            change -= extraSatisfactionDrain;
            Satisfaction = Mathf.Clamp(Satisfaction + change * dt, 0f, 100f);
        }

        public void AddSatisfaction(float amount) => Satisfaction = Mathf.Clamp(Satisfaction + amount, 0f, 100f);
    }
}
