using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Plant;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Mini-game Desinfeksi: atur dosis klorin dengan slider selama 20 detik.
    /// Sebagian klorin "terpakai" untuk membunuh kuman (kebutuhan klorin), dan kebutuhan ini
    /// naik-turun terus. Yang diukur adalah SISA klorin = dosis - kebutuhan.
    /// Target: sisa klorin 0.2-0.5 mg/L (baku mutu Permenkes 2/2023, lihat WaterQuality).
    /// Skor = lama sisa klorin berada di zona hijau (cukup 90% waktu untuk nilai 100).
    /// </summary>
    public class DisinfectionGame : MiniGame
    {
        [SerializeField] Slider doseSlider;          // 0..1.6 mg/L
        [SerializeField] TMP_Text doseText;
        [SerializeField] RectTransform gaugeFill;    // lebar = sisa klorin / gaugeMax
        [SerializeField] Image gaugeFillImage;
        [SerializeField] TMP_Text residualText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text timerText;

        [SerializeField] float duration = 20f;
        [Tooltip("Batas kanan pengukur (mg/L)")]
        [SerializeField] float gaugeMax = 1f;

        [SerializeField] Color lowColor = new Color(0.85f, 0.3f, 0.25f);
        [SerializeField] Color goodColor = new Color(0.25f, 0.75f, 0.35f);
        [SerializeField] Color highColor = new Color(0.95f, 0.75f, 0.2f);

        float elapsed;
        float inZone;
        float phase;
        bool running;

        protected override void OnBegin()
        {
            elapsed = 0f;
            inZone = 0f;
            phase = Random.Range(0f, 10f);   // pola naik-turun beda tiap main
            doseSlider.value = 0f;
            running = true;
        }

        /// <summary>Kebutuhan klorin (mg/L) berubah-ubah, seolah kuman di air yang masuk naik-turun.</summary>
        float Demand(float t) => 0.5f + 0.25f * Mathf.Sin((t + phase) * 0.6f)
                                      + 0.15f * Mathf.Sin((t + phase) * 1.7f + 1f);

        void Update()
        {
            if (!running) return;

            elapsed += Time.deltaTime;
            float dose = doseSlider.value;
            float residual = Mathf.Max(0f, dose - Demand(elapsed));
            bool good = residual >= WaterQuality.MinSafeChlorine && residual <= WaterQuality.MaxSafeChlorine;
            if (good) inZone += Time.deltaTime;

            // Tampilan pengukur.
            gaugeFill.anchorMax = new Vector2(Mathf.Clamp01(residual / gaugeMax), 1f);
            gaugeFillImage.color = good ? goodColor : residual < WaterQuality.MinSafeChlorine ? lowColor : highColor;
            doseText.text = $"Dosis klorin: {dose:0.00} mg/L";
            residualText.text = $"Sisa klorin: {residual:0.00} mg/L";
            statusText.text = good ? "Aman! Kuman mati, air tidak bau kaporit."
                            : residual < WaterQuality.MinSafeChlorine ? "Kurang! Kuman masih hidup, tambah dosis."
                            : "Kebanyakan! Air bau kaporit, kurangi dosis.";
            timerText.text = $"Waktu: {Mathf.CeilToInt(duration - elapsed)} dtk";

            // Air di unit Desinfeksi ikut berubah: jernih kalau aman, sedikit keruh kalau tidak.
            Unit.SetClarity(good ? 1f : 0.75f);
            // Kuman di bak AR: banyak kalau sisa klorin 0, hilang kalau sudah mencapai batas aman.
            Effects.SetGerms(1f - Mathf.Clamp01(residual / WaterQuality.MinSafeChlorine));

            if (elapsed >= duration)
            {
                running = false;
                Finish(Mathf.RoundToInt(Mathf.Clamp01(inZone / (duration * 0.9f)) * 100f));
            }
        }

        public override void Cancel()
        {
            running = false;
            base.Cancel();
        }
    }
}
