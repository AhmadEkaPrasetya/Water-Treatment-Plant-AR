using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WTP.Gameplay.Rakit
{
    /// <summary>
    /// Tombol kartu di tangan pemain (nama + harga). Semua kartu sengaja berwarna sama,
    /// jadi pemain harus menilai sendiri mana unit WTP asli dan mana kartu jebakan.
    /// </summary>
    public class UnitCard : MonoBehaviour
    {
        static readonly Color NormalColor = new Color(0.18f, 0.5f, 0.76f);
        static readonly Color SelectedColor = new Color(0.98f, 0.79f, 0.25f);
        static readonly Color NormalText = Color.white;
        static readonly Color SelectedText = new Color(0.08f, 0.14f, 0.2f);

        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] TMP_Text label;

        public Button Button => button;
        public CardData Data { get; private set; }

        public void Bind(CardData data, string costText)
        {
            Data = data;
            label.text = $"<b>{data.displayName}</b>\n<size=80%>{costText}</size>";
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            background.color = selected ? SelectedColor : NormalColor;
            label.color = selected ? SelectedText : NormalText;
        }
    }
}
