using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WTP.AR;
using WTP.Plant;

namespace WTP.EditorTools
{
    /// <summary>
    /// Alat Editor (tidak ikut ke APK) untuk menyusun prefab WTP_Plant secara otomatis.
    /// Menu: WTP > Build Plant Prefab. Jalankan ulang setiap kali model FBX diubah.
    ///
    /// Tata letak (dilihat dari atas, satuan meter model):
    ///
    ///   z+  ~~~~~~~~~~~~~~~~~ SUNGAI ~~~~~~~~~~~~~~~~~
    ///        [4 Sedimentasi][3 Flokulasi][2 Koagulasi][1 Intake]   <- air mengalir ke -X
    ///        [5 Filtrasi   ][6 Desinfeksi][7 Reservoir]  [KOTA]    <- air mengalir ke +X
    ///   z-                                 (sisi ini menghadap pemain)
    /// </summary>
    public static class WTPPlantBuilder
    {
        const string ModelFolder = "Assets/WTP/Models";
        const string MaterialFolder = "Assets/WTP/Materials";
        const string PrefabPath = "Assets/WTP/Prefabs/WTP_Plant.prefab";

        // Skala seluruh WTP di dunia AR: 1 m model = 5 cm di meja (total ±80 x 50 cm).
        const float PlantScale = 0.05f;
        const float UnitSpacing = 3.4f;   // jarak antar pusat unit (lebar unit 2.6 + celah 0.8)
        const float RowBackZ = 2f;
        const float RowFrontZ = -2f;
        const float PipeDiameter = 0.196f; // sama dengan pipa bawaan model unit

        static readonly string[] UnitNames =
        {
            "WTP_01_Intake", "WTP_02_Koagulasi", "WTP_03_Flokulasi", "WTP_04_Sedimentasi",
            "WTP_05_Filtrasi", "WTP_06_Desinfeksi", "WTP_07_Reservoir",
        };

        // ---------------------------------------------------------------- Materials

        [MenuItem("WTP/Setup Materials")]
        public static void SetupMaterials()
        {
            EnsureFolder(MaterialFolder);

            // 1. Salin material bawaan FBX menjadi file .mat sendiri (sekali saja per nama),
            //    lalu arahkan (remap) semua FBX ke file .mat tersebut. Hasilnya: satu material
            //    "Water" dipakai bersama oleh semua unit dan bisa diedit di Inspector.
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                bool changed = false;

                foreach (Material embedded in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    string matPath = $"{MaterialFolder}/{embedded.name}.mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null)
                    {
                        mat = new Material(embedded);
                        AssetDatabase.CreateAsset(mat, matPath);
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), mat);
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }

            // 2. Air pakai shader buatan sendiri WTP/Water (keruh <-> jernih), kaca dibuat tembus pandang.
            UseWaterShader(LoadMat("Water"), clarity: 1f);
            MakeTransparent(LoadMat("Glass"), smoothness: 0.95f);

            // 3. Material tambahan yang tidak ada di FBX.
            GetOrCreateMat("Ground", new Color(0.47f, 0.62f, 0.38f));
            UseWaterShader(GetOrCreateMat("RiverWater", Color.white), clarity: 0.05f); // sungai tercemar abu karhutla
            var window = GetOrCreateMat("Window", new Color(0.25f, 0.27f, 0.32f));
            window.SetColor("_EmissionColor", Color.black); // jendela gelap (belum ada air bersih)
            // Jendela menyala (dipakai CityLights): kuning hangat + emission supaya terlihat "bercahaya".
            var lit = GetOrCreateMat("WindowLit", new Color(1f, 0.85f, 0.45f));
            lit.EnableKeyword("_EMISSION");
            lit.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.3f) * 1.5f);
            lit.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(lit);

            AssetDatabase.SaveAssets();
            Debug.Log("[WTP] Material siap di " + MaterialFolder);
        }

        static Material LoadMat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");

        static Material GetOrCreateMat(string name, Color color)
        {
            var mat = LoadMat(name);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(mat, $"{MaterialFolder}/{name}.mat");
            return mat;
        }

        static void UseWaterShader(Material mat, float clarity)
        {
            if (mat == null) return;
            mat.shader = Shader.Find("WTP/Water");
            mat.SetColor("_DirtyColor", new Color(0.45f, 0.33f, 0.18f, 0.95f));
            mat.SetColor("_CleanColor", new Color(0.1f, 0.5f, 0.95f, 0.8f));
            mat.SetFloat("_Clarity", clarity);
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
        }

        // Ubah material URP Lit ke mode Transparent (sama seperti mengganti "Surface Type" di Inspector).
        static void MakeTransparent(Material mat, float smoothness)
        {
            if (mat == null) return;
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
        }

        // ---------------------------------------------------------------- Prefab

        [MenuItem("WTP/Build Plant Prefab")]
        public static void BuildPlantPrefab()
        {
            if (LoadMat("Water") == null) SetupMaterials();
            EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));

            var root = new GameObject("WTP_Plant");
            root.AddComponent<PlantGestures>();

            // "Content" berisi semua model dalam skala meter asli, lalu diperkecil.
            // Digeser supaya titik tengah alas WTP tepat di titik tap pemain.
            var content = new GameObject("Content").transform;
            content.SetParent(root.transform, false);
            content.localScale = Vector3.one * PlantScale;
            content.localPosition = -new Vector3(0.6f, 0f, 0.9f) * PlantScale;

            Transform units = Child(content, "Units");
            Transform pipes = Child(content, "Pipes");

            // --- 7 unit WTP
            var placed = new List<Transform>();
            for (int i = 0; i < UnitNames.Length; i++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/Units/{UnitNames[i]}.fbx");
                var unit = (GameObject)PrefabUtility.InstantiatePrefab(model, units);

                bool backRow = i < 4;
                float x = backRow ? (1.5f - i) * UnitSpacing          // 5.1, 1.7, -1.7, -5.1
                                  : (i - 5.5f) * UnitSpacing;         // -5.1, -1.7, 1.7
                unit.transform.localPosition = new Vector3(x, 0f, backRow ? RowBackZ : RowFrontZ);
                unit.transform.localRotation = Quaternion.Euler(0f, backRow ? 0f : 180f, 0f);
                placed.Add(unit.transform);

                var wtpUnit = unit.AddComponent<WTPUnit>();
                var so = new SerializedObject(wtpUnit);
                so.FindProperty("unitType").enumValueIndex = i;
                so.FindProperty("displayName").stringValue = ((UnitType)i).ToString();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // PlantController di root memegang ke-7 unit sesuai urutan aliran.
            var controller = root.AddComponent<PlantController>();
            var cso = new SerializedObject(controller);
            var unitsProp = cso.FindProperty("units");
            unitsProp.arraySize = placed.Count;
            for (int i = 0; i < placed.Count; i++)
                unitsProp.GetArrayElementAtIndex(i).objectReferenceValue = placed[i].GetComponent<WTPUnit>();
            cso.ApplyModifiedPropertiesWithoutUndo();

            // --- pipa antar unit: Outlet unit i -> Inlet unit i+1
            for (int i = 0; i < placed.Count - 1; i++)
                BuildPipeRoute(pipes, $"Pipe_{i + 1}_{i + 2}", placed[i].Find("Outlet"), placed[i + 1].Find("Inlet"));

            // --- sungai di belakang Intake + pipa pengambilan air
            const float riverZ = 4.8f;
            Box(content, "River", new Vector3(0.6f, 0.01f, riverZ), new Vector3(16.2f, 0.04f, 1.6f), LoadMat("RiverWater"));
            Transform intakeIn = placed[0].Find("Inlet");
            Vector3 a = content.InverseTransformPoint(intakeIn.position);
            BuildPipe(pipes, "Pipe_Sungai_Intake", new List<Vector3>
            {
                a, a + Vector3.right * 0.4f, new Vector3(a.x + 0.4f, a.y, riverZ), new Vector3(a.x + 0.4f, 0.05f, riverZ),
            });

            // --- kota placeholder + pipa distribusi dari Reservoir
            Transform city = BuildCity(content);
            var lights = city.gameObject.AddComponent<CityLights>();
            var lso = new SerializedObject(lights);
            lso.FindProperty("darkWindow").objectReferenceValue = LoadMat("Window");
            lso.FindProperty("litWindow").objectReferenceValue = LoadMat("WindowLit");
            lso.ApplyModifiedPropertiesWithoutUndo();
            cso.FindProperty("cityLights").objectReferenceValue = lights;
            cso.ApplyModifiedPropertiesWithoutUndo();
            Vector3 r = content.InverseTransformPoint(placed[6].Find("Outlet").position);
            BuildPipe(pipes, "Pipe_Reservoir_Kota", new List<Vector3> { r, new Vector3(4.2f, r.y, r.z) });

            // --- alas tanah
            Box(content, "Ground", new Vector3(0.6f, -0.075f, 0.9f), new Vector3(16.4f, 0.15f, 9.8f), LoadMat("Ground"));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[WTP] Prefab tersimpan: " + PrefabPath);
        }

        // Jalur pipa siku (gaya "Manhattan") dari outlet ke inlet:
        // keluar lurus dari outlet -> belok ke Z inlet -> naik/turun ke tinggi inlet -> masuk ke inlet.
        static void BuildPipeRoute(Transform parent, string name, Transform outlet, Transform inlet)
        {
            Transform space = parent.parent; // "Content"
            Vector3 a = space.InverseTransformPoint(outlet.position);
            Vector3 b = space.InverseTransformPoint(inlet.position);
            Vector3 outDir = space.InverseTransformDirection(outlet.parent.TransformDirection(Vector3.left));
            Vector3 inDir = space.InverseTransformDirection(inlet.parent.TransformDirection(Vector3.right));

            float stub = (UnitSpacing - 2.6f) / 2f; // separuh celah antar unit
            Vector3 a1 = a + outDir * stub;
            Vector3 b1 = b + inDir * stub;

            BuildPipe(parent, name, new List<Vector3>
            {
                a, a1,
                new Vector3(a1.x, a1.y, b1.z),
                new Vector3(a1.x, b1.y, b1.z),
                b1, b,
            });
        }

        // Pipa dari daftar titik: tiap ruas = mesh pipa yang direntangkan, tiap belokan = bola sambungan.
        static void BuildPipe(Transform parent, string name, List<Vector3> points)
        {
            var pipeMesh = AssetDatabase.LoadAllAssetsAtPath($"{ModelFolder}/Pipes/Pipe_Straight.fbx")
                .OfType<Mesh>().First(m => m.name == "Pipe");
            Material pipeMat = LoadMat("Pipe");

            // Buang titik yang sama berurutan (ruas panjang 0).
            var pts = new List<Vector3>();
            foreach (var p in points)
                if (pts.Count == 0 || (pts[^1] - p).sqrMagnitude > 0.0001f) pts.Add(p);

            Transform group = Child(parent, name);
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 dir = pts[i + 1] - pts[i];
                var seg = new GameObject($"Segment_{i}", typeof(MeshFilter), typeof(MeshRenderer));
                seg.transform.SetParent(group, false);
                seg.transform.localPosition = (pts[i] + pts[i + 1]) / 2f;
                // Mesh "Pipe" (tanpa transform node FBX) memanjang di sumbu Y dengan panjang 1.02.
                seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                seg.transform.localScale = new Vector3(1f, dir.magnitude / 1.02f, 1f);
                seg.GetComponent<MeshFilter>().sharedMesh = pipeMesh;
                seg.GetComponent<MeshRenderer>().sharedMaterial = pipeMat;

                if (i > 0) // sambungan di setiap belokan
                {
                    var joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.DestroyImmediate(joint.GetComponent<Collider>());
                    joint.name = $"Joint_{i}";
                    joint.transform.SetParent(group, false);
                    joint.transform.localPosition = pts[i];
                    joint.transform.localScale = Vector3.one * PipeDiameter;
                    joint.GetComponent<MeshRenderer>().sharedMaterial = pipeMat;
                }
            }
        }

        // Kota Tirtapura (placeholder): rumah kotak dengan jendela yang nanti menyala per level selesai.
        static Transform BuildCity(Transform content)
        {
            Transform city = Child(content, "City");
            Material wall = LoadMat("BuildingWall"), roof = LoadMat("Roof"), window = LoadMat("Window");
            float[] heights = { 1.0f, 1.8f, 1.3f, 2.2f, 0.9f, 1.5f };
            int n = 0;
            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++, n++)
            {
                float h = heights[n];
                var house = Child(city, $"House_{n + 1}");
                house.localPosition = new Vector3(4.9f + col * 1.3f, 0f, -1.2f - row * 1.6f);
                Box(house, "Body", new Vector3(0f, h / 2f, 0f), new Vector3(1f, h, 1f), wall);
                Box(house, "Roof", new Vector3(0f, h + 0.05f, 0f), new Vector3(1.1f, 0.1f, 1.1f), roof);
                // Jendela di sisi depan (-Z) yang menghadap pemain.
                Box(house, "Window", new Vector3(0f, h * 0.6f, -0.51f), new Vector3(0.6f, h * 0.3f, 0.02f), window);
            }
            return city;
        }

        // ---------------------------------------------------------------- Helpers

        static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>()); // tidak perlu fisika
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
