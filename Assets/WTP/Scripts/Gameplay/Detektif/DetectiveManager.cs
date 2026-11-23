using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Audio;
using WTP.Core;
using WTP.Plant;
using WTP.Progress;
using WTP.Story;
using WTP.UI.Panels;

namespace WTP.Gameplay.Detektif
{
    /// <summary>
    /// Bagian 3 (Detektif). Alur satu kasus:
    ///   laporan warga -> periksa data unit (tap tombol unit) -> "Tuduh" unit penyebab
    ///   -> pilih cara perbaikan -> unit pulih di AR -> skor & bintang -> dialog penutup
    ///
    /// Kunci kasus: kerusakan satu unit membuat data unit-unit SESUDAHNYA ikut tidak normal,
    /// jadi penyebabnya adalah unit paling hulu yang datanya tidak normal.
    ///
    /// Skor (maks 100): 3 unit pertama yang diperiksa gratis, unit berikutnya -5,
    ///                  salah tuduh -25, salah pilih perbaikan -15. Minimal 20.
    /// </summary>
    public class DetectiveManager : MonoBehaviour
    {
        const int MaxScore = 100;
        const int MinScore = 20;
        const int FreeInspections = 3;
        const int InspectionCost = 5;
        const int WrongAccusationCost = 25;
        const int WrongFixCost = 15;

        static readonly Color UnitNew = new Color(0.18f, 0.5f, 0.76f);
        static readonly Color UnitChecked = new Color(0.12f, 0.36f, 0.5f);
        static readonly Color UnitSelected = new Color(0.98f, 0.79f, 0.25f);
        static readonly Color TextLight = Color.white;
        static readonly Color TextDark = new Color(0.08f, 0.14f, 0.2f);

        [Header("Konten (Assets/WTP/Data/Detektif)")]
        [SerializeField] CaseData[] cases = new CaseData[PlayerData.DetektifCaseCount];
        [SerializeField] Character tirta;

        [Header("Komponen di scene")]
        [SerializeField] DialogueManager dialogue;
        [SerializeField] LevelResultPanel resultPanel;

        [Header("Pilih kasus")]
        [SerializeField] GameObject caseSelectPanel;
        [SerializeField] Button[] caseButtons = new Button[PlayerData.DetektifCaseCount];
        [SerializeField] TMP_Text[] caseLabels = new TMP_Text[PlayerData.DetektifCaseCount];
        [SerializeField] Button caseBackButton;

        [Header("Layar penyelidikan")]
        [SerializeField] GameObject topPanel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text reportText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] GameObject bottomPanel;
        [SerializeField] Button[] unitButtons = new Button[7];

        [Tooltip("Data unit + tombol Tuduh/Keluar")]
        [SerializeField] GameObject inspectRoot;
        [SerializeField] TMP_Text dataTitle;
        [SerializeField] TMP_Text dataText;
        [SerializeField] Button accuseButton;
        [SerializeField] TMP_Text accuseLabel;
        [SerializeField] Button exitButton;

        [Tooltip("Pertanyaan perbaikan + 3 pilihan")]
        [SerializeField] GameObject fixRoot;
        [SerializeField] TMP_Text fixQuestion;
        [SerializeField] Button[] fixButtons = new Button[3];
        [SerializeField] TMP_Text[] fixLabels = new TMP_Text[3];

        PlantController plant;
        Action onExit;
        int currentCase = -1;
        int selectedUnit = -1;
        readonly HashSet<int> inspected = new HashSet<int>();
        int wrongAccusations;
        int wrongFixes;
        readonly int[] fixOrder = { 0, 1, 2 };   // fixButtons[i] menampilkan fixOptions[fixOrder[i]]

        CaseData Case => cases[currentCase];
        static string PlayerName => SaveSystem.Data.HasName ? SaveSystem.Data.playerName : "kawan";

        void Awake()
        {
            for (int i = 0; i < caseButtons.Length; i++)
            {
                int index = i;
                caseButtons[i].onClick.AddListener(() => StartCase(index));
            }
            caseBackButton.onClick.AddListener(Exit);
            for (int i = 0; i < unitButtons.Length; i++)
            {
                int index = i;
                unitButtons[i].onClick.AddListener(() => Inspect(index));
            }
            for (int i = 0; i < fixButtons.Length; i++)
            {
                int index = i;
                fixButtons[i].onClick.AddListener(() => OnFix(index));
            }
            accuseButton.onClick.AddListener(OnAccuse);
            exitButton.onClick.AddListener(ShowCaseSelect);
            HideAll();
        }

        void Start() => GameManager.Instance.StateChanged += OnStateChanged;

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
        }

        // WTP ditaruh ulang -> batalkan semua.
        void OnStateChanged(GameState state)
        {
            if (state != GameState.Scanning || plant == null) return;
            dialogue.Stop();
            resultPanel.Hide();
            HideAll();
            plant = null;
            onExit = null;
            currentCase = -1;
        }

        // ------------------------------------------------------------ masuk / keluar bagian

        /// <summary>Dipanggil SectionController saat pemain memilih Detektif.</summary>
        public void Open(PlantController placedPlant, Action onExitSection)
        {
            plant = placedPlant;
            onExit = onExitSection;
            ShowCaseSelect();
        }

        void Exit()
        {
            HideAll();
            Action callback = onExit;
            onExit = null;
            plant = null;
            callback?.Invoke();
        }

        void HideAll()
        {
            if (plant != null) plant.Effects.StopAll();
            caseSelectPanel.SetActive(false);
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);
        }

        void ShowCaseSelect()
        {
            HideAll();
            // WTP di AR kembali normal (semua unit sudah berfungsi setelah Bagian 2).
            if (plant != null) plant.SetWorkingUnitCount(AllUnits, instant: true);
            currentCase = -1;

            PlayerData data = SaveSystem.Data;
            for (int i = 0; i < caseButtons.Length; i++)
            {
                bool exists = i < cases.Length && cases[i] != null;
                bool open = exists && data.IsDetektifCaseUnlocked(i);
                caseButtons[i].interactable = open;
                string done = data.detektifStars[i] > 0 ? $"   Bintang {data.detektifStars[i]}/3" : "";
                caseLabels[i].text = exists ? $"Kasus {i + 1}: {cases[i].title}{(open ? done : "  (terkunci)")}" : "";
            }
            caseSelectPanel.SetActive(true);
        }

        static int AllUnits => PlayerData.BelajarLevelCount;   // 7 unit WTP

        // ------------------------------------------------------------ mulai kasus

        void StartCase(int caseIndex)
        {
            caseSelectPanel.SetActive(false);
            currentCase = caseIndex;
            dialogue.Play(Case.intro, PlayerName, BeginInvestigation);
        }

        void BeginInvestigation()
        {
            inspected.Clear();
            selectedUnit = -1;
            wrongAccusations = 0;
            wrongFixes = 0;
            ShowSymptomsInAR();

            titleText.text = $"Kasus {currentCase + 1}: {Case.title}";
            reportText.text = $"Laporan {Case.reporter}: \"{Case.report}\"";
            statusText.text = "Tap unit untuk memeriksa datanya. Bandingkan dengan batas normal.";
            dataTitle.text = "Data Unit";
            dataText.text = "Belum ada unit yang diperiksa.\n\nPetunjuk: kerusakan satu unit membuat unit-unit sesudahnya ikut tidak normal.";
            accuseButton.interactable = false;
            accuseLabel.text = "Tuduh unit ini";
            RefreshUnitButtons();

            inspectRoot.SetActive(true);
            fixRoot.SetActive(false);
            topPanel.SetActive(true);
            bottomPanel.SetActive(true);
        }

        /// <summary>
        /// Petunjuk visual di AR: air keruh mulai dari unit penyebab ke hilir.
        /// Unit tidak ditandai rusak (rotor tetap berputar) supaya jawabannya tidak ketahuan.
        /// </summary>
        void ShowSymptomsInAR()
        {
            plant.SetWorkingUnitCount(AllUnits, instant: true);
            float clarity = new WaterQuality(Case.symptomTurbidity).Clarity;
            for (int i = (int)Case.faultyUnit; i < plant.Units.Count; i++)
                plant.Units[i].SetClarity(clarity);
            plant.Effects.SetAsh(Case.ashFall);
        }

        // ------------------------------------------------------------ inspeksi

        void Inspect(int unitIndex)
        {
            if (fixRoot.activeSelf) return;
            selectedUnit = unitIndex;
            bool isNew = inspected.Add(unitIndex);

            UnitReport report = Case.units[unitIndex];
            var sb = new StringBuilder();
            foreach (Measurement m in report.measurements)
                sb.Append($"{m.label}: <b>{m.value}</b>\n<size=80%><color=#9FD3F5>   normal: {m.normalRange}</color></size>\n\n");
            dataTitle.text = $"Data {(UnitType)unitIndex}";
            dataText.text = sb.ToString();

            int extra = inspected.Count - FreeInspections;
            statusText.text = isNew && extra > 0
                ? $"Unit diperiksa: {inspected.Count} (pemeriksaan ke-{inspected.Count} mengurangi skor {InspectionCost})"
                : $"Unit diperiksa: {inspected.Count}. {FreeInspections} pemeriksaan pertama gratis.";

            accuseButton.interactable = true;
            accuseLabel.text = $"Tuduh {(UnitType)unitIndex}";
            RefreshUnitButtons();
        }

        void RefreshUnitButtons()
        {
            for (int i = 0; i < unitButtons.Length; i++)
            {
                bool selected = i == selectedUnit;
                unitButtons[i].image.color = selected ? UnitSelected : inspected.Contains(i) ? UnitChecked : UnitNew;
                unitButtons[i].GetComponentInChildren<TMP_Text>().color = selected ? TextDark : TextLight;
            }
        }

        // ------------------------------------------------------------ tuduh

        void OnAccuse()
        {
            if (selectedUnit < 0) return;

            if (selectedUnit == (int)Case.faultyUnit)
            {
                AudioManager.Play(Sfx.Correct);
                ShowFixQuestion();
                return;
            }

            wrongAccusations++;
            AudioManager.Play(Sfx.Wrong);
            UnitType accused = (UnitType)selectedUnit;
            // Petunjuk berbeda: unit yang memang tidak normal (korban di hilir) vs unit yang normal.
            string hint = Case.units[selectedUnit].IsAbnormal
                ? $"Data {accused} memang tidak normal, tapi itu akibat masalah dari unit sebelumnya. Cari unit paling hulu yang datanya tidak normal!"
                : $"Data {accused} masih dalam batas normal semua. Bukan dia biang keroknya.";
            PlayTirta(hint, () => statusText.text = $"Salah tuduh -{WrongAccusationCost} skor. Periksa lagi datanya.");
        }

        void ShowFixQuestion()
        {
            // Acak urutan pilihan supaya jawaban benar tidak selalu di tempat yang sama.
            for (int i = fixOrder.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (fixOrder[i], fixOrder[j]) = (fixOrder[j], fixOrder[i]);
            }
            for (int i = 0; i < fixButtons.Length; i++)
            {
                fixLabels[i].text = Case.fixOptions[fixOrder[i]];
                fixButtons[i].interactable = true;
            }
            fixQuestion.text = $"Betul, masalahnya di {Case.faultyUnit}! Bagaimana cara memperbaikinya?";
            statusText.text = "Pilih tindakan perbaikan yang tepat.";
            inspectRoot.SetActive(false);
            fixRoot.SetActive(true);
        }

        void OnFix(int buttonIndex)
        {
            if (fixOrder[buttonIndex] != Case.correctFix)
            {
                wrongFixes++;
                AudioManager.Play(Sfx.Wrong);
                fixButtons[buttonIndex].interactable = false;
                statusText.text = $"Itu tidak mengatasi penyebabnya (-{WrongFixCost} skor). Coba pilihan lain.";
                return;
            }
            Solve();
        }

        // ------------------------------------------------------------ selesai

        void Solve()
        {
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);
            plant.SetWorkingUnitCount(AllUnits);   // air kembali jernih pelan-pelan
            plant.Effects.StopAll();
            plant.Effects.CelebrateAll();
            AudioManager.Play(Sfx.Correct);

            int extraInspections = Mathf.Max(0, inspected.Count - FreeInspections);
            int score = Mathf.Max(MinScore, MaxScore
                                            - extraInspections * InspectionCost
                                            - wrongAccusations * WrongAccusationCost
                                            - wrongFixes * WrongFixCost);
            int stars = ScoreManager.StarsForRatio((float)score / MaxScore);

            PlayerData data = SaveSystem.Data;
            data.SetDetektifResult(currentCase, stars, score);
            BadgeManager.Evaluate();   // Detektif Jeli (skor 100), Detektif Hulu (kasus terakhir)
            SaveSystem.Save();
            LeaderboardManager.Submit(LeaderboardCategory.Detektif, data.playerName, data.DetektifTotalScore);

            string detail = $"Unit diperiksa: {inspected.Count} (-{extraInspections * InspectionCost})\n" +
                            $"Salah tuduh: {wrongAccusations}x (-{wrongAccusations * WrongAccusationCost})\n" +
                            $"Salah perbaikan: {wrongFixes}x (-{wrongFixes * WrongFixCost})";

            // Penjelasan perbaikan dulu, baru layar skor.
            PlayTirta(Case.fixExplanation, () =>
            {
                GameManager.Instance.SetState(GameState.Result);
                resultPanel.Show("Kasus terpecahkan!", score, MaxScore, stars, detail, PlayOutro);
            }, keepPanelsHidden: true);
        }

        void PlayOutro()
        {
            resultPanel.Hide();
            GameManager.Instance.SetState(GameState.Playing);
            dialogue.Play(Case.outro, PlayerName, ShowCaseSelect);
        }

        /// <summary>Tirta bicara satu baris; panel disembunyikan selama dialog supaya tidak menumpuk.</summary>
        void PlayTirta(string text, Action after, bool keepPanelsHidden = false)
        {
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);
            dialogue.Play(new[] { new DialogueLine { speaker = tirta, text = text } }, PlayerName, () =>
            {
                if (!keepPanelsHidden)
                {
                    topPanel.SetActive(true);
                    bottomPanel.SetActive(true);
                }
                after?.Invoke();
            });
        }
    }
}
