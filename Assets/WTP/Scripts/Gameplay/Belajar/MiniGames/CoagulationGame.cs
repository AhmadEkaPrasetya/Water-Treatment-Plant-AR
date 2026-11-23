using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Audio;
using WTP.Plant;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Mini-game Koagulasi (jar test): pilih dosis tawas dengan slider lalu "Tuang &amp; Aduk".
    ///   - kurang tawas  -> kotoran tidak menggumpal, air tetap keruh
    ///   - kelebihan     -> boros dan pH air turun (asam)
    ///   - pas (±5 mg/L) -> terbentuk flok, air di gelas mulai jernih
    /// Dosis tepat diacak tiap main, makin keruh air baku makin banyak tawas.
    /// Skor: benar di percobaan 1 = 100, ke-2 = 80, ke-3 = 60, gagal = 30.
    /// </summary>
    public class CoagulationGame : MiniGame
    {
        [SerializeField] TMP_Text rawWaterText;
        [SerializeField] Slider doseSlider;
        [SerializeField] TMP_Text doseText;
        [SerializeField] Button pourButton;
        [SerializeField] Image jarWater;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text attemptsText;

        [SerializeField] Color dirtyColor = new Color(0.55f, 0.42f, 0.25f);
        [SerializeField] Color cleanColor = new Color(0.45f, 0.75f, 1f);

        const int DoseStep = 5;     // slider bernilai 0..12, dikali 5 = 0..60 mg/L
        const int Tolerance = 5;    // selisih yang masih dianggap pas
        const int MaxAttempts = 3;
        static readonly int[] AttemptScores = { 100, 80, 60 };

        int optimalDose;
        int attempts;
        bool busy;

        int Dose => Mathf.RoundToInt(doseSlider.value) * DoseStep;

        void Awake()
        {
            doseSlider.onValueChanged.AddListener(_ => doseText.text = $"Tawas: {Dose} mg/L");
            pourButton.onClick.AddListener(() => { if (!busy) StartCoroutine(Pour()); });
        }

        protected override void OnBegin()
        {
            optimalDose = Random.Range(4, 9) * DoseStep;            // 20..40 mg/L
            int turbidity = 60 + (optimalDose - 20) * 6 + Random.Range(-10, 11);
            rawWaterText.text = $"Air baku: {turbidity} NTU (makin keruh, makin banyak tawas)";

            attempts = 0;
            busy = false;
            doseSlider.value = 0;
            doseText.text = $"Tawas: {Dose} mg/L";
            jarWater.color = dirtyColor;
            resultText.text = "Geser slider, lalu tekan Tuang & Aduk.";
            attemptsText.text = $"Percobaan 0/{MaxAttempts}";
            pourButton.interactable = true;
        }

        IEnumerator Pour()
        {
            busy = true;
            pourButton.interactable = false;
            attempts++;
            attemptsText.text = $"Percobaan {attempts}/{MaxAttempts}";
            int dose = Dose;
            resultText.text = "Mengaduk...";
            AudioManager.Play(Sfx.Pour);

            // Hitung hasil: seberapa jernih air di gelas setelah flok mengendap.
            int diff = dose - optimalDose;
            float clarity = Mathf.Abs(diff) <= Tolerance ? 1f
                          : diff < 0 ? Mathf.Clamp01((float)dose / optimalDose) * 0.5f
                          : 0.7f;

            // Animasi warna air di gelas + air di unit Koagulasi di AR.
            Color from = jarWater.color;
            Color to = Color.Lerp(dirtyColor, cleanColor, clarity);
            Unit.SetClarity(new WaterQuality(WaterQuality.RiverTurbidity * (1f - clarity * 0.8f)).Clarity);
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                jarWater.color = Color.Lerp(from, to, t / 1.5f);
                yield return null;
            }
            jarWater.color = to;

            // pH turun kira-kira 0.02 per mg/L tawas (tawas bersifat asam).
            float pH = 7.4f - dose * 0.02f;
            bool success = Mathf.Abs(diff) <= Tolerance;
            AudioManager.Play(success ? Sfx.Correct : Sfx.Wrong);
            if (success)
                resultText.text = $"Pas! Flok terbentuk dan mengendap. pH {pH:0.0}";
            else if (diff < 0)
                resultText.text = "Kurang tawas: kotoran belum menggumpal, air masih keruh.";
            else
                resultText.text = $"Kebanyakan tawas: boros dan pH turun jadi {pH:0.0}" +
                                  (pH < WaterQuality.MinSafePH ? " (terlalu asam!)" : ".");

            if (success || attempts >= MaxAttempts)
            {
                if (!success) resultText.text += $"\nDosis yang pas: {optimalDose} mg/L.";
                yield return new WaitForSeconds(2.5f);
                Finish(success ? AttemptScores[attempts - 1] : 30);
                yield break;
            }

            busy = false;
            pourButton.interactable = true;
        }

        public override void Cancel()
        {
            StopAllCoroutines();
            busy = false;
            base.Cancel();
        }
    }
}
