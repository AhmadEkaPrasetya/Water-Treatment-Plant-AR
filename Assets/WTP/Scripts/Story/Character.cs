using UnityEngine;

namespace WTP.Story
{
    /// <summary>
    /// Data satu tokoh cerita (Tirta, Pak Wacana, Pak Bakar Lahan).
    /// Buat aset baru: klik kanan di Project > Create > WTP > Character.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Character", fileName = "Character")]
    public class Character : ScriptableObject
    {
        public string displayName = "Tirta";
        [Tooltip("Warna label nama di kotak dialog")]
        public Color nameColor = new Color(0.2f, 0.6f, 1f);
        [Tooltip("Potret 2D (opsional)")]
        public Sprite portrait;
    }
}
