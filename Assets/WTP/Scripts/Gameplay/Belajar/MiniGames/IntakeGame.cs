using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using WTP.Audio;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Mini-game Intake: sampah hanyut di sungai menuju mulut intake.
    /// Pemain men-tap sampah (objek 3D di dunia AR) untuk membuangnya sebelum masuk intake.
    /// Skor = persentase sampah yang berhasil dibuang.
    /// </summary>
    public class IntakeGame : MiniGame
    {
        [Header("UI")]
        [SerializeField] TMP_Text counterText;
        [SerializeField] TMP_Text statusText;

        [Header("Sampah")]
        [Tooltip("Material sampah (botol, plastik, kardus, ranting), dipilih acak")]
        [SerializeField] Material[] trashMaterials;
        [SerializeField] int trashCount = 10;
        [SerializeField] float spawnInterval = 1.6f;
        [Tooltip("Kecepatan hanyut dalam satuan lokal model (1 satuan = 5 cm di meja)")]
        [SerializeField] Vector2 speedRange = new Vector2(1.3f, 2f);

        // Posisi dalam koordinat "Content" prefab WTP_Plant (lihat WTPPlantBuilder):
        // sungai di z = 4.8, mulut intake di x = 6.3.
        const float RiverZ = 4.8f;
        const float SurfaceY = 0.15f;
        const float IntakeX = 6.3f;
        const float SpawnMinX = -6f, SpawnMaxX = 0f;

        readonly List<Transform> trash = new List<Transform>();
        readonly List<float> speeds = new List<float>();
        Transform content;
        int spawned, removed, missed;
        float spawnTimer;
        bool running;

        protected override void OnBegin()
        {
            // Unit ada di Content/Units/WTP_01_Intake -> naik dua tingkat ke Content.
            content = Unit.transform.parent.parent;
            spawned = removed = missed = 0;
            spawnTimer = 0f;
            running = true;
            statusText.text = "Tap sampah sebelum masuk ke intake!";
            UpdateCounter();
        }

        void Update()
        {
            if (!running) return;

            spawnTimer -= Time.deltaTime;
            if (spawned < trashCount && spawnTimer <= 0f)
            {
                Spawn();
                spawnTimer = spawnInterval;
            }

            // Gerakkan sampah ke arah intake + efek terapung naik-turun.
            for (int i = trash.Count - 1; i >= 0; i--)
            {
                Transform t = trash[i];
                Vector3 p = t.localPosition;
                p.x += speeds[i] * Time.deltaTime;
                p.y = SurfaceY + Mathf.Sin(Time.time * 3f + i) * 0.05f;
                t.localPosition = p;
                t.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);

                if (p.x >= IntakeX)
                {
                    missed++;
                    statusText.text = "Ada sampah yang lolos ke intake!";
                    RemoveAt(i);
                    UpdateCounter();
                }
            }

            HandleTap();

            if (spawned >= trashCount && trash.Count == 0) End();
        }

        void HandleTap()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            Vector2 screenPos = pointer.position.ReadValue();
            if (IsOverUI(screenPos)) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // RaycastAll: model WTP tidak punya collider, tapi tetap aman kalau nanti ada.
            foreach (RaycastHit hit in Physics.RaycastAll(cam.ScreenPointToRay(screenPos), 50f))
            {
                int index = trash.IndexOf(hit.transform);
                if (index < 0) continue;
                removed++;
                AudioManager.Play(Sfx.Splash);
                statusText.text = "Bagus! Sampah dibuang.";
                RemoveAt(index);
                UpdateCounter();
                return;
            }
        }

        static bool IsOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            return results.Count > 0;
        }

        void Spawn()
        {
            spawned++;

            // Induk kosong dengan collider bola besar supaya gampang di-tap di HP.
            var root = new GameObject("Trash");
            root.transform.SetParent(content, false);
            root.transform.localPosition = new Vector3(Random.Range(SpawnMinX, SpawnMaxX), SurfaceY,
                                                       RiverZ + Random.Range(-0.4f, 0.4f));
            root.AddComponent<SphereCollider>().radius = 1.1f;

            // Bentuk acak dari primitif Unity (botol, kantong, kardus, ranting).
            PrimitiveType[] shapes = { PrimitiveType.Capsule, PrimitiveType.Sphere, PrimitiveType.Cube, PrimitiveType.Cylinder };
            Vector3[] scales = { new Vector3(0.4f, 0.45f, 0.4f), new Vector3(0.8f, 0.3f, 0.7f),
                                 new Vector3(0.7f, 0.45f, 0.7f), new Vector3(0.18f, 0.6f, 0.18f) };
            int kind = Random.Range(0, shapes.Length);
            GameObject visual = GameObject.CreatePrimitive(shapes[kind]);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = scales[kind];
            visual.transform.localRotation = Quaternion.Euler(kind == 0 || kind == 3 ? 90f : 0f, 0f, 0f);
            if (trashMaterials != null && trashMaterials.Length > 0)
                visual.GetComponent<Renderer>().sharedMaterial = trashMaterials[kind % trashMaterials.Length];

            trash.Add(root.transform);
            speeds.Add(Random.Range(speedRange.x, speedRange.y));
            UpdateCounter();
        }

        void RemoveAt(int index)
        {
            Destroy(trash[index].gameObject);
            trash.RemoveAt(index);
            speeds.RemoveAt(index);
        }

        void UpdateCounter() =>
            counterText.text = $"Dibuang: {removed}   Lolos: {missed}   Sisa: {trashCount - removed - missed}";

        void End()
        {
            running = false;
            Finish(Mathf.RoundToInt(100f * removed / trashCount));
        }

        public override void Cancel()
        {
            running = false;
            ClearTrash();
            base.Cancel();
        }

        void ClearTrash()
        {
            foreach (Transform t in trash)
                if (t != null) Destroy(t.gameObject);
            trash.Clear();
            speeds.Clear();
        }

        void OnDisable() => ClearTrash();
    }
}
