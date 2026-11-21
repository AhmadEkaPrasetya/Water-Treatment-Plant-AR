using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WTP.Story;
using static WTP.EditorTools.WTPMiniGameBuilder;

namespace WTP.EditorTools
{
    /// <summary>
    /// Menu WTP > Setup Character Portraits:
    ///   1. Atur PNG di Assets/WTP/UI/Portraits jadi Sprite (hasil render Tools/Blender/build_wtp_units.py)
    ///   2. Pasang potret ke aset tokoh (Tirta, Pak Wacana, Pak Bakar Lahan) di Assets/WTP/Data/Characters
    ///   3. Tambah gambar "Portrait" di kanan atas kotak dialog scene ARPlay, sambungkan ke DialogueManager
    /// Aman dijalankan berkali-kali.
    /// </summary>
    public static class WTPPortraitBuilder
    {
        const string PortraitDir = "Assets/WTP/UI/Portraits";
        const string CharacterDir = "Assets/WTP/Data/Characters";
        const string ScenePath = "Assets/WTP/Scenes/ARPlay.unity";
        static readonly string[] Names = { "Tirta", "PakWacana", "PakBakarLahan" };

        [MenuItem("WTP/Setup Character Portraits")]
        public static void Build()
        {
            foreach (string name in Names)
            {
                Sprite sprite = ImportAsSprite($"{PortraitDir}/{name}.png");
                var character = AssetDatabase.LoadAssetAtPath<Character>($"{CharacterDir}/{name}.asset");
                if (sprite == null || character == null)
                {
                    Debug.LogWarning($"[WTP] Potret atau tokoh {name} tidak ditemukan, dilewati.");
                    continue;
                }
                character.portrait = sprite;
                EditorUtility.SetDirty(character);
            }
            AssetDatabase.SaveAssets();
            AddPortraitToDialogue();
            Debug.Log("[WTP] Potret tokoh dipasang ke kotak dialog.");
        }

        static Sprite ImportAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            if (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != 512)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void AddPortraitToDialogue()
        {
            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Transform hud = null;
            DialogueManager dialogue = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "HUD") hud = root.transform;
                if (dialogue == null) dialogue = root.GetComponentInChildren<DialogueManager>(true);
            }
            Transform panel = hud != null ? hud.Find("DialoguePanel") : null;
            if (panel == null || dialogue == null)
            {
                Debug.LogError("[WTP] DialoguePanel atau DialogueManager tidak ditemukan di ARPlay.");
                return;
            }

            Transform old = panel.Find("Portrait");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Menonjol di atas tepi kanan kotak dialog, seperti tokoh yang "berdiri" di belakang kotak.
            RectTransform r = NewRect("Portrait", panel);
            r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(-20f, -40f);
            r.sizeDelta = new Vector2(300f, 300f);
            r.SetAsFirstSibling();   // digambar di belakang teks dialog
            Image img = AddImage(r, Color.white);
            img.preserveAspect = true;
            img.raycastTarget = false;
            r.gameObject.SetActive(false);

            var so = new SerializedObject(dialogue);
            so.FindProperty("portraitImage").objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
