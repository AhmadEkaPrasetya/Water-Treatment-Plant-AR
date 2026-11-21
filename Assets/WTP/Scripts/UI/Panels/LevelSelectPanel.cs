using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WTP.UI.Panels
{
    /// <summary>
    /// Daftar 7 level Bagian 1. Tiap baris: tombol nama unit + bintang. Level terkunci abu-abu.
    /// Tombol Kembali menutup daftar dan kembali ke layar Pilih Bagian.
    /// Panel ini hanya menampilkan; isi & aksi diberikan oleh pemanggil lewat Show().
    /// </summary>
    public class LevelSelectPanel : MonoBehaviour
    {
        static readonly Color UnlockedColor = new Color(0.18f, 0.5f, 0.76f, 0.95f);
        static readonly Color DoneColor = new Color(0.2f, 0.62f, 0.4f, 0.95f);
        static readonly Color LockedColor = new Color(0.35f, 0.38f, 0.42f, 0.9f);
        static readonly Color StarOn = new Color(1f, 0.84f, 0.29f);
        static readonly Color StarOff = new Color(1f, 1f, 1f, 0.25f);

        [SerializeField] GameObject panel;
        [SerializeField] Button[] levelButtons;
        [SerializeField] TMP_Text[] levelLabels;
        [Tooltip("3 gambar bintang per level, berurutan (level 1: 0-2, level 2: 3-5, ...)")]
        [SerializeField] Image[] starImages;
        [SerializeField] Button backButton;

        Action<int> onChosen;
        Action onBack;

        void Awake()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int index = i;
                levelButtons[i].onClick.AddListener(() => onChosen?.Invoke(index));
            }
            if (backButton != null) backButton.onClick.AddListener(() => onBack?.Invoke());
        }

        public void Show(string[] titles, int[] stars, bool[] unlocked, Action<int> onLevelChosen, Action onBackClicked)
        {
            onChosen = onLevelChosen;
            onBack = onBackClicked;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool open = i < titles.Length && unlocked[i];
                levelButtons[i].interactable = open;
                levelButtons[i].image.color = !open ? LockedColor : stars[i] > 0 ? DoneColor : UnlockedColor;
                levelLabels[i].text = i < titles.Length ? titles[i] + (open ? "" : "  (terkunci)") : "";
                for (int s = 0; s < 3; s++)
                    starImages[i * 3 + s].color = s < stars[i] ? StarOn : StarOff;
            }
            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);
    }
}
