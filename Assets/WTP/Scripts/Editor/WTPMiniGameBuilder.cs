using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WTP.Gameplay.Belajar;

namespace WTP.EditorTools
{
    /// <summary>
    /// Menu WTP > Build Mini-Game UI: membangun ulang panel mini-game di HUD scene ARPlay
    /// lalu menyambungkannya ke LevelController. Aman dijalankan berkali-kali
    /// (panel lama "MiniGames" dihapus dulu).
    /// </summary>
    public static class WTPMiniGameBuilder
    {
        const string ScenePath = "Assets/WTP/Scenes/ARPlay.unity";
        const string MaterialDir = "Assets/WTP/Materials";

        static readonly Color CardColor = new Color(0.04f, 0.13f, 0.22f, 0.94f);
        static readonly Color TitleColor = new Color(1f, 0.84f, 0.29f);
        static readonly Color InfoColor = new Color(0.7f, 0.9f, 1f);
        static readonly Color ButtonBlue = new Color(0.18f, 0.5f, 0.76f);
        static readonly Color ButtonYellow = new Color(0.98f, 0.79f, 0.25f);
        static readonly Color DarkText = new Color(0.08f, 0.14f, 0.2f);

        static Sprite uiSprite;

        [MenuItem("WTP/Build Mini-Game UI")]
        public static void Build()
        {
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Transform hud = null;
            LevelController controller = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "HUD") hud = root.transform;
                if (controller == null) controller = root.GetComponentInChildren<LevelController>(true);
            }
            if (hud == null || controller == null)
            {
                Debug.LogError("[WTP] HUD atau LevelController tidak ditemukan di ARPlay.");
                return;
            }

            Transform old = hud.Find("MiniGames");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Wadah semua panel, diletakkan tepat setelah QuizPanel supaya tombol Menu/Taruh Ulang tetap di atas.
            var container = NewRect("MiniGames", hud);
            Stretch(container);
            Transform quiz = hud.Find("QuizPanel");
            if (quiz != null) container.SetSiblingIndex(quiz.GetSiblingIndex() + 1);

            var intake = BuildIntake(container);
            var coagulation = BuildCoagulation(container);
            var filtration = BuildFiltration(container);
            var disinfection = BuildDisinfection(container);

            // Sambungkan ke LevelController.miniGames (indeks = UnitType).
            var so = new SerializedObject(controller);
            SerializedProperty games = so.FindProperty("miniGames");
            games.arraySize = 7;
            games.GetArrayElementAtIndex(0).objectReferenceValue = intake;
            games.GetArrayElementAtIndex(1).objectReferenceValue = coagulation;
            games.GetArrayElementAtIndex(4).objectReferenceValue = filtration;
            games.GetArrayElementAtIndex(5).objectReferenceValue = disinfection;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[WTP] Panel mini-game dibangun ulang di ARPlay.");
        }

        // ---------------- Intake ----------------

        static IntakeGame BuildIntake(RectTransform parent)
        {
            // Kartu kecil di atas, supaya sungai di AR tetap terlihat & bisa di-tap.
            RectTransform card = Card("IntakeGame", parent, top: true, height: 260);
            Text(card, "Title", "Bersihkan Sampah Sungai", -20, 70, 44, TitleColor);
            TMP_Text status = Text(card, "Status", "", -95, 70, 34, Color.white);
            TMP_Text counter = Text(card, "Counter", "", -170, 60, 34, InfoColor);

            var game = card.gameObject.AddComponent<IntakeGame>();
            var so = new SerializedObject(game);
            so.FindProperty("counterText").objectReferenceValue = counter;
            so.FindProperty("statusText").objectReferenceValue = status;
            SerializedProperty mats = so.FindProperty("trashMaterials");
            Color[] colors =
            {
                new Color(0.35f, 0.7f, 0.85f),   // botol plastik
                new Color(0.92f, 0.92f, 0.9f),   // kantong plastik
                new Color(0.62f, 0.45f, 0.28f),  // kardus
                new Color(0.35f, 0.25f, 0.15f),  // ranting
            };
            string[] names = { "Trash_Bottle", "Trash_Bag", "Trash_Box", "Trash_Twig" };
            mats.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                mats.GetArrayElementAtIndex(i).objectReferenceValue = TrashMaterial(names[i], colors[i]);
            so.ApplyModifiedPropertiesWithoutUndo();

            card.gameObject.SetActive(false);
            return game;
        }

        static Material TrashMaterial(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.4f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------- Koagulasi ----------------

        static CoagulationGame BuildCoagulation(RectTransform parent)
        {
            RectTransform card = Card("CoagulationGame", parent, top: false, height: 840);
            Text(card, "Title", "Jar Test: Dosis Tawas", -25, 70, 46, TitleColor);
            TMP_Text raw = Text(card, "RawWater", "", -100, 80, 34, InfoColor);

            // Gelas jar test: bingkai kaca + air di dalamnya.
            RectTransform glass = NewRect("Jar", card);
            SetRect(glass, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(200f, 220f));
            AddImage(glass, new Color(0.85f, 0.95f, 1f, 0.35f));
            RectTransform water = NewRect("JarWater", glass);
            water.anchorMin = Vector2.zero;
            water.anchorMax = Vector2.one;
            water.offsetMin = new Vector2(12f, 12f);
            water.offsetMax = new Vector2(-12f, -45f);
            Image jarWater = AddImage(water, new Color(0.55f, 0.42f, 0.25f));

            TMP_Text result = Text(card, "Result", "", -425, 120, 34, Color.white);
            TMP_Text dose = Text(card, "Dose", "", -550, 60, 40, TitleColor);
            Slider slider = MakeSlider(card, -640, 0f, 12f, true);
            TMP_Text attempts = Text(card, "Attempts", "", -705, 50, 30, InfoColor);
            Button pour = MakeButton(card, "PourButton", "Tuang & Aduk", new Vector2(0f, 30f), new Vector2(460f, 110f),
                                     ButtonYellow, DarkText);

            var game = card.gameObject.AddComponent<CoagulationGame>();
            var so = new SerializedObject(game);
            so.FindProperty("rawWaterText").objectReferenceValue = raw;
            so.FindProperty("doseSlider").objectReferenceValue = slider;
            so.FindProperty("doseText").objectReferenceValue = dose;
            so.FindProperty("pourButton").objectReferenceValue = pour;
            so.FindProperty("jarWater").objectReferenceValue = jarWater;
            so.FindProperty("resultText").objectReferenceValue = result;
            so.FindProperty("attemptsText").objectReferenceValue = attempts;
            so.ApplyModifiedPropertiesWithoutUndo();

            card.gameObject.SetActive(false);
            return game;
        }

        // ---------------- Filtrasi ----------------

        static FiltrationGame BuildFiltration(RectTransform parent)
        {
            RectTransform card = Card("FiltrationGame", parent, top: false, height: 760);
            Text(card, "Title", "Susun Lapisan Saringan", -25, 70, 46, TitleColor);
            TMP_Text feedback = Text(card, "Feedback", "", -100, 140, 34, Color.white);

            // Kiri: gambaran tumpukan. Kanan: 4 kartu lapisan.
            TMP_Text stack = Text(card, "Stack", "", -260, 440, 34, InfoColor);
            var stackRect = stack.rectTransform;
            stackRect.anchorMin = new Vector2(0f, 1f);
            stackRect.anchorMax = new Vector2(0.5f, 1f);
            stackRect.sizeDelta = new Vector2(-40f, 440f);
            stackRect.anchoredPosition = new Vector2(10f, -260f);
            stack.alignment = TextAlignmentOptions.Top;
            stack.lineSpacing = 20f;

            var cards = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                cards[i] = MakeButton(card, $"Card{i + 1}", "", Vector2.zero, Vector2.zero, ButtonBlue, Color.white);
                var r = (RectTransform)cards[i].transform;
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.sizeDelta = new Vector2(-50f, 95f);
                r.anchoredPosition = new Vector2(-10f, -265f - i * 110f);
            }

            var game = card.gameObject.AddComponent<FiltrationGame>();
            var so = new SerializedObject(game);
            SerializedProperty cardsProp = so.FindProperty("cards");
            cardsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            so.FindProperty("stackText").objectReferenceValue = stack;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;
            so.ApplyModifiedPropertiesWithoutUndo();

            card.gameObject.SetActive(false);
            return game;
        }

        // ---------------- Desinfeksi ----------------

        static DisinfectionGame BuildDisinfection(RectTransform parent)
        {
            RectTransform card = Card("DisinfectionGame", parent, top: false, height: 760);
            Text(card, "Title", "Jaga Dosis Klorin", -25, 70, 46, TitleColor);
            TMP_Text timer = Text(card, "Timer", "", -25, 70, 34, InfoColor);
            timer.alignment = TextAlignmentOptions.Right;
            TMP_Text status = Text(card, "Status", "", -105, 100, 36, Color.white);
            TMP_Text residual = Text(card, "Residual", "", -215, 60, 40, TitleColor);

            // Pengukur sisa klorin 0..1 mg/L dengan pita zona aman 0.2-0.5.
            RectTransform gauge = NewRect("Gauge", card);
            SetRect(gauge, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -320f), new Vector2(-120f, 70f));
            AddImage(gauge, new Color(0.02f, 0.07f, 0.12f, 1f));
            RectTransform fill = NewRect("Fill", gauge);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = AddImage(fill, Color.red);

            RectTransform zone = NewRect("SafeZone", card);
            SetRect(zone, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -365f), new Vector2(-120f, 16f));
            RectTransform band = NewRect("Band", zone);
            band.anchorMin = new Vector2(0.2f, 0f);
            band.anchorMax = new Vector2(0.5f, 1f);
            band.offsetMin = band.offsetMax = Vector2.zero;
            AddImage(band, new Color(0.25f, 0.75f, 0.35f));
            Text(card, "Scale", "0          0.2     zona aman     0.5                              1.0 mg/L",
                 -385, 45, 26, InfoColor);

            TMP_Text dose = Text(card, "Dose", "", -455, 60, 38, Color.white);
            Slider slider = MakeSlider(card, -545, 0f, 1.6f, false);
            Text(card, "Hint", "Geser slider supaya sisa klorin tetap di zona hijau sampai waktu habis.",
                 -620, 90, 30, InfoColor);

            var game = card.gameObject.AddComponent<DisinfectionGame>();
            var so = new SerializedObject(game);
            so.FindProperty("doseSlider").objectReferenceValue = slider;
            so.FindProperty("doseText").objectReferenceValue = dose;
            so.FindProperty("gaugeFill").objectReferenceValue = fill;
            so.FindProperty("gaugeFillImage").objectReferenceValue = fillImage;
            so.FindProperty("residualText").objectReferenceValue = residual;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("timerText").objectReferenceValue = timer;
            so.ApplyModifiedPropertiesWithoutUndo();

            card.gameObject.SetActive(false);
            return game;
        }

        // ---------------- Helper UI ----------------

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static void SetRect(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.pivot = new Vector2(0.5f, anchorMin.y > 0.5f ? 1f : 0f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        static Image AddImage(RectTransform r, Color color, bool sliced = false)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            if (sliced)
            {
                img.sprite = uiSprite;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        /// <summary>Kartu panel lebar penuh (margin 30), menempel di atas (di bawah tombol Menu) atau di bawah layar.</summary>
        static RectTransform Card(string name, Transform parent, bool top, float height)
        {
            RectTransform card = NewRect(name, parent);
            if (top) SetRect(card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -230f), new Vector2(-60f, height));
            else SetRect(card, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 40f), new Vector2(-60f, height));
            AddImage(card, CardColor, sliced: true);
            return card;
        }

        /// <summary>Teks selebar kartu, posisi y diukur dari atas kartu.</summary>
        static TMP_Text Text(RectTransform parent, string name, string text, float y, float height, float size, Color color)
        {
            RectTransform r = NewRect(name, parent);
            SetRect(r, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-80f, height));
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = TMP_Settings.defaultFontAsset;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(RectTransform parent, string name, string label, Vector2 pos, Vector2 size,
                                 Color color, Color textColor)
        {
            RectTransform r = NewRect(name, parent);
            SetRect(r, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);
            Image img = AddImage(r, color, sliced: true);
            var button = r.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            RectTransform tr = NewRect("Text (TMP)", r);
            Stretch(tr);
            tr.offsetMin = new Vector2(24f, 8f);
            tr.offsetMax = new Vector2(-24f, -8f);
            var t = tr.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = TMP_Settings.defaultFontAsset;
            t.text = label;
            t.fontSize = 40;
            t.color = textColor;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return button;
        }

        /// <summary>Slider besar (mudah digeser jari) memakai template bawaan Unity lalu diperbesar.</summary>
        static Slider MakeSlider(RectTransform parent, float y, float min, float max, bool wholeNumbers)
        {
            var resources = new DefaultControls.Resources
            {
                standard = uiSprite,
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };
            GameObject go = DefaultControls.CreateSlider(resources);
            go.name = "Slider";
            go.transform.SetParent(parent, false);
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("UI");

            var r = (RectTransform)go.transform;
            SetRect(r, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-140f, 50f));
            var handle = (RectTransform)go.transform.Find("Handle Slide Area/Handle");
            handle.sizeDelta = new Vector2(70f, 40f);   // kenop lebih besar untuk jari

            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = min;
            slider.fillRect.GetComponent<Image>().color = ButtonYellow;
            return slider;
        }
    }
}
