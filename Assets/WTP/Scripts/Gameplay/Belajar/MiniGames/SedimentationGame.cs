using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Mini-game Sedimentasi: flok mengendap jadi lumpur di dasar bak, tebalnya terus naik.
    /// Tekan "Buka Katup Lumpur" untuk membuang lumpur, tapi pada waktu yang tepat:
    ///   - lumpur penuh (100%)  -> lumpur terbawa ke filter, air keruh lolos (-15)
    ///   - buka saat lumpur tipis (&lt; 30%) -> yang terbuang air bersih, boros (-10)
    /// Kadang turun hujan: lumpur naik lebih cepat. Lama main 25 detik, skor awal 100.
    /// Di model AR, permukaan lumpur (objek "Sludge") naik-turun dan roda katup berputar.
    /// </summary>
    public class SedimentationGame : MiniGame
    {
        [SerializeField] Button valveButton;
        [SerializeField] RectTransform sludgeFill;   // tinggi = tebal lumpur
        [SerializeField] Image sludgeFillImage;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text scoreText;

        [SerializeField] float duration = 25f;
        [SerializeField] float normalRate = 0.09f;   // kenaikan lumpur per detik
        [SerializeField] float rainRate = 0.14f;
        [SerializeField] float drainAmount = 0.5f;   // lumpur yang dibuang sekali buka katup
        [SerializeField] float drainTime = 1f;

        [SerializeField] Color okColor = new Color(0.55f, 0.42f, 0.25f);
        [SerializeField] Color dangerColor = new Color(0.85f, 0.3f, 0.25f);

        const float WasteThreshold = 0.3f;
        const float DangerThreshold = 0.8f;
        const float SludgeRise = 0.45f;    // tinggi naiknya objek Sludge saat penuh (satuan lokal unit)

        Transform sludgeModel;
        Transform valveWheel;
        Vector3 sludgeBasePos;
        float elapsed;
        float sludge;            // 0..1
        float draining;          // sisa waktu katup terbuka
        float rainUntil, nextRain;
        int score;
        bool running;

        void Awake() => valveButton.onClick.AddListener(OpenValve);

        protected override void OnBegin()
        {
            sludgeModel = Unit.transform.Find("Sludge");
            valveWheel = Unit.transform.Find("SludgeValve_Wheel");
            if (sludgeModel != null) sludgeBasePos = sludgeModel.localPosition;

            elapsed = 0f;
            sludge = 0.2f;
            draining = 0f;
            rainUntil = 0f;
            nextRain = Random.Range(6f, 10f);
            score = 100;
            running = true;
            statusText.text = "Lumpur menumpuk di dasar bak. Buang sebelum penuh!";
        }

        void OpenValve()
        {
            if (!running || draining > 0f) return;
            if (sludge < WasteThreshold)
            {
                score -= 10;
                statusText.text = "Lumpur masih tipis, yang terbuang malah air bersih! (-10)";
            }
            else
            {
                statusText.text = "Katup dibuka, lumpur dibuang.";
            }
            draining = drainTime;
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            elapsed += dt;

            // Hujan sesekali: air sungai lebih keruh, lumpur cepat menumpuk.
            if (elapsed >= nextRain)
            {
                rainUntil = elapsed + 5f;
                nextRain = elapsed + Random.Range(9f, 13f);
                statusText.text = "Hujan! Air lebih keruh, lumpur cepat menumpuk.";
            }
            float rate = elapsed < rainUntil ? rainRate : normalRate;

            if (draining > 0f)
            {
                draining -= dt;
                sludge -= drainAmount / drainTime * dt;
                if (valveWheel != null) valveWheel.Rotate(0f, 360f * dt, 0f, Space.Self);
            }
            else
            {
                sludge += rate * dt;
            }
            sludge = Mathf.Clamp01(sludge);

            if (sludge >= 1f)
            {
                score -= 15;
                sludge = 0.4f;   // lumpur luber terbawa keluar ke filter
                statusText.text = "Lumpur penuh dan terbawa ke filter, air keruh lolos! (-15)";
            }

            // Tampilan.
            sludgeFill.anchorMax = new Vector2(1f, sludge);
            sludgeFillImage.color = sludge >= DangerThreshold ? dangerColor : okColor;
            if (sludgeModel != null) sludgeModel.localPosition = sludgeBasePos + Vector3.up * (sludge * SludgeRise);
            timerText.text = $"Waktu: {Mathf.CeilToInt(duration - elapsed)} dtk";
            scoreText.text = $"Nilai: {Mathf.Max(0, score)}";
            valveButton.interactable = draining <= 0f;

            if (elapsed >= duration)
            {
                running = false;
                ResetModel();
                Finish(Mathf.Max(0, score));
            }
        }

        void ResetModel()
        {
            if (sludgeModel != null) sludgeModel.localPosition = sludgeBasePos;
        }

        public override void Cancel()
        {
            running = false;
            ResetModel();
            base.Cancel();
        }
    }
}
