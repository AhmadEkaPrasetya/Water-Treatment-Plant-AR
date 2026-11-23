using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using WTP.Audio;

namespace WTP.AR
{
    /// <summary>
    /// Mengatur penempatan WTP di dunia nyata:
    /// 1. ARPlaneManager memindai bidang datar (meja/lantai).
    /// 2. Pemain tap layar -> raycast ke bidang -> WTP muncul di titik itu.
    /// 3. ResetPlacement() menghapus WTP supaya bisa ditaruh ulang.
    /// </summary>
    public class ARPlacementManager : MonoBehaviour
    {
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] Camera arCamera;
        [SerializeField] GameObject plantPrefab;

        [Tooltip("Matikan pemindaian & sembunyikan bidang setelah WTP ditaruh (hemat baterai, tampilan lebih bersih)")]
        [SerializeField] bool hidePlanesAfterPlacement = true;

        /// <summary>Dipanggil saat WTP berhasil ditaruh. Dipakai GameManager / UI nanti.</summary>
        public event Action<GameObject> PlantPlaced;
        /// <summary>Dipanggil saat WTP dihapus lewat tombol reset.</summary>
        public event Action PlacementReset;

        public GameObject PlacedPlant { get; private set; }
        public bool IsPlaced => PlacedPlant != null;
        public bool HasPlane => planeManager != null && planeManager.trackables.count > 0;

        // Dipakai ulang tiap frame supaya tidak membuat list baru (mengurangi garbage).
        static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        void Reset()
        {
            // Isi otomatis referensi saat script ditambahkan di Inspector.
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
            planeManager = FindFirstObjectByType<ARPlaneManager>();
            arCamera = Camera.main;
        }

        void Update()
        {
            if (IsPlaced) return;
            if (!TryGetTap(out Vector2 screenPos)) return;

            // Tembakkan sinar dari titik tap ke bidang yang sudah terdeteksi.
            if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
                Place(hits[0].pose);
        }

        void Place(Pose pose)
        {
            // Putar WTP supaya sisi depannya (sisi -Z: Reservoir + kota) menghadap kamera.
            Vector3 toCamera = arCamera.transform.position - pose.position;
            toCamera.y = 0f;
            Quaternion rotation = toCamera.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(-toCamera.normalized, Vector3.up)
                : pose.rotation;

            PlacedPlant = Instantiate(plantPrefab, pose.position, rotation);
            AudioManager.Play(Sfx.Place);

            if (hidePlanesAfterPlacement) SetPlanesActive(false);
            PlantPlaced?.Invoke(PlacedPlant);
        }

        /// <summary>Hapus WTP dan mulai pindai lagi. Dipanggil dari tombol "Taruh Ulang".</summary>
        public void ResetPlacement()
        {
            if (PlacedPlant != null) Destroy(PlacedPlant);
            PlacedPlant = null;
            SetPlanesActive(true);
            PlacementReset?.Invoke();
        }

        void SetPlanesActive(bool active)
        {
            planeManager.enabled = active;
            foreach (ARPlane plane in planeManager.trackables)
                plane.gameObject.SetActive(active);
        }

        /// <summary>
        /// True di frame saat layar baru disentuh (atau mouse diklik di Editor),
        /// kecuali sentuhannya mengenai tombol UI.
        /// </summary>
        static bool TryGetTap(out Vector2 position)
        {
            position = default;
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return false;

            position = pointer.position.ReadValue();
            return !IsOverUI(position);
        }

        static bool IsOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screenPos };
            uiHits.Clear();
            EventSystem.current.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }
    }
}
