using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Progress;

namespace WTP.UI.Panels
{
    /// <summary>
    /// Layar Leaderboard di menu utama: dua tab (Detektif / Operator Shift), 10 skor teratas.
    /// Baris milik pemain yang sedang main diberi warna kuning.
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        static readonly Color TabOn = new Color(0.98f, 0.79f, 0.25f);
        static readonly Color TabOff = new Color(0.18f, 0.5f, 0.76f);
        static readonly Color TabTextOn = new Color(0.08f, 0.14f, 0.2f);
        static readonly Color RowNormal = Color.white;
        static readonly Color RowPlayer = new Color(1f, 0.84f, 0.29f);

        [SerializeField] GameObject panel;
        [SerializeField] Button detektifTab;
        [SerializeField] Button shiftTab;
        [SerializeField] TMP_Text noteText;
        [Tooltip("Nama per baris (1..10)")]
        [SerializeField] TMP_Text[] nameTexts;
        [Tooltip("Skor per baris (1..10)")]
        [SerializeField] TMP_Text[] scoreTexts;
        [SerializeField] Button closeButton;

        void Awake()
        {
            detektifTab.onClick.AddListener(() => ShowCategory(LeaderboardCategory.Detektif));
            shiftTab.onClick.AddListener(() => ShowCategory(LeaderboardCategory.Shift));
            closeButton.onClick.AddListener(Hide);
            panel.SetActive(false);
        }

        public void Show()
        {
            ShowCategory(LeaderboardCategory.Detektif);
            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);

        void ShowCategory(LeaderboardCategory category)
        {
            SetTab(detektifTab, category == LeaderboardCategory.Detektif);
            SetTab(shiftTab, category == LeaderboardCategory.Shift);
            noteText.text = category == LeaderboardCategory.Detektif
                ? "Total skor terbaik 6 kasus Detektif (maks 600)"
                : "Skor terbaik Operator Shift";

            string me = SaveSystem.Data.playerName;
            List<LeaderboardEntry> top = LeaderboardManager.GetTop(category, nameTexts.Length);
            for (int i = 0; i < nameTexts.Length; i++)
            {
                bool has = i < top.Count;
                nameTexts[i].transform.parent.gameObject.SetActive(has);   // sembunyikan baris kosong
                nameTexts[i].text = has ? $"{i + 1}. {top[i].name}" : "";
                scoreTexts[i].text = has ? top[i].score.ToString() : "";
                Color c = has && !top[i].npc && top[i].name == me ? RowPlayer : RowNormal;
                nameTexts[i].color = c;
                scoreTexts[i].color = c;
            }
        }

        static void SetTab(Button tab, bool on)
        {
            tab.image.color = on ? TabOn : TabOff;
            tab.GetComponentInChildren<TMP_Text>().color = on ? TabTextOn : Color.white;
        }
    }
}
