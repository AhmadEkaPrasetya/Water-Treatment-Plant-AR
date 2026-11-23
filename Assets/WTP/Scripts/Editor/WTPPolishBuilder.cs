using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WTP.Audio;
using WTP.Gameplay.Belajar;
using WTP.Plant;
using WTP.UI;
using Object = UnityEngine.Object;

namespace WTP.EditorTools
{
    /// <summary>
    /// Menu WTP > Setup Audio & Effects:
    ///   1. atur import audio (SFX ringan di memori, musik di-stream) dari Assets/WTP/Audio
    ///   2. isi Assets/WTP/Resources/AudioLibrary.asset sesuai enum Sfx / Music
    ///   3. buat tekstur titik lembut + material partikel URP
    ///   4. pasang PlantEffects (+ material) di prefab WTP_Plant
    ///   5. pasang PanelTransition di semua panel UI kedua scene
    /// Aman dijalankan berulang kali.
    /// </summary>
    public static class WTPPolishBuilder
    {
        const string AudioFolder = "Assets/WTP/Audio";
        const string LibraryPath = "Assets/WTP/Resources/AudioLibrary.asset";
        const string DotTexturePath = "Assets/WTP/Materials/ParticleDot.png";
        const string ParticleMatPath = "Assets/WTP/Materials/Particle.mat";
        const string PlantPrefabPath = "Assets/WTP/Prefabs/WTP_Plant.prefab";
        static readonly string[] ScenePaths = { "Assets/WTP/Scenes/MainMenu.unity", "Assets/WTP/Scenes/ARPlay.unity" };

        [MenuItem("WTP/Setup Audio & Effects")]
        public static void SetupAll()
        {
            SetupAudio();
            SetupParticleMaterial();
            AddEffectsToPrefab();
            AddPanelTransitions();
            Debug.Log("[WTP] Audio & efek siap.");
        }

        // ------------------------------------------------------------ audio

        public static void SetupAudio()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            string[] sfxNames = Enum.GetNames(typeof(Sfx));
            library.sfx = new AudioClip[sfxNames.Length];
            for (int i = 0; i < sfxNames.Length; i++)
                library.sfx[i] = ImportClip($"{AudioFolder}/SFX/{sfxNames[i].ToLower()}.wav", music: false);

            string[] musicNames = Enum.GetNames(typeof(Music));
            library.music = new AudioClip[musicNames.Length];
            for (int i = 1; i < musicNames.Length; i++)   // 0 = None
                library.music[i] = ImportClip($"{AudioFolder}/Music/{musicNames[i].ToLower()}.wav", music: true);

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        static AudioClip ImportClip(string path, bool music)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning("[WTP] Audio tidak ditemukan: " + path + " (jalankan Tools/Audio/generate_audio.py)");
                return null;
            }
            importer.forceToMono = true;
            importer.loadInBackground = music;
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            if (music)
            {
                // Musik panjang: dikompres Vorbis dan dibaca sedikit-sedikit (hemat RAM HP).
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.5f;
            }
            else
            {
                // Efek pendek: langsung siap di memori supaya tidak telat bunyi.
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ------------------------------------------------------------ material partikel

        public static Material SetupParticleMaterial()
        {
            if (!File.Exists(DotTexturePath))
            {
                // Titik putih bulat dengan tepi memudar (warna diberikan oleh partikel).
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));   // smoothstep
                }
                File.WriteAllBytes(DotTexturePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(DotTexturePath);
                var ti = (TextureImporter)AssetImporter.GetAtPath(DotTexturePath);
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(ParticleMatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, ParticleMatPath);
            }
            // Transparan, campuran alpha biasa, tidak menulis depth.
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DotTexturePath));
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // ------------------------------------------------------------ prefab WTP

        /// <summary>Pasang PlantEffects + material di root prefab. Dipanggil juga oleh Build Plant Prefab.</summary>
        public static void AddEffects(GameObject plantRoot)
        {
            var effects = plantRoot.GetComponent<PlantEffects>();
            if (effects == null) effects = plantRoot.AddComponent<PlantEffects>();
            var so = new SerializedObject(effects);
            so.FindProperty("particleMaterial").objectReferenceValue = SetupParticleMaterial();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddEffectsToPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlantPrefabPath);
            AddEffects(root);
            PrefabUtility.SaveAsPrefabAsset(root, PlantPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ------------------------------------------------------------ animasi panel

        /// <summary>
        /// Cari semua field GameObject bernama "...panel..." di script WTP pada kedua scene
        /// (mis. LevelResultPanel.panel, AssemblyManager.topPanel), plus objek mini-game,
        /// lalu pasang PanelTransition di objek tersebut.
        /// </summary>
        static void AddPanelTransitions()
        {
            string before = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (string scenePath in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var targets = new HashSet<GameObject>();

                foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                {
                    if (mb == null || mb.GetType().Namespace == null || !mb.GetType().Namespace.StartsWith("WTP")) continue;
                    if (mb is MiniGame) targets.Add(mb.gameObject);

                    SerializedProperty p = new SerializedObject(mb).GetIterator();
                    while (p.NextVisible(true))
                    {
                        if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (p.name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        GameObject go = p.objectReferenceValue as GameObject;
                        if (go == null && p.objectReferenceValue is Component c) go = c.gameObject;
                        if (go != null && go.transform is RectTransform) targets.Add(go);
                    }
                }

                foreach (GameObject go in targets)
                {
                    // Kanvas utama diskalakan oleh CanvasScaler, jangan dianimasikan.
                    if (go.GetComponent<Canvas>() != null)
                    {
                        var wrong = go.GetComponent<PanelTransition>();
                        if (wrong != null) Object.DestroyImmediate(wrong);
                        continue;
                    }
                    if (go.GetComponent<PanelTransition>() != null) continue;
                    if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
                    go.AddComponent<PanelTransition>();
                    Debug.Log($"[WTP] PanelTransition -> {scene.name}/{go.name}");
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(before)) EditorSceneManager.OpenScene(before, OpenSceneMode.Single);
        }
    }
}
