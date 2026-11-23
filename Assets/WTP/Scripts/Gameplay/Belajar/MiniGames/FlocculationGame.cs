using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Mini-game Flokulasi: putar jari melingkar di roda pengaduk untuk mengaduk air PELAN-PELAN.
    ///   - terlalu pelan  -> flok tidak bertemu, tidak tumbuh
    ///   - pas            -> gumpalan kecil saling menempel, flok makin besar
    ///   - terlalu cepat  -> flok pecah lagi (ukuran turun)
    /// Pengaduk (Paddle_Rotor) di model AR ikut berputar sesuai kecepatan jari.
    /// Skor: flok penuh dalam 12 dtk = 100, makin lama makin kecil (min 60);
    /// kalau waktu habis, skor = 60% x ukuran flok.
    /// </summary>
    public class FlocculationGame : MiniGame, IBeginDragHandler, IDragHandler
    {
        [SerializeField] RectTransform dial;          // roda yang diputar jari
        [SerializeField] RectTransform dialHandle;    // tanda di roda, ikut berputar
        [SerializeField] RectTransform speedFill;     // bar kecepatan aduk
        [SerializeField] Image speedFillImage;
        [SerializeField] RectTransform flocFill;      // bar ukuran flok
        [SerializeField] RectTransform[] flocDots;    // titik-titik flok yang membesar
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text timerText;

        [SerializeField] float duration = 25f;
        [Tooltip("Batas kecepatan aduk yang pas (derajat per detik)")]
        [SerializeField] float minGoodSpeed = 180f;
        [SerializeField] float maxGoodSpeed = 540f;
        [SerializeField] float maxShownSpeed = 900f;

        [SerializeField] Color slowColor = new Color(0.5f, 0.6f, 0.7f);
        [SerializeField] Color goodColor = new Color(0.25f, 0.75f, 0.35f);
        [SerializeField] Color fastColor = new Color(0.85f, 0.3f, 0.25f);

        readonly List<Transform> paddles = new List<Transform>();
        float elapsed;
        float floc;              // 0..1
        float speed;             // kecepatan aduk saat ini (derajat/detik, dihaluskan)
        float dragAngleThisFrame;
        float lastAngle;
        bool running;

        protected override void OnBegin()
        {
            paddles.Clear();
            foreach (Transform t in Unit.GetComponentsInChildren<Transform>(true))
                if (t.name.EndsWith("_Rotor")) paddles.Add(t);

            elapsed = 0f;
            floc = 0f;
            speed = 0f;
            dragAngleThisFrame = 0f;
            running = true;
        }

        // Sudut jari terhadap pusat roda (derajat).
        float AngleAt(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dial, e.position, e.pressEventCamera, out Vector2 local);
            return Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
        }

        public void OnBeginDrag(PointerEventData e) => lastAngle = AngleAt(e);

        public void OnDrag(PointerEventData e)
        {
            float angle = AngleAt(e);
            // DeltaAngle: selisih sudut terpendek (-180..180), arah putar bebas.
            dragAngleThisFrame += Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle));
            lastAngle = angle;
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            elapsed += dt;

            // Kecepatan dihaluskan supaya tidak loncat-loncat tiap frame.
            float rawSpeed = dt > 0f ? dragAngleThisFrame / dt : 0f;
            dragAngleThisFrame = 0f;
            speed = Mathf.Lerp(speed, rawSpeed, 1f - Mathf.Exp(-4f * dt));

            bool tooSlow = speed < minGoodSpeed, tooFast = speed > maxGoodSpeed;
            if (tooFast) floc -= 0.15f * dt;
            else if (!tooSlow) floc += 0.1f * dt;
            floc = Mathf.Clamp01(floc);

            // Tampilan.
            dialHandle.localRotation *= Quaternion.Euler(0f, 0f, -speed * dt);
            foreach (Transform p in paddles) p.Rotate(0f, speed * 0.5f * dt, 0f, Space.Self);
            Effects.SetFloc(floc, speed * 0.5f);   // partikel flok di bak AR ikut membesar & berputar
            speedFill.anchorMax = new Vector2(Mathf.Clamp01(speed / maxShownSpeed), 1f);
            speedFillImage.color = tooFast ? fastColor : tooSlow ? slowColor : goodColor;
            flocFill.anchorMax = new Vector2(floc, 1f);
            for (int i = 0; i < flocDots.Length; i++)
                flocDots[i].localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, floc);
            statusText.text = tooFast ? "Terlalu cepat! Flok pecah lagi."
                            : tooSlow ? "Putar jari melingkar di roda, pelan-pelan saja."
                            : "Pas! Gumpalan kecil saling menempel jadi flok besar.";
            timerText.text = $"Waktu: {Mathf.CeilToInt(duration - elapsed)} dtk";

            if (floc >= 1f)
            {
                running = false;
                // 12 dtk atau kurang = 100, turun linear sampai 60 di akhir waktu.
                float t = Mathf.InverseLerp(12f, duration, elapsed);
                Finish(Mathf.RoundToInt(Mathf.Lerp(100f, 60f, t)));
            }
            else if (elapsed >= duration)
            {
                running = false;
                Finish(Mathf.RoundToInt(floc * 60f));
            }
        }

        public override void Cancel()
        {
            running = false;
            base.Cancel();
        }
    }
}
