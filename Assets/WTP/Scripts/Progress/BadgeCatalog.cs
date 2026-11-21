using UnityEngine;

namespace WTP.Progress
{
    /// <summary>
    /// Daftar semua badge yang bisa dikumpulkan (urutan = urutan tampil di layar Koleksi Badge).
    /// Aset: Assets/WTP/Data/Badges/BadgeCatalog.asset. Buat baru: Create > WTP > Badge Catalog.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Badge Catalog", fileName = "BadgeCatalog")]
    public class BadgeCatalog : ScriptableObject
    {
        public BadgeInfo[] badges;

        public BadgeInfo Find(string id)
        {
            foreach (BadgeInfo b in badges)
                if (b.id == id) return b;
            return null;
        }
    }
}
