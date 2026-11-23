using System;
using UnityEngine;
using WTP.Plant;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Kelas dasar (abstract) semua mini-game Bagian 1. Tiap unit punya turunannya sendiri
    /// (IntakeGame, CoagulationGame, ...). Turunan wajib:
    ///   - mengisi Begin(): siapkan UI / objek mini-game untuk unit tersebut
    ///   - memanggil Finish(skor 0-100) saat mini-game selesai
    /// LevelController tidak perlu tahu isi mini-game, cukup Begin() lalu tunggu Finished.
    /// </summary>
    public abstract class MiniGame : MonoBehaviour
    {
        /// <summary>Dipanggil dengan skor 0-100 saat mini-game selesai.</summary>
        public event Action<int> Finished;

        protected WTPUnit Unit { get; private set; }
        /// <summary>Efek partikel WTP (flok, endapan, kuman). Otomatis dimatikan saat mini-game selesai.</summary>
        protected PlantEffects Effects { get; private set; }

        public void Begin(WTPUnit unit)
        {
            Unit = unit;
            Effects = unit.GetComponentInParent<PlantController>().Effects;
            gameObject.SetActive(true);
            OnBegin();
        }

        /// <summary>Isi persiapan mini-game di sini.</summary>
        protected abstract void OnBegin();

        /// <summary>Hentikan paksa (mis. WTP ditaruh ulang). Turunan boleh override untuk bersih-bersih.</summary>
        public virtual void Cancel()
        {
            if (Effects != null) Effects.StopAll();
            gameObject.SetActive(false);
        }

        protected void Finish(int score)
        {
            if (Effects != null) Effects.StopAll();
            gameObject.SetActive(false);
            Finished?.Invoke(Mathf.Clamp(score, 0, 100));
        }
    }
}
