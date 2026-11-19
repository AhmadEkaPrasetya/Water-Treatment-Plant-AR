using UnityEngine;

namespace WTP.AR
{
    /// <summary>
    /// Panduan di layar selama penempatan:
    /// - belum ada bidang  -> tampilkan scanPanel ("Arahkan kamera ke meja...")
    /// - bidang ditemukan  -> tampilkan tapPanel ("Tap bidang untuk menaruh WTP")
    /// - WTP sudah ditaruh -> sembunyikan keduanya, tampilkan placedPanel (tombol Taruh Ulang)
    /// </summary>
    public class PlaneScanGuide : MonoBehaviour
    {
        [SerializeField] ARPlacementManager placement;
        [SerializeField] GameObject scanPanel;
        [SerializeField] GameObject tapPanel;
        [SerializeField] GameObject placedPanel;

        void Update()
        {
            bool placed = placement.IsPlaced;
            bool hasPlane = placement.HasPlane;

            SetActive(scanPanel, !placed && !hasPlane);
            SetActive(tapPanel, !placed && hasPlane);
            SetActive(placedPanel, placed);
        }

        // Hanya ubah kalau statusnya beda, supaya tidak memicu rebuild UI tiap frame.
        static void SetActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active) panel.SetActive(active);
        }
    }
}
