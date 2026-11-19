using System;
using System.Collections.Generic;
using UnityEngine;

namespace WTP.Plant
{
    /// <summary>
    /// Satu unit WTP di dunia AR. Dipasang otomatis oleh WTPPlantBuilder di setiap model unit.
    /// Mengatur bagian model yang namanya sudah disepakati:
    ///   "Water"          -> warna air keruh/jernih (_Clarity di shader WTP/Water)
    ///   "*_Rotor"        -> pengaduk yang berputar selama unit berjalan
    ///   "LevelIndicator" -> tinggi air reservoir (skala Y)
    /// </summary>
    public class WTPUnit : MonoBehaviour
    {
        static readonly int ClarityId = Shader.PropertyToID("_Clarity");

        [SerializeField] UnitType unitType;
        [SerializeField] string displayName;

        [Tooltip("Kecepatan putar rotor (derajat per detik)")]
        [SerializeField] float rotorSpeed = 120f;
        [Tooltip("Seberapa cepat air berubah warna (kejernihan per detik)")]
        [SerializeField] float clarityChangeSpeed = 0.5f;

        public UnitType Type => unitType;
        public string DisplayName => displayName;
        public bool IsBroken { get; private set; }
        public float Clarity => currentClarity;

        /// <summary>Dipanggil saat unit rusak / diperbaiki (dipakai UI & Detektif).</summary>
        public event Action<WTPUnit> StateChanged;

        readonly List<Renderer> waterRenderers = new List<Renderer>();
        readonly List<Transform> rotors = new List<Transform>();
        Transform levelIndicator;
        MaterialPropertyBlock block;

        float currentClarity = 1f;
        float targetClarity = 1f;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Water" && t.TryGetComponent(out Renderer r)) waterRenderers.Add(r);
                else if (t.name.EndsWith("_Rotor")) rotors.Add(t);
                else if (t.name == "LevelIndicator") levelIndicator = t;
            }
            ApplyClarity();
        }

        void Update()
        {
            // Rotor hanya berputar kalau unit tidak rusak.
            if (!IsBroken)
                foreach (Transform rotor in rotors)
                    rotor.Rotate(0f, rotorSpeed * Time.deltaTime, 0f, Space.Self);

            // Ubah warna air pelan-pelan menuju target supaya perubahannya terlihat.
            if (!Mathf.Approximately(currentClarity, targetClarity))
            {
                currentClarity = Mathf.MoveTowards(currentClarity, targetClarity, clarityChangeSpeed * Time.deltaTime);
                ApplyClarity();
            }
        }

        /// <summary>Atur kejernihan air unit ini (0 keruh .. 1 jernih).</summary>
        public void SetClarity(float clarity, bool instant = false)
        {
            targetClarity = Mathf.Clamp01(clarity);
            if (instant)
            {
                currentClarity = targetClarity;
                ApplyClarity();
            }
        }

        public void SetBroken(bool broken)
        {
            if (IsBroken == broken) return;
            IsBroken = broken;
            StateChanged?.Invoke(this);
        }

        /// <summary>Tinggi air di indikator reservoir (0 kosong .. 1 penuh). Unit lain mengabaikan.</summary>
        public void SetWaterLevel(float level)
        {
            if (levelIndicator == null) return;
            Vector3 s = levelIndicator.localScale;
            levelIndicator.localScale = new Vector3(s.x, Mathf.Clamp01(level), s.z);
        }

        // MaterialPropertyBlock: ubah nilai shader untuk renderer ini saja,
        // tanpa membuat salinan material (material "Water" tetap dipakai bersama).
        void ApplyClarity()
        {
            foreach (Renderer r in waterRenderers)
            {
                r.GetPropertyBlock(block);
                block.SetFloat(ClarityId, currentClarity);
                r.SetPropertyBlock(block);
            }
        }
    }
}
