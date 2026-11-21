using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WTP.Gameplay.Rakit
{
    /// <summary>Satu slot di jalur WTP (slot 1 = paling dekat sungai, slot 7 = paling dekat kota).</summary>
    public class UnitSlot : MonoBehaviour
    {
        public enum Look { Empty, Filled, Flowing, Good, Bad }

        static readonly Color EmptyColor = new Color(0.08f, 0.2f, 0.3f);
        static readonly Color FilledColor = new Color(0.18f, 0.5f, 0.76f);
        static readonly Color FlowingColor = new Color(0.2f, 0.75f, 0.95f);
        static readonly Color GoodColor = new Color(0.2f, 0.62f, 0.4f);
        static readonly Color BadColor = new Color(0.85f, 0.3f, 0.25f);

        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] TMP_Text numberText;
        [SerializeField] TMP_Text nameText;

        public Button Button => button;
        /// <summary>Kartu yang terpasang, null kalau kosong.</summary>
        public CardData Card { get; private set; }

        public void SetCard(CardData card, int slotNumber)
        {
            Card = card;
            numberText.text = slotNumber.ToString();
            nameText.text = card != null ? card.displayName : "kosong";
            SetLook(card != null ? Look.Filled : Look.Empty);
        }

        public void SetLook(Look look)
        {
            switch (look)
            {
                case Look.Empty: background.color = EmptyColor; break;
                case Look.Filled: background.color = FilledColor; break;
                case Look.Flowing: background.color = FlowingColor; break;
                case Look.Good: background.color = GoodColor; break;
                default: background.color = BadColor; break;
            }
        }
    }
}
