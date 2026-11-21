using System;
using UnityEngine;

namespace WTP.Progress
{
    /// <summary>Nama dan keterangan satu badge (disimpan di BadgeCatalog). Syaratnya ada di BadgeManager.</summary>
    [Serializable]
    public class BadgeInfo
    {
        [Tooltip("Harus sama dengan konstanta di BadgeManager, mis. anti_gapura")]
        public string id;
        public string title;
        [Tooltip("Cara mendapatkan badge ini")]
        [TextArea(1, 3)] public string description;
        public Color color = new Color(1f, 0.84f, 0.29f);
    }
}
