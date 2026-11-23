using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Audio;
using WTP.Core;
using WTP.Plant;
using WTP.Progress;
using WTP.Story;
using WTP.UI.Panels;

namespace WTP.Gameplay.Rakit
{
    /// <summary>
    /// Bagian 2 (Rakit WTP). Alur:
    ///   pilih level -> dialog anggaran Pak Wacana -> susun kartu ke 7 slot (tap kartu, lalu tap slot)
    ///   -> "Alirkan Air" -> simulasi slot per slot (WTP di AR ikut berubah)
    ///   -> benar: skor & bintang -> dialog penutup | salah: Tirta menjelaskan akibatnya, pemain memperbaiki susunan
    ///
    /// Skor (maks 100): 50 kalau WTP berhasil + 25 kalau tidak pernah memasang kartu jebakan
    ///                  + 25 dikurangi 10 per simulasi gagal.
    /// </summary>
    public class AssemblyManager : MonoBehaviour
    {
        const int MaxScore = 100;
        const float StepDelay = 1.1f;   // jeda antar-slot saat simulasi (detik)

        [Header("Konten (Assets/WTP/Data/Rakit)")]
        [SerializeField] AssemblyLevelData[] levels = new AssemblyLevelData[PlayerData.RakitLevelCount];
        [Tooltip("Tokoh yang menjelaskan kesalahan simulasi")]
        [SerializeField] Character tirta;

        [Header("Komponen di scene")]
        [SerializeField] DialogueManager dialogue;
        [SerializeField] LevelResultPanel resultPanel;

        [Header("Pilih level Rakit")]
        [SerializeField] GameObject levelSelectPanel;
        [SerializeField] Button[] levelButtons = new Button[PlayerData.RakitLevelCount];
        [SerializeField] TMP_Text[] levelLabels = new TMP_Text[PlayerData.RakitLevelCount];
        [SerializeField] Button levelBackButton;

        [Header("Layar merakit")]
        [SerializeField] GameObject topPanel;
        [SerializeField] GameObject bottomPanel;
        [Tooltip("Kartu, keterangan, dan tombol; disembunyikan saat simulasi")]
        [SerializeField] GameObject controls;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text budgetText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text infoText;
        [SerializeField] UnitSlot[] slots = new UnitSlot[FlowSimulation.SlotCount];
        [SerializeField] UnitCard[] cards = new UnitCard[12];
        [SerializeField] Button flowButton;
        [SerializeField] Button clearButton;
        [SerializeField] Button exitButton;

        PlantController plant;
        Action onExit;
        int currentLevel = -1;
        int budgetLeft;
        UnitCard selected;
        int failedRuns;
        bool trapUsed;          // pernah memasang kartu jebakan di level ini
        bool simulating;

        AssemblyLevelData Level => levels[currentLevel];
        static string PlayerName => SaveSystem.Data.HasName ? SaveSystem.Data.playerName : "kawan";

        void Awake()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int index = i;
                levelButtons[i].onClick.AddListener(() => StartLevel(index));
            }
            levelBackButton.onClick.AddListener(Exit);

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slots[i].Button.onClick.AddListener(() => OnSlot(index));
            }
            foreach (UnitCard card in cards)
            {
                UnitCard c = card;
                c.Button.onClick.AddListener(() => OnCard(c));
            }
            flowButton.onClick.AddListener(OnFlow);
            clearButton.onClick.AddListener(ClearSlots);
            exitButton.onClick.AddListener(BackToLevelSelect);
            HideAll();
        }

        void Start() => GameManager.Instance.StateChanged += OnStateChanged;

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
        }

        // WTP ditaruh ulang -> batalkan semua. SectionController akan menampilkan menu lagi.
        void OnStateChanged(GameState state)
        {
            if (state != GameState.Scanning || plant == null) return;
            StopAllCoroutines();
            dialogue.Stop();
            resultPanel.Hide();
            HideAll();
            plant = null;
            onExit = null;
            currentLevel = -1;
            simulating = false;
        }

        // ------------------------------------------------------------ masuk / keluar bagian

        /// <summary>Dipanggil SectionController saat pemain memilih Rakit WTP.</summary>
        public void Open(PlantController placedPlant, Action onExitSection)
        {
            plant = placedPlant;
            onExit = onExitSection;
            ShowLevelSelect();
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
            levelSelectPanel.SetActive(false);
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);
        }

        // Kembalikan WTP di AR ke kondisi sesuai progres Belajar.
        void RestorePlant()
        {
            if (plant != null) plant.SetWorkingUnitCount(GameManager.Instance.CompletedLevelCount, instant: true);
        }

        void ShowLevelSelect()
        {
            HideAll();
            RestorePlant();
            currentLevel = -1;
            PlayerData data = SaveSystem.Data;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool open = i < levels.Length && data.IsRakitLevelUnlocked(i);
                levelButtons[i].interactable = open;
                string stars = data.rakitStars[i] > 0 ? $"   Bintang {data.rakitStars[i]}/3" : "";
                levelLabels[i].text = i < levels.Length
                    ? $"{i + 1}. {levels[i].title}{(open ? stars : "  (terkunci)")}"
                    : "";
            }
            levelSelectPanel.SetActive(true);
        }

        void BackToLevelSelect()
        {
            if (simulating) return;
            ShowLevelSelect();
        }

        // ------------------------------------------------------------ mulai level

        void StartLevel(int levelIndex)
        {
            levelSelectPanel.SetActive(false);
            currentLevel = levelIndex;
            failedRuns = 0;
            trapUsed = false;

            // Ceritanya banjir menjebol WTP lama: semua unit di AR rusak dulu.
            plant.SetWorkingUnitCount(0, instant: true);
            dialogue.Play(Level.intro, PlayerName, BeginBuild);
        }

        void BeginBuild()
        {
            budgetLeft = Level.budget;
            selected = null;
            DealCards();
            for (int i = 0; i < slots.Length; i++) slots[i].SetCard(null, i + 1);

            titleText.text = $"Rakit WTP {currentLevel + 1}: {Level.title}";
            statusText.text = "Tap kartu, lalu tap slot. Slot 1 paling dekat sungai, slot 7 paling dekat kota.";
            infoText.text = "";
            topPanel.SetActive(true);
            bottomPanel.SetActive(true);
            controls.SetActive(true);
            Refresh();
        }

        // Acak urutan kartu (Fisher-Yates) supaya posisi kartu tidak membocorkan jawaban.
        void DealCards()
        {
            var deck = new List<CardData>(Level.cards);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < deck.Count;
                cards[i].gameObject.SetActive(used);
                if (used) cards[i].Bind(deck[i], Money(deck[i].cost));
            }
        }

        // ------------------------------------------------------------ interaksi

        void OnCard(UnitCard card)
        {
            if (simulating) return;
            if (selected != null) selected.SetSelected(false);
            selected = selected == card ? null : card;   // tap kartu yang sama = batal pilih

            if (selected == null)
            {
                infoText.text = "";
                return;
            }
            selected.SetSelected(true);
            CardData d = selected.Data;
            infoText.text = $"<b>{d.displayName}</b> ({Money(d.cost)})\n{d.description}";
        }

        void OnSlot(int index)
        {
            if (simulating) return;
            UnitSlot slot = slots[index];

            // Slot terisi -> kartunya kembali ke tangan, uangnya kembali.
            if (slot.Card != null)
            {
                budgetLeft += slot.Card.cost;
                SetCardVisible(slot.Card, true);
                statusText.text = $"{slot.Card.displayName} dilepas dari slot {index + 1}.";
                slot.SetCard(null, index + 1);
                Refresh();
                return;
            }

            if (selected == null)
            {
                statusText.text = "Pilih kartu dulu, baru tap slot.";
                return;
            }

            CardData card = selected.Data;
            if (card.cost > budgetLeft)
            {
                AudioManager.Play(Sfx.Wrong);
                statusText.text = $"Anggaran tidak cukup! {card.displayName} butuh {Money(card.cost)}, sisa {Money(budgetLeft)}.";
                return;
            }

            budgetLeft -= card.cost;
            slot.SetCard(card, index + 1);
            selected.SetSelected(false);
            selected.gameObject.SetActive(false);
            selected = null;
            infoText.text = "";
            if (card.kind == CardKind.Trap)
            {
                trapUsed = true;
                AudioManager.Play(Sfx.Wrong, 0.6f);   // celetukan Pak Wacana: kartu jebakan
            }
            statusText.text = !string.IsNullOrEmpty(card.placedLine)
                ? card.placedLine
                : $"{card.displayName} dipasang di slot {index + 1}.";
            Refresh();
        }

        void ClearSlots()
        {
            if (simulating) return;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Card == null) continue;
                budgetLeft += slots[i].Card.cost;
                SetCardVisible(slots[i].Card, true);
                slots[i].SetCard(null, i + 1);
            }
            statusText.text = "Semua slot dikosongkan.";
            Refresh();
        }

        void SetCardVisible(CardData data, bool visible)
        {
            foreach (UnitCard c in cards)
                if (c.Data == data) c.gameObject.SetActive(visible);
        }

        void Refresh()
        {
            budgetText.text = $"Sisa anggaran: {Money(budgetLeft)} dari {Money(Level.budget)}";
            bool full = true;
            foreach (UnitSlot s in slots)
                if (s.Card == null) full = false;
            flowButton.interactable = full;
        }

        // ------------------------------------------------------------ simulasi

        void OnFlow()
        {
            if (simulating) return;
            StartCoroutine(Simulate());
        }

        IEnumerator Simulate()
        {
            simulating = true;
            controls.SetActive(false);   // sembunyikan kartu supaya WTP di AR kelihatan

            var placed = new CardData[slots.Length];
            for (int i = 0; i < slots.Length; i++) placed[i] = slots[i].Card;
            FlowSimulation.Step[] steps = FlowSimulation.Run(placed, out string problem);

            // Mulai dari kondisi rusak, lalu air mengalir slot demi slot.
            plant.SetWorkingUnitCount(0, instant: true);
            for (int i = 0; i < steps.Length; i++)
            {
                slots[i].SetLook(UnitSlot.Look.Flowing);
                statusText.text = $"Slot {i + 1} ({placed[i].displayName}): {steps[i].message}";

                // Unit ke-i di model AR menampilkan air setelah slot ke-i.
                WTPUnit unit = plant.Units[i];
                unit.SetBroken(!steps[i].ok);
                unit.SetClarity(new WaterQuality(steps[i].turbidity).Clarity);
                AudioManager.Play(steps[i].ok ? Sfx.Splash : Sfx.Wrong);

                yield return new WaitForSeconds(StepDelay);
                slots[i].SetLook(steps[i].ok ? UnitSlot.Look.Good : UnitSlot.Look.Bad);
            }

            if (problem == null) yield return Success();
            else Fail(problem);
        }

        IEnumerator Success()
        {
            plant.SetWorkingUnitCount(FlowSimulation.SlotCount);   // kota menyala
            plant.Effects.CelebrateAll();
            AudioManager.Play(Sfx.Pour);
            statusText.text = "Air bersih sampai ke Kota Tirtapura!";
            yield return new WaitForSeconds(1.5f);

            int retryPoints = Mathf.Max(0, 25 - 10 * failedRuns);
            int score = 50 + (trapUsed ? 0 : 25) + retryPoints;
            int stars = ScoreManager.StarsForRatio((float)score / MaxScore);

            PlayerData data = SaveSystem.Data;
            data.SetRakitStars(currentLevel, stars);
            if (!trapUsed) BadgeManager.Award(BadgeManager.AntiGapura);
            BadgeManager.Evaluate();
            SaveSystem.Save();
            GameManager.Instance.SetState(GameState.Result);

            string detail = $"Berhasil: 50\n" +
                            $"Tanpa kartu jebakan: {(trapUsed ? 0 : 25)}/25\n" +
                            $"Percobaan gagal {failedRuns}x: {retryPoints}/25\n" +
                            $"Sisa anggaran: {Money(budgetLeft)}";
            simulating = false;
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);
            resultPanel.Show("WTP baru berdiri!", score, MaxScore, stars, detail, PlayOutro);
        }

        void PlayOutro()
        {
            resultPanel.Hide();
            GameManager.Instance.SetState(GameState.Playing);
            dialogue.Play(Level.outro, PlayerName, ShowLevelSelect);
        }

        void Fail(string problem)
        {
            failedRuns++;
            simulating = false;
            AudioManager.Play(Sfx.Fail);
            topPanel.SetActive(false);
            bottomPanel.SetActive(false);

            var hint = new[]
            {
                new DialogueLine { speaker = tirta, text = problem },
                new DialogueLine { speaker = tirta, text = "Slot merah belum bekerja. Tap kartunya untuk melepas, lalu susun ulang, ya {player}!" },
            };
            dialogue.Play(hint, PlayerName, () =>
            {
                // Warna merah/hijau slot sengaja dibiarkan supaya pemain tahu slot mana yang salah.
                plant.SetWorkingUnitCount(0, instant: true);
                statusText.text = "Perbaiki susunan, lalu alirkan air lagi.";
                topPanel.SetActive(true);
                bottomPanel.SetActive(true);
                controls.SetActive(true);
                Refresh();
            });
        }

        // ------------------------------------------------------------ format uang

        /// <summary>1500 -> "Rp 1,5 M", 500 -> "Rp 500 jt" (nilai dalam juta rupiah).</summary>
        public static string Money(int million)
        {
            if (Math.Abs(million) < 1000) return $"Rp {million} jt";
            float billion = million / 1000f;
            return "Rp " + billion.ToString(billion % 1f == 0f ? "0" : "0.0").Replace('.', ',') + " M";
        }
    }
}
