using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Progress;

namespace WTP.UI.Panels
{
    /// <summary>
    /// Layar Koleksi Badge di menu utama: semua badge dari BadgeCatalog.
    /// Badge yang sudah didapat berwarna, yang belum abu-abu (keterangannya tetap terlihat
    /// supaya pemain tahu cara mendapatkannya).
    /// </summary>
    public class BadgeCollectionPanel : MonoBehaviour
    {
        static readonly Color LockedIcon = new Color(0.3f, 0.33f, 0.37f);
        static readonly Color LockedText = new Color(1f, 1f, 1f, 0.45f);

        [SerializeField] GameObject panel;
        [SerializeField] BadgeCatalog catalog;
        [SerializeField] TMP_Text countText;
        [Tooltip("Satu baris per badge, urutan sama dengan catalog")]
        [SerializeField] Image[] icons;
        [SerializeField] TMP_Text[] iconLetters;
        [SerializeField] TMP_Text[] titles;
        [SerializeField] TMP_Text[] descriptions;
        [SerializeField] Button closeButton;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            panel.SetActive(false);
        }

        public void Show()
        {
            PlayerData data = SaveSystem.Data;
            int earned = 0;
            for (int i = 0; i < icons.Length; i++)
            {
                bool exists = i < catalog.badges.Length;
                icons[i].transform.parent.gameObject.SetActive(exists);   // baris badge
                if (!exists) continue;

                BadgeInfo b = catalog.badges[i];
                bool has = data.badges.Contains(b.id);
                if (has) earned++;
                icons[i].color = has ? b.color : LockedIcon;
                iconLetters[i].text = has ? b.title.Substring(0, 1) : "?";
                titles[i].text = b.title;
                titles[i].color = has ? Color.white : LockedText;
                descriptions[i].text = b.description;
                descriptions[i].color = has ? new Color(0.7f, 0.9f, 1f) : LockedText;
            }
            countText.text = $"{earned} dari {catalog.badges.Length} badge terkumpul";
            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);
    }
}
