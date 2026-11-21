using UnityEngine;
using WTP.Gameplay.Belajar;
using WTP.Gameplay.Detektif;
using WTP.Gameplay.Rakit;
using WTP.Gameplay.Shift;
using WTP.Plant;
using WTP.Progress;
using WTP.Story;
using WTP.UI.Panels;

namespace WTP.Core
{
    /// <summary>
    /// Pengatur bagian di scene ARPlay:
    ///   WTP ditaruh -> (prolog, hanya pertama kali) -> layar Pilih Bagian
    ///   -> bagian yang dipilih berjalan -> tombol Kembali di bagian itu -> Pilih Bagian lagi.
    /// Aturan buka-kunci: Rakit terbuka setelah 7 level Belajar selesai,
    /// Detektif setelah Rakit selesai, Operator Shift setelah Detektif selesai.
    /// </summary>
    public class SectionController : MonoBehaviour
    {
        enum Section { Belajar, Rakit, Detektif, Shift }

        static readonly string[] Titles = { "1. Belajar", "2. Rakit WTP", "3. Detektif", "4. Operator Shift" };

        [SerializeField] DialogueScene prologue;
        [SerializeField] DialogueManager dialogue;
        [SerializeField] SectionSelectPanel sectionPanel;

        [Header("Bagian")]
        [SerializeField] LevelController belajar;
        [SerializeField] AssemblyManager rakit;
        [SerializeField] DetectiveManager detektif;
        [SerializeField] ShiftManager shift;

        PlantController plant;

        static string PlayerName => SaveSystem.Data.HasName ? SaveSystem.Data.playerName : "kawan";

        // Start, bukan OnEnable: GameManager dibuat otomatis setelah Awake/OnEnable scene pertama.
        void Start()
        {
            GameManager.Instance.PlantReady += OnPlantReady;
            GameManager.Instance.StateChanged += OnStateChanged;
            sectionPanel.Hide();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.PlantReady -= OnPlantReady;
            GameManager.Instance.StateChanged -= OnStateChanged;
        }

        void OnPlantReady(PlantController placedPlant)
        {
            plant = placedPlant;
            if (!SaveSystem.Data.prologueSeen && prologue != null)
            {
                dialogue.Play(prologue.lines, PlayerName, () =>
                {
                    SaveSystem.Data.prologueSeen = true;
                    SaveSystem.Save();
                    ShowMenu();
                });
            }
            else
            {
                ShowMenu();
            }
        }

        // WTP ditaruh ulang: tiap bagian membatalkan dirinya sendiri, di sini cukup tutup menu.
        void OnStateChanged(GameState state)
        {
            if (state != GameState.Scanning) return;
            dialogue.Stop();
            sectionPanel.Hide();
            plant = null;
        }

        void ShowMenu()
        {
            if (plant == null) return;
            PlayerData data = SaveSystem.Data;
            bool[] unlocked =
            {
                true,
                data.IsBelajarComplete,
                data.IsRakitComplete,
                data.IsDetektifComplete,
            };
            sectionPanel.Show(Titles, unlocked, section => OnSectionChosen((Section)section, unlocked[section]));
        }

        void OnSectionChosen(Section section, bool unlocked)
        {
            if (!unlocked)
            {
                sectionPanel.SetStatus(LockedHint(section));
                return;
            }

            switch (section)
            {
                case Section.Belajar:
                    sectionPanel.Hide();
                    belajar.Open(plant, ShowMenu);
                    break;
                case Section.Rakit:
                    sectionPanel.Hide();
                    rakit.Open(plant, ShowMenu);
                    break;
                case Section.Detektif:
                    sectionPanel.Hide();
                    detektif.Open(plant, ShowMenu);
                    break;
                case Section.Shift:
                    sectionPanel.Hide();
                    shift.Open(plant, ShowMenu);
                    break;
            }
        }

        static string LockedHint(Section section)
        {
            switch (section)
            {
                case Section.Rakit: return "Selesaikan ke-7 level Belajar dulu untuk membuka Rakit WTP.";
                case Section.Detektif: return "Selesaikan ke-3 level Rakit WTP dulu untuk membuka Detektif.";
                default: return "Selesaikan ke-6 kasus Detektif dulu untuk membuka Operator Shift.";
            }
        }
    }
}
