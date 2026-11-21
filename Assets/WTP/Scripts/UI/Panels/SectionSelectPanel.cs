using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WTP.UI.Panels
{
    /// <summary>
    /// Layar pilih bagian (Belajar / Rakit WTP / Detektif / Operator Shift) setelah WTP ditaruh.
    /// Tombol bagian yang terkunci tetap bisa di-tap supaya pemain diberi tahu cara membukanya.
    /// </summary>
    public class SectionSelectPanel : MonoBehaviour
    {
        static readonly Color OpenColor = new Color(0.18f, 0.5f, 0.76f, 0.95f);
        static readonly Color LockedColor = new Color(0.35f, 0.38f, 0.42f, 0.9f);

        [SerializeField] GameObject panel;
        [SerializeField] Button[] sectionButtons;
        [SerializeField] TMP_Text[] sectionLabels;
        [SerializeField] TMP_Text statusText;

        Action<int> onChosen;

        void Awake()
        {
            for (int i = 0; i < sectionButtons.Length; i++)
            {
                int index = i;
                sectionButtons[i].onClick.AddListener(() => onChosen?.Invoke(index));
            }
        }

        public void Show(string[] titles, bool[] unlocked, Action<int> onSectionChosen)
        {
            onChosen = onSectionChosen;
            for (int i = 0; i < sectionButtons.Length; i++)
            {
                bool exists = i < titles.Length;
                sectionButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                sectionButtons[i].image.color = unlocked[i] ? OpenColor : LockedColor;
                sectionLabels[i].text = titles[i] + (unlocked[i] ? "" : "  (terkunci)");
            }
            statusText.text = "";
            panel.SetActive(true);
        }

        public void SetStatus(string message) => statusText.text = message;

        public void Hide() => panel.SetActive(false);
    }
}
