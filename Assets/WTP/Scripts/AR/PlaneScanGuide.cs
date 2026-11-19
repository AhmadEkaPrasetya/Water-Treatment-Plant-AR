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
        [Tooltip("Petunjuk cubit/putar, hilang sendiri supaya tidak menutupi panel lain")]
        [SerializeField] GameObject gestureHint;
        [SerializeField] float gestureHintSeconds = 6f;

        bool wasPlaced;
        float placedTime;

        void Update()
        {
            bool placed = placement.IsPlaced;
            bool hasPlane = placement.HasPlane;

            if (placed && !wasPlaced) placedTime = Time.unscaledTime; // catat saat WTP baru ditaruh
            wasPlaced = placed;

            SetActive(scanPanel, !placed && !hasPlane);
            SetActive(tapPanel, !placed && hasPlane);
            SetActive(placedPanel, placed);
            SetActive(gestureHint, placed && Time.unscaledTime - placedTime < gestureHintSeconds);
        }

        // Hanya ubah kalau statusnya beda, supaya tidak memicu rebuild UI tiap frame.
        static void SetActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active) panel.SetActive(active);
        }
    }
}
