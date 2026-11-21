using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Core;
using WTP.Plant;
using WTP.Progress;
using WTP.Story;
using WTP.UI.Panels;

namespace WTP.Gameplay.Shift
{
    /// <summary>
    /// Bagian 4 (Operator Shift: Musim Asap). Pemain menjaga WTP selama 3 menit secara real-time:
    ///   - geser dosis tawas, dosis klorin, dan pompa intake (slider)
    ///   - ShiftSimulation menghitung kekeruhan, sisa klorin, reservoir, dan kepuasan warga
    ///   - 5 event datang bergiliran pada waktu acak (urutan diacak)
    ///   - kepuasan warga 0 = shift gagal; bertahan 3 menit = berhasil (badge + leaderboard + cutscene tamat)
    /// Skor: poin per detik (semua aman 5, dua aman 2) + bonus kepuasan akhir (maks 100). Maks 1000.
    /// </summary>
    public class ShiftManager : MonoBehaviour
    {
        const float ShiftSeconds = 180f;
        const int MaxScore = 1000;
        const float FirstEventTime = 20f;
        const float EventSpacing = 32f;
        const float ArRefreshInterval = 0.25f;

        static readonly Color SafeColor = new Color(0.45f, 0.9f, 0.5f);
        static readonly Color DangerColor = new Color(1f, 0.45f, 0.4f);
        static readonly Color HiddenColor = new Color(0.7f, 0.7f, 0.7f);

        [Header("Konten (Assets/WTP/Data)")]
        [SerializeField] ShiftEvent[] events;
        [Tooltip("Penjelasan aturan dari Tirta (diputar sekali)")]
        [SerializeField] DialogueScene intro;
        [Tooltip("Cutscene tamat (diputar saat pertama kali berhasil)")]
        [SerializeField] DialogueScene ending;
        [SerializeField] Character pakWacana;

        [Header("Komponen di scene")]
        [SerializeField] DialogueManager dialogue;
        [SerializeField] LevelResultPanel resultPanel;

        [Header("Layar mulai")]
        [SerializeField] GameObject startPanel;
        [SerializeField] TMP_Text startInfoText;
        [SerializeField] Button startButton;
        [SerializeField] Button startBackButton;

        [Header("Panel atas")]
        [SerializeField] GameObject topPanel;
        [SerializeField] TMP_Text timerText;
        [SerializeField] RectTransform satisfactionFill;
        [SerializeField] Image satisfactionFillImage;
        [SerializeField] TMP_Text satisfactionText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text eventText;
        [SerializeField] Button quitButton;

        [Header("Panel bawah: 3 parameter")]
        [SerializeField] GameObject bottomPanel;
        [SerializeField] TMP_Text turbidityText;
        [SerializeField] TMP_Text rawText;
        [SerializeField] Slider alumSlider;
        [SerializeField] TMP_Text alumLabel;
        [SerializeField] TMP_Text chlorineText;
        [SerializeField] TMP_Text chlorineInfoText;
        [SerializeField] Slider chlorineSlider;
        [SerializeField] TMP_Text chlorineLabel;
        [SerializeField] TMP_Text reservoirText;
        [SerializeField] TMP_Text demandText;
        [SerializeField] Slider pumpSlider;
        [SerializeField] TMP_Text pumpLabel;

        [Header("Inspeksi Pak Wacana")]
        [SerializeField] GameObject inspectionPanel;
        [SerializeField] Image inspectionPortrait;
        [SerializeField] TMP_Text inspectionText;
        [SerializeField] Button acceptButton;
        [SerializeField] TMP_Text acceptLabel;
        [SerializeField] Button refuseButton;
        [SerializeField] TMP_Text refuseLabel;

        [Header("Kabut asap")]
        [Tooltip("Lapisan abu-abu transparan selayar penuh")]
        [SerializeField] GameObject hazeOverlay;

        PlantController plant;
        Action onExit;
        ShiftSimulation sim;
        bool running;
        float elapsed;
        float score;
        float pumpOffTimer;
        float arTimer;

        // Jadwal event: event ke-i mulai di startTimes[i].
        readonly List<ShiftEvent> schedule = new List<ShiftEvent>();
        readonly List<float> startTimes = new List<float>();
        readonly List<ShiftEvent> active = new List<ShiftEvent>();
        readonly Dictionary<ShiftEvent, float> endTimes = new Dictionary<ShiftEvent, float>();
        int nextEvent;
        ShiftEvent pendingInspection;
        float inspectionTimeout;

        static string PlayerName => SaveSystem.Data.HasName ? SaveSystem.Data.playerName : "kawan";

        void Awake()
        {
            startButton.onClick.AddListener(OnStart);
            startBackButton.onClick.AddListener(Exit);
            quitButton.onClick.AddListener(() => End(false, quit: true));
            acceptButton.onClick.AddListener(() => AnswerInspection(true));
            refuseButton.onClick.AddListener(() => AnswerInspection(false));
            HideAll();
        }

        void Start() => GameManager.Instance.StateChanged += OnStateChanged;

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
            SetSmoke(false);
        }

        // WTP ditaruh ulang -> shift dibatalkan.
        void OnStateChanged(GameState state)
        {
            if (state != GameState.Scanning || plant == null) return;
            running = false;
            dialogue.Stop();
            resultPanel.Hide();
            HideAll();
            SetSmoke(false);
            plant = null;
            onExit = null;
        }

        // ------------------------------------------------------------ masuk / keluar bagian

        /// <summary>Dipanggil SectionController saat pemain memilih Operator Shift.</summary>
        public void Open(PlantController placedPlant, Action onExitSection)
        {
            plant = placedPlant;
            onExit = onExitSection;
            ShowStart();
        }

        void Exit()
        {
            HideAll();
            RestorePlant();
            Action callback = onExit;
            onExit = null;
            plant = null;
            callback?.Invoke();
        }

        void HideAll()
        {
            startPanel.SetActive(false);
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);
            inspectionPanel.SetActive(false);
            hazeOverlay.SetActive(false);
        }

        void RestorePlant()
        {
            if (plant == null) return;
            plant.SetWorkingUnitCount(PlayerData.BelajarLevelCount, instant: true);
        }

        void ShowStart()
        {
            HideAll();
            RestorePlant();
            int best = SaveSystem.Data.bestShiftScore;
            startInfoText.text =
                "Jaga WTP selama 3 menit di musim asap.\n" +
                "Atur dosis tawas, dosis klorin, dan pompa supaya:\n" +
                "<color=#9FD3F5>kekeruhan < 3 NTU  |  sisa klorin 0,2-0,5 mg/L  |  reservoir 20-95%</color>\n" +
                "Kalau kepuasan warga habis, shift gagal.\n\n" +
                (best > 0 ? $"Skor terbaikmu: {best}" : "Belum pernah main.");
            startPanel.SetActive(true);
        }

        void OnStart()
        {
            startPanel.SetActive(false);
            if (!SaveSystem.Data.shiftIntroSeen && intro != null)
            {
                dialogue.Play(intro.lines, PlayerName, () =>
                {
                    SaveSystem.Data.shiftIntroSeen = true;
                    SaveSystem.Save();
                    BeginShift();
                });
            }
            else
            {
                BeginShift();
            }
        }

        // ------------------------------------------------------------ jalannya shift

        void BeginShift()
        {
            sim = new ShiftSimulation();
            elapsed = 0f;
            score = 0f;
            pumpOffTimer = 0f;
            pendingInspection = null;
            active.Clear();
            endTimes.Clear();
            BuildSchedule();

            // Slider mulai dari pengaturan awal yang aman.
            alumSlider.SetValueWithoutNotify(sim.alumDose);
            chlorineSlider.SetValueWithoutNotify(sim.chlorineDose);
            pumpSlider.SetValueWithoutNotify(sim.pump * 100f);
            eventText.text = "Kondisi normal. Pantau terus parameternya!";

            plant.SetWorkingUnitCount(PlayerData.BelajarLevelCount, instant: true);
            topPanel.SetActive(true);
            bottomPanel.SetActive(true);
            GameManager.Instance.SetState(GameState.Playing);
            running = true;
            RefreshUI();
        }

        /// <summary>Urutan event diacak; waktunya kira-kira tiap 32 detik (sedikit diacak juga).</summary>
        void BuildSchedule()
        {
            schedule.Clear();
            startTimes.Clear();
            schedule.AddRange(events);
            for (int i = schedule.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (schedule[i], schedule[j]) = (schedule[j], schedule[i]);
            }
            for (int i = 0; i < schedule.Count; i++)
                startTimes.Add(FirstEventTime + i * EventSpacing + UnityEngine.Random.Range(-4f, 4f));
            nextEvent = 0;
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            elapsed += dt;

            UpdateEvents();

            // Kendali pemain -> simulasi.
            sim.alumDose = alumSlider.value;
            sim.chlorineDose = chlorineSlider.value;
            sim.pump = pumpSlider.value / 100f;
            pumpOffTimer = Mathf.Max(0f, pumpOffTimer - dt);
            sim.pumpForcedOff = pumpOffTimer > 0f;

            sim.Step(dt);
            score += sim.PointsPerSecond * dt;
            RefreshUI();

            arTimer -= dt;
            if (arTimer <= 0f)
            {
                arTimer = ArRefreshInterval;
                RefreshAR();
            }

            if (sim.Satisfaction <= 0f) End(false);
            else if (elapsed >= ShiftSeconds) End(true);
        }

        void UpdateEvents()
        {
            // Event baru mulai.
            if (nextEvent < schedule.Count && elapsed >= startTimes[nextEvent])
            {
                ShiftEvent e = schedule[nextEvent++];
                active.Add(e);
                endTimes[e] = elapsed + e.duration;
                eventText.text = $"<color=#FFB347>PERINGATAN: {e.title}!</color> {e.warning}";
                if (e.isInspection) ShowInspection(e);
            }

            // Event yang sudah habis waktunya.
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (elapsed < endTimes[active[i]]) continue;
                if (active[i] == pendingInspection) AnswerInspection(false);   // tidak dijawab = dianggap menolak
                active.RemoveAt(i);
                if (active.Count == 0) eventText.text = "Kejadian sudah lewat. Kembalikan pengaturan ke kondisi normal.";
            }

            // Gabungkan efek semua event aktif.
            sim.turbidityMultiplier = 1f;
            sim.demandMultiplier = 1f;
            sim.pumpCapMultiplier = 1f;
            sim.extraSatisfactionDrain = 0f;
            bool smoke = false;
            foreach (ShiftEvent e in active)
            {
                sim.turbidityMultiplier *= e.turbidityMultiplier;
                sim.demandMultiplier *= e.demandMultiplier;
                sim.pumpCapMultiplier *= e.pumpCapMultiplier;
                sim.extraSatisfactionDrain += e.satisfactionDrain;
                smoke |= e.smoke;
            }
            SetSmoke(smoke);
        }

        bool ReadingsHidden
        {
            get
            {
                foreach (ShiftEvent e in active)
                    if (e.hideReadings) return true;
                return false;
            }
        }

        // ------------------------------------------------------------ inspeksi Pak Wacana

        void ShowInspection(ShiftEvent e)
        {
            pendingInspection = e;
            inspectionText.text = e.inspectionText;
            acceptLabel.text = e.acceptLabel;
            refuseLabel.text = e.refuseLabel;
            bool hasPortrait = pakWacana != null && pakWacana.portrait != null;
            inspectionPortrait.gameObject.SetActive(hasPortrait);
            if (hasPortrait) inspectionPortrait.sprite = pakWacana.portrait;
            inspectionPanel.SetActive(true);
        }

        void AnswerInspection(bool accept)
        {
            ShiftEvent e = pendingInspection;
            if (e == null) return;
            pendingInspection = null;
            inspectionPanel.SetActive(false);

            if (accept)
            {
                pumpOffTimer = e.acceptPumpOffSeconds;
                sim.AddSatisfaction(e.acceptSatisfaction);
                eventText.text = e.acceptResult;
            }
            else
            {
                sim.AddSatisfaction(e.refuseSatisfaction);
                eventText.text = e.refuseResult;
            }
        }

        // ------------------------------------------------------------ tampilan

        void RefreshUI()
        {
            int left = Mathf.CeilToInt(Mathf.Max(0f, ShiftSeconds - elapsed));
            timerText.text = $"Sisa waktu {left / 60}:{left % 60:00}";
            scoreText.text = $"Skor: {Mathf.FloorToInt(score)}";

            float s = sim.Satisfaction / 100f;
            satisfactionFill.anchorMax = new Vector2(s, 1f);
            satisfactionFillImage.color = Color.Lerp(DangerColor, SafeColor, s);
            satisfactionText.text = $"Kepuasan warga: {Mathf.RoundToInt(sim.Satisfaction)}%";

            bool hidden = ReadingsHidden;
            if (hidden)
            {
                SetReading(turbidityText, "Kekeruhan: ??? (sensor tertutup asap)", HiddenColor);
                SetReading(chlorineText, "Sisa klorin: ??? (sensor tertutup asap)", HiddenColor);
                rawText.text = "Lihat warna air di WTP AR untuk menebak kondisinya.";
            }
            else
            {
                SetReading(turbidityText, $"Kekeruhan air olahan: {sim.Turbidity:0.0} NTU  (aman < 3)",
                           sim.TurbiditySafe ? SafeColor : DangerColor);
                SetReading(chlorineText, $"Sisa klorin: {sim.Chlorine:0.00} mg/L  (aman 0,2-0,5)",
                           sim.ChlorineSafe ? SafeColor : DangerColor);
                rawText.text = $"Air baku sungai: {sim.RawTurbidity:0} NTU";
            }
            chlorineInfoText.text = "Makin keruh air, makin banyak klorin habis bereaksi.";
            alumLabel.text = $"Dosis tawas: {alumSlider.value:0} mg/L";
            chlorineLabel.text = $"Dosis klorin: {chlorineSlider.value:0.00} mg/L";

            SetReading(reservoirText, $"Reservoir: {sim.Reservoir:0}%  (aman 20-95%)",
                       sim.ReservoirSafe ? SafeColor : DangerColor);
            demandText.text = sim.pumpForcedOff
                ? $"POMPA DIMATIKAN ({Mathf.CeilToInt(pumpOffTimer)} dtk)"
                : $"Pemakaian kota: {(sim.demandMultiplier > 1f ? "tinggi" : "normal")}" +
                  (sim.pumpCapMultiplier < 1f ? "  |  debit sungai turun" : "");
            pumpLabel.text = $"Pompa intake: {pumpSlider.value:0}%";
        }

        static void SetReading(TMP_Text text, string value, Color color)
        {
            text.text = value;
            text.color = color;
        }

        /// <summary>WTP di AR mengikuti simulasi: warna air tiap unit, tinggi reservoir, lampu kota, rotor.</summary>
        void RefreshAR()
        {
            IReadOnlyList<WTPUnit> units = plant.Units;
            float raw = new WaterQuality(sim.RawTurbidity).Clarity;
            float treated = new WaterQuality(sim.Turbidity).Clarity;
            float settled = new WaterQuality(Mathf.Lerp(sim.Turbidity, sim.RawTurbidity, 0.3f)).Clarity;
            for (int i = 0; i < units.Count; i++)
            {
                // Intake-Koagulasi-Flokulasi masih air baku, Sedimentasi setengah jernih, sisanya air olahan.
                float clarity = i <= (int)UnitType.Flokulasi ? raw : i == (int)UnitType.Sedimentasi ? settled : treated;
                units[i].SetClarity(clarity);
                units[i].SetBroken(sim.pumpForcedOff);   // pompa mati -> rotor berhenti
            }
            plant.GetUnit(UnitType.Reservoir).SetWaterLevel(sim.Reservoir / 100f);
            plant.SetCityLevel(sim.Satisfaction / 100f);
        }

        void SetSmoke(bool on)
        {
            if (hazeOverlay.activeSelf != on) hazeOverlay.SetActive(on);
            RenderSettings.fog = on;
            if (on)
            {
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = new Color(0.6f, 0.58f, 0.55f);
                RenderSettings.fogDensity = 0.9f;
            }
        }

        // ------------------------------------------------------------ selesai

        void End(bool survived, bool quit = false)
        {
            if (!running) return;
            running = false;
            SetSmoke(false);
            inspectionPanel.SetActive(false);
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);

            if (quit)
            {
                ShowStart();
                return;
            }

            int satisfactionBonus = survived ? Mathf.RoundToInt(sim.Satisfaction) : 0;
            int finalScore = Mathf.Min(MaxScore, Mathf.FloorToInt(score) + satisfactionBonus);
            int stars = survived ? ScoreManager.StarsForRatio((float)finalScore / MaxScore) : 0;

            PlayerData data = SaveSystem.Data;
            bool firstWin = survived && !data.shiftCompleted;
            if (finalScore > data.bestShiftScore) data.bestShiftScore = finalScore;
            if (survived)
            {
                data.shiftCompleted = true;
                BadgeManager.Award(BadgeManager.PenyelamatTirtapura);
            }
            BadgeManager.Evaluate();
            SaveSystem.Save();
            LeaderboardManager.Submit(LeaderboardCategory.Shift, data.playerName, finalScore);

            string detail = survived
                ? $"Bertahan 3 menit!\nPoin menjaga parameter: {Mathf.FloorToInt(score)}\nBonus kepuasan warga: {satisfactionBonus}"
                : $"Kepuasan warga habis di menit {Mathf.FloorToInt(elapsed / 60f)}:{Mathf.FloorToInt(elapsed % 60f):00}.\nCoba lagi: siapkan dosis sebelum event memuncak.";
            GameManager.Instance.SetState(GameState.Result);
            resultPanel.Show(survived ? "Shift selesai!" : "Shift gagal...", finalScore, MaxScore, stars, detail, () =>
            {
                resultPanel.Hide();
                GameManager.Instance.SetState(GameState.Playing);
                RestorePlant();
                if (firstWin && ending != null) dialogue.Play(ending.lines, PlayerName, ShowStart);
                else ShowStart();
            });
        }
    }
}
