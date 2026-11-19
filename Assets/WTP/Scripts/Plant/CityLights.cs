using System.Collections.Generic;
using UnityEngine;

namespace WTP.Plant
{
    /// <summary>
    /// Lampu Kota Tirtapura: makin banyak unit WTP berfungsi, makin banyak rumah yang jendelanya menyala
    /// (air bersih sudah sampai ke rumah itu). Semua menyala kalau ke-7 unit berfungsi.
    /// Rumah yang baru menyala diberi efek "membal" kecil supaya pemain sadar ada perubahan.
    /// Dipasang di objek "City" prefab WTP_Plant oleh WTPPlantBuilder.
    /// </summary>
    public class CityLights : MonoBehaviour
    {
        [SerializeField] Material darkWindow;
        [SerializeField] Material litWindow;
        [SerializeField] float popDuration = 0.6f;

        readonly List<Renderer> windows = new List<Renderer>();
        readonly List<Vector3> baseScales = new List<Vector3>();
        int litCount;
        int popIndex = -1;   // rumah pertama yang sedang dianimasikan
        float popTime;

        void Awake()
        {
            // Urutan anak = House_1 .. House_6, masing-masing punya "Window".
            foreach (Transform house in transform)
            {
                Transform w = house.Find("Window");
                if (w == null || !w.TryGetComponent(out Renderer r)) continue;
                windows.Add(r);
                baseScales.Add(house.localScale);
            }
        }

        /// <summary>Nyalakan rumah sesuai jumlah unit yang berfungsi dari totalUnits.</summary>
        public void SetWorkingUnits(int workingUnits, int totalUnits, bool instant)
        {
            int target = workingUnits >= totalUnits ? windows.Count : workingUnits * windows.Count / totalUnits;
            if (!instant && target > litCount)
            {
                popIndex = litCount;          // animasikan rumah yang baru menyala saja
                popTime = 0f;
            }
            litCount = target;
            for (int i = 0; i < windows.Count; i++)
                windows[i].sharedMaterial = i < litCount ? litWindow : darkWindow;
        }

        void Update()
        {
            if (popIndex < 0) return;

            popTime += Time.deltaTime;
            float t = Mathf.Clamp01(popTime / popDuration);
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;   // naik lalu kembali ke 1
            for (int i = popIndex; i < litCount; i++)
                windows[i].transform.parent.localScale = baseScales[i] * bounce;

            if (t >= 1f) popIndex = -1;
        }
    }
}
