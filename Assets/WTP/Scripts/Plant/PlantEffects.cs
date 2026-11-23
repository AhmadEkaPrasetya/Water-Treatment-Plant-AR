using UnityEngine;

namespace WTP.Plant
{
    /// <summary>
    /// Efek partikel di model WTP AR (semua dibuat lewat kode, hanya butuh satu material):
    ///   Flok      - gumpalan kotoran berputar di bak Flokulasi, makin besar saat diaduk pas
    ///   Endapan   - butiran lumpur turun pelan di bak Sedimentasi
    ///   Kuman     - titik hijau bergoyang di bak Desinfeksi (hilang kalau klorin cukup)
    ///   Abu       - abu karhutla turun dari langit di atas seluruh WTP
    ///   Kilau     - semburan kilau biru saat unit selesai diperbaiki
    /// Dipasang di root prefab WTP_Plant (menu WTP > Setup Audio & Effects).
    ///
    /// Ukuran & kecepatan ditulis dalam meter "asli" model (sebelum diperkecil jadi maket),
    /// karena partikel memakai skala hierarki: ikut mengecil/membesar saat WTP dicubit.
    /// </summary>
    [RequireComponent(typeof(PlantController))]
    public class PlantEffects : MonoBehaviour
    {
        [Tooltip("Material partikel (Assets/WTP/Materials/Particle.mat)")]
        [SerializeField] Material particleMaterial;

        static readonly Color FlocColor = new Color(0.9f, 0.82f, 0.62f, 0.95f);   // krem terang: kontras dengan air keruh
        static readonly Color SludgeColor = new Color(0.62f, 0.47f, 0.3f, 0.9f);
        static readonly Color GermColor = new Color(0.35f, 0.9f, 0.3f, 0.9f);
        static readonly Color AshColor = new Color(0.55f, 0.53f, 0.5f, 0.85f);
        static readonly Color SparkleColor = new Color(0.6f, 0.9f, 1f, 1f);

        PlantController plant;
        // Dibuat saat pertama kali dipakai (lazy), supaya ukuran dihitung setelah WTP ditaruh.
        ParticleSystem floc, sludge, germs, ash;
        readonly ParticleSystem[] sparkles = new ParticleSystem[7];

        void Awake() => plant = GetComponent<PlantController>();

        // ------------------------------------------------------------ API

        /// <summary>Flok di bak Flokulasi. size: 0 = tidak ada, 1 = flok besar. stir = kecepatan putar (derajat/detik).</summary>
        public void SetFloc(float size, float stir = 90f)
        {
            if (floc == null)
            {
                floc = CreateInUnit(UnitType.Flokulasi, "Fx_Floc", FlocColor, 0.15f, 0.2f);
                // Sumber berbentuk lingkaran mendatar: partikel yang berputar (orbit) tetap di dalam bak,
                // kalau kotak, partikel di pojok akan berputar keluar dinding.
                ParticleSystem.ShapeModule shape = floc.shape;
                float radius = Mathf.Min(shape.scale.x, shape.scale.z) * 0.5f;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = radius;
                shape.rotation = new Vector3(90f, 0f, 0f);   // lingkaran Unity berdiri tegak, direbahkan
                shape.scale = Vector3.one;
            }
            ParticleSystem.MainModule main = floc.main;
            main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.04f, 0.12f, size), Mathf.Lerp(0.07f, 0.3f, size));
            // Kecepatan orbit (radian/detik) di sekitar sumbu Y = air ikut berputar.
            ParticleSystem.VelocityOverLifetimeModule vel = floc.velocityOverLifetime;
            vel.orbitalY = stir * Mathf.Deg2Rad * 0.5f;
            SetRate(floc, size > 0.01f ? 25f : 0f);
        }

        /// <summary>Endapan lumpur di bak Sedimentasi. amount 0..1 (0 = berhenti).</summary>
        public void SetSettling(float amount)
        {
            if (sludge == null)
            {
                sludge = CreateInUnit(UnitType.Sedimentasi, "Fx_Sludge", SludgeColor, 0.55f, 0.1f);
                ParticleSystem.MainModule main = sludge.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2f);   // jatuh sampai permukaan air lalu hilang
                SetFall(sludge, -0.35f, -0.2f);   // turun pelan
            }
            SetRate(sludge, Mathf.Clamp01(amount) * 30f);
        }

        /// <summary>Kuman di bak Desinfeksi. amount 0..1 (0 = semua kuman mati).</summary>
        public void SetGerms(float amount)
        {
            if (germs == null)
            {
                germs = CreateInUnit(UnitType.Desinfeksi, "Fx_Germs", GermColor, 0.12f, 0.15f);
                ParticleSystem.MainModule main = germs.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
                main.startLifetime = 1.5f;
                // Noise = gerakan acak, kesannya kuman "berenang".
                ParticleSystem.NoiseModule noise = germs.noise;
                noise.enabled = true;
                noise.strength = 0.15f;   // kecil supaya kuman tidak keluar dari bak
                noise.frequency = 1.5f;
            }
            SetRate(germs, Mathf.Clamp01(amount) * 30f);
        }

        /// <summary>Hujan abu karhutla di atas seluruh WTP.</summary>
        public void SetAsh(bool on)
        {
            if (ash == null) ash = CreateAsh();
            SetRate(ash, on ? 60f : 0f);
        }

        /// <summary>Semburan kilau dari satu unit (unit baru diperbaiki / air jernih).</summary>
        public void Celebrate(UnitType unit)
        {
            int i = (int)unit;
            if (sparkles[i] == null) sparkles[i] = CreateSparkle(unit);
            sparkles[i].Emit(30);
        }

        /// <summary>Kilau di semua unit (WTP selesai dirakit / kasus terpecahkan / shift selesai).</summary>
        public void CelebrateAll()
        {
            for (int i = 0; i < sparkles.Length; i++) Celebrate((UnitType)i);
        }

        /// <summary>Matikan semua efek yang terus-menerus (dipanggil saat keluar dari mini-game/bagian).</summary>
        public void StopAll()
        {
            if (floc != null) SetRate(floc, 0f);
            if (sludge != null) SetRate(sludge, 0f);
            if (germs != null) SetRate(germs, 0f);
            if (ash != null) SetRate(ash, 0f);
        }

        // ------------------------------------------------------------ pembuatan partikel

        static void SetRate(ParticleSystem ps, float rate)
        {
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            if (rate > 0f && !ps.isPlaying) ps.Play();
        }

        /// <summary>
        /// Kecepatan vertikal acak antara min..max. Sumbu X & Z juga diisi (0..0) karena Unity
        /// mewajibkan ketiga sumbu memakai mode kurva yang sama.
        /// </summary>
        static void SetFall(ParticleSystem ps, float min, float max)
        {
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(min, max);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        /// <summary>Ukuran air (objek "Water") di dalam unit, dalam koordinat lokal unit.</summary>
        static Bounds WaterBounds(Transform unit)
        {
            Renderer target = null;
            foreach (Renderer r in unit.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Water") { target = r; break; }
                if (target == null) target = r;   // cadangan: bagian model apa saja
            }
            if (target == null) return new Bounds(Vector3.up, Vector3.one);

            Bounds world = target.bounds;
            float scale = unit.lossyScale.x;   // skala seragam
            return new Bounds(unit.InverseTransformPoint(world.center), world.size / scale);
        }

        /// <summary>
        /// Partikel di atas permukaan air satu unit. Air di model hanya bidang tipis yang keruh,
        /// jadi partikel di bawahnya tidak kelihatan; karena itu partikel dibuat di lapisan
        /// setinggi 'thickness' meter, 'heightAbove' meter di atas permukaan air.
        /// </summary>
        ParticleSystem CreateInUnit(UnitType type, string name, Color color, float heightAbove, float thickness)
        {
            Transform unit = plant.GetUnit(type).transform;
            Bounds b = WaterBounds(unit);
            // 75% luas permukaan supaya partikel tidak menempel di dinding bak.
            Vector3 size = new Vector3(b.size.x * 0.75f, thickness, b.size.z * 0.75f);
            Vector3 center = new Vector3(b.center.x, b.max.y + heightAbove, b.center.z);

            ParticleSystem ps = NewSystem(name, unit, center, color);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            return ps;
        }

        ParticleSystem CreateAsh()
        {
            // Kotak setinggi 3 m di atas seluruh model.
            Transform content = plant.GetUnit(UnitType.Intake).transform.parent.parent; // Units -> Content
            Bounds world = new Bounds(content.position, Vector3.zero);
            foreach (Renderer r in content.GetComponentsInChildren<Renderer>()) world.Encapsulate(r.bounds);
            float scale = content.lossyScale.x;
            Vector3 center = content.InverseTransformPoint(world.center) + Vector3.up * (world.size.y / scale * 0.5f + 1.5f);

            ParticleSystem ps = NewSystem("Fx_Ash", content, center, AshColor);
            ParticleSystem.MainModule main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startLifetime = 3.5f;
            main.maxParticles = 300;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(world.size.x / scale, 0.2f, world.size.z / scale);
            SetFall(ps, -1.6f, -1f);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.6f;
            return ps;
        }

        ParticleSystem CreateSparkle(UnitType type)
        {
            Transform unit = plant.GetUnit(type).transform;
            Bounds b = WaterBounds(unit);
            ParticleSystem ps = NewSystem("Fx_Sparkle", unit, b.center + Vector3.up * b.extents.y, SparkleColor);
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.32f);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(b.size.x * 0.8f, 0.05f, b.size.z * 0.8f);
            SetFall(ps, 1.5f, 3f);   // nilai positif = menyembur ke atas
            // Rate 0 = tidak memancar sendiri, hanya lewat Emit(). Tetap harus Play()
            // supaya partikel hasil Emit() disimulasikan (bergerak, membesar, lalu hilang).
            ps.Play();
            return ps;
        }

        /// <summary>Partikel dasar: ruang lokal, ikut skala hierarki, mengecil di akhir umur.</summary>
        ParticleSystem NewSystem(string name, Transform parent, Vector3 localPos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // komponen baru langsung jalan, hentikan dulu

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            main.startColor = color;
            main.maxParticles = 200;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = vel.y = vel.z = 0f;
            vel.orbitalX = vel.orbitalY = vel.orbitalZ = 0f;

            // Muncul & hilang halus: ukuran 0 -> 1 -> 0 sepanjang umur partikel.
            ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = -10f;   // digambar setelah air transparan supaya kelihatan
            return ps;
        }
    }
}
