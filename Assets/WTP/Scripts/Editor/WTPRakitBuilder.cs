using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WTP.Core;
using WTP.Gameplay.Belajar;
using WTP.Gameplay.Rakit;
using WTP.Story;
using WTP.UI.Panels;
using static WTP.EditorTools.WTPMiniGameBuilder;

namespace WTP.EditorTools
{
    /// <summary>
    /// Menu WTP > Build Section & Rakit UI: membangun ulang di HUD scene ARPlay
    ///   - layar Pilih Bagian (SectionSelectPanel)
    ///   - semua layar Bagian 2 Rakit WTP (pilih level, panel atas, slot + kartu)
    ///   - tombol Kembali di daftar level Belajar
    /// lalu memasang & menyambungkan SectionController dan AssemblyManager di objek "Gameplay".
    /// Aman dijalankan berkali-kali (panel lama dihapus dulu).
    /// </summary>
    public static class WTPRakitBuilder
    {
        const string ScenePath = "Assets/WTP/Scenes/ARPlay.unity";
        const string RakitDataDir = "Assets/WTP/Data/Rakit";
        const int CardCount = 12;

        [MenuItem("WTP/Build Section & Rakit UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Transform hud = null;
            LevelController levelController = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "HUD") hud = root.transform;
                if (levelController == null) levelController = root.GetComponentInChildren<LevelController>(true);
            }
            if (hud == null || levelController == null)
            {
                Debug.LogError("[WTP] HUD atau LevelController tidak ditemukan di ARPlay.");
                return;
            }
            GameObject gameplay = levelController.gameObject;

            foreach (string old in new[] { "SectionSelectPanel", "RakitPanel" })
            {
                Transform t = hud.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            // Letakkan setelah MiniGames: di atas dialog, di bawah panel hasil & tombol Menu.
            int index = hud.Find("MiniGames").GetSiblingIndex() + 1;
            SectionSelectPanel sectionPanel = BuildSectionPanel(hud, gameplay);
            sectionPanel.transform.SetSiblingIndex(index);   // komponen ada di Gameplay, panelnya di HUD
            RectTransform rakitRoot = NewRect("RakitPanel", hud);
            Stretch(rakitRoot);
            rakitRoot.SetSiblingIndex(index + 1);

            AssemblyManager assembly = GetOrAdd<AssemblyManager>(gameplay);
            var so = new SerializedObject(assembly);
            BuildRakitLevelSelect(rakitRoot, so);
            BuildTopPanel(rakitRoot, so);
            BuildBottomPanel(rakitRoot, so);

            // Konten & komponen yang sudah ada.
            var levels = AssetDatabase.FindAssets("t:AssemblyLevelData", new[] { RakitDataDir })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<AssemblyLevelData>).ToArray();
            SetArray(so, "levels", levels);
            so.FindProperty("tirta").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Character>("Assets/WTP/Data/Characters/Tirta.asset");
            so.FindProperty("dialogue").objectReferenceValue = gameplay.GetComponent<DialogueManager>();
            so.FindProperty("resultPanel").objectReferenceValue = gameplay.GetComponent<LevelResultPanel>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var section = GetOrAdd<SectionController>(gameplay);
            var sso = new SerializedObject(section);
            sso.FindProperty("prologue").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueScene>("Assets/WTP/Data/Story/Prologue.asset");
            sso.FindProperty("dialogue").objectReferenceValue = gameplay.GetComponent<DialogueManager>();
            sso.FindProperty("sectionPanel").objectReferenceValue = sectionPanel;
            sso.FindProperty("belajar").objectReferenceValue = levelController;
            sso.FindProperty("rakit").objectReferenceValue = assembly;
            sso.ApplyModifiedPropertiesWithoutUndo();

            AddLevelSelectBackButton(hud, gameplay.GetComponent<LevelSelectPanel>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"[WTP] Pilih Bagian + Rakit UI dibangun ulang ({levels.Length} level Rakit).");
        }

        // ---------------- Pilih Bagian ----------------

        static SectionSelectPanel BuildSectionPanel(Transform hud, GameObject gameplay)
        {
            RectTransform card = Card("SectionSelectPanel", hud, top: false, height: 810);
            Text(card, "Title", "Pilih Bagian", -30, 70, 48, TitleColor);
            var buttons = new Button[4];
            var labels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                buttons[i] = RowButton(card, $"Section{i + 1}", -125 - i * 155, 135);
                labels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
            }
            TMP_Text status = Text(card, "Status", "", -730, 60, 30, InfoColor);

            var panel = GetOrAdd<SectionSelectPanel>(gameplay);
            var so = new SerializedObject(panel);
            so.FindProperty("panel").objectReferenceValue = card.gameObject;
            SetArray(so, "sectionButtons", buttons);
            SetArray(so, "sectionLabels", labels);
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();

            card.gameObject.SetActive(false);
            return panel;
        }

        static void AddLevelSelectBackButton(Transform hud, LevelSelectPanel panel)
        {
            var list = (RectTransform)hud.Find("LevelSelectPanel");
            Transform old = list.Find("BackButton");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Button back = MakeButton(list, "BackButton", "Kembali", Vector2.zero, Vector2.zero, ButtonYellow, DarkText);
            var r = (RectTransform)back.transform;
            r.anchorMin = r.anchorMax = r.pivot = Vector2.one;
            r.anchoredPosition = new Vector2(-30f, -22f);
            r.sizeDelta = new Vector2(220f, 80f);
            back.GetComponentInChildren<TMP_Text>().fontSize = 32;

            var so = new SerializedObject(panel);
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- Rakit: pilih level ----------------

        static void BuildRakitLevelSelect(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("RakitLevelSelect", root, top: false, height: 780);
            Text(card, "Title", "Rakit WTP: Pilih Level", -30, 70, 48, TitleColor);
            var buttons = new Button[3];
            var labels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                buttons[i] = RowButton(card, $"Level{i + 1}", -125 - i * 165, 145);
                labels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
            }
            Button back = MakeButton(card, "BackButton", "Kembali", new Vector2(0f, 30f), new Vector2(400f, 110f),
                                     ButtonYellow, DarkText);

            so.FindProperty("levelSelectPanel").objectReferenceValue = card.gameObject;
            SetArray(so, "levelButtons", buttons);
            SetArray(so, "levelLabels", labels);
            so.FindProperty("levelBackButton").objectReferenceValue = back;
            card.gameObject.SetActive(false);
        }

        // ---------------- Rakit: panel atas (judul, anggaran, status) ----------------

        static void BuildTopPanel(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("RakitTop", root, top: true, height: 220);
            TMP_Text title = Text(card, "Title", "", -15, 55, 38, TitleColor);
            TMP_Text budget = Text(card, "Budget", "", -72, 45, 32, Color.white);
            TMP_Text status = Text(card, "Status", "", -120, 90, 27, InfoColor);

            so.FindProperty("topPanel").objectReferenceValue = card.gameObject;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("budgetText").objectReferenceValue = budget;
            so.FindProperty("statusText").objectReferenceValue = status;
            card.gameObject.SetActive(false);
        }

        // ---------------- Rakit: slot + kartu + tombol ----------------

        static void BuildBottomPanel(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("RakitBottom", root, top: false, height: 930);
            Text(card, "FlowLabel", "Sungai  >>>  slot 1 sampai 7  >>>  Kota", -15, 40, 26, InfoColor);

            // 7 slot sejajar, urutan aliran air dari kiri ke kanan. Lebar slot = 1/7 lebar baris,
            // jadi tetap muat di HP yang layarnya lebih panjang (canvas lebih sempit dari 1080).
            RectTransform row = NewRect("SlotRow", card);
            SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(-30f, 150f));
            var slots = new UnitSlot[FlowSimulation.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                Button b = MakeButton(row, $"Slot{i + 1}", "", Vector2.zero, Vector2.zero, ButtonBlue, Color.white);
                Object.DestroyImmediate(b.transform.Find("Text (TMP)").gameObject);
                var r = (RectTransform)b.transform;
                Columns(r, i, slots.Length, 4f);
                r.anchorMin = new Vector2(r.anchorMin.x, 0f);
                r.anchorMax = new Vector2(r.anchorMax.x, 1f);
                r.offsetMin = new Vector2(r.offsetMin.x, 0f);
                r.offsetMax = new Vector2(r.offsetMax.x, 0f);

                TMP_Text number = Text(r, "Number", (i + 1).ToString(), -6, 40, 28, TitleColor);
                number.rectTransform.sizeDelta = new Vector2(-10f, 40f);
                TMP_Text name = Text(r, "Name", "kosong", -46, 98, 22, Color.white);
                name.rectTransform.sizeDelta = new Vector2(-12f, 98f);
                name.enableAutoSizing = true;
                name.fontSizeMin = 14;
                name.fontSizeMax = 22;

                var slot = b.gameObject.AddComponent<UnitSlot>();
                var sso = new SerializedObject(slot);
                sso.FindProperty("button").objectReferenceValue = b;
                sso.FindProperty("background").objectReferenceValue = b.image;
                sso.FindProperty("numberText").objectReferenceValue = number;
                sso.FindProperty("nameText").objectReferenceValue = name;
                sso.ApplyModifiedPropertiesWithoutUndo();
                slots[i] = slot;
            }

            // Bagian yang disembunyikan saat simulasi.
            RectTransform controls = NewRect("Controls", card);
            Stretch(controls);
            TMP_Text info = Text(controls, "Info", "", -222, 90, 26, Color.white);

            // Kartu 3 kolom x 4 baris. Posisi tiap kartu tetap (tidak bergeser saat kartu lain dipasang).
            RectTransform hand = NewRect("Hand", controls);
            SetRect(hand, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -320f), new Vector2(-30f, 460f));

            var cards = new UnitCard[CardCount];
            for (int i = 0; i < cards.Length; i++)
            {
                Button b = MakeButton(hand, $"Card{i + 1}", "", Vector2.zero, Vector2.zero, ButtonBlue, Color.white);
                var cr = (RectTransform)b.transform;
                Columns(cr, i % 3, 3, 6f);
                cr.anchoredPosition = new Vector2(0f, -(i / 3) * 117f);
                cr.sizeDelta = new Vector2(cr.sizeDelta.x, 105f);
                TMP_Text label = b.GetComponentInChildren<TMP_Text>();
                ((RectTransform)label.transform).offsetMin = new Vector2(10f, 4f);
                ((RectTransform)label.transform).offsetMax = new Vector2(-10f, -4f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 18;
                label.fontSizeMax = 30;

                var c = b.gameObject.AddComponent<UnitCard>();
                var cso = new SerializedObject(c);
                cso.FindProperty("button").objectReferenceValue = b;
                cso.FindProperty("background").objectReferenceValue = b.image;
                cso.FindProperty("label").objectReferenceValue = label;
                cso.ApplyModifiedPropertiesWithoutUndo();
                cards[i] = c;
            }

            Button exit = SmallButton(controls, "ExitButton", "Keluar", 0f, 0.26f, ButtonBlue, Color.white);
            Button clear = SmallButton(controls, "ClearButton", "Kosongkan", 0.26f, 0.58f, ButtonBlue, Color.white);
            Button flow = SmallButton(controls, "FlowButton", "Alirkan Air", 0.58f, 1f, ButtonYellow, DarkText);

            so.FindProperty("bottomPanel").objectReferenceValue = card.gameObject;
            so.FindProperty("controls").objectReferenceValue = controls.gameObject;
            so.FindProperty("infoText").objectReferenceValue = info;
            SetArray(so, "slots", slots);
            SetArray(so, "cards", cards);
            so.FindProperty("flowButton").objectReferenceValue = flow;
            so.FindProperty("clearButton").objectReferenceValue = clear;
            so.FindProperty("exitButton").objectReferenceValue = exit;
            card.gameObject.SetActive(false);
        }

        // ---------------- Helper ----------------

        /// <summary>Tombol selebar kartu (seperti tombol level di daftar Belajar).</summary>
        internal static Button RowButton(RectTransform card, string name, float y, float height)
        {
            Button b = MakeButton(card, name, "", Vector2.zero, Vector2.zero, ButtonBlue, Color.white);
            var r = (RectTransform)b.transform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(-80f, height);
            TMP_Text t = b.GetComponentInChildren<TMP_Text>();
            t.fontSize = 38;
            t.alignment = TextAlignmentOptions.Left;
            return b;
        }

        /// <summary>Tombol bawah selebar bagian [x0..x1] dari lebar panel (0..1).</summary>
        internal static Button SmallButton(RectTransform parent, string name, string label, float x0, float x1, Color color, Color text)
        {
            Button b = MakeButton(parent, name, label, Vector2.zero, Vector2.zero, color, text);
            var r = (RectTransform)b.transform;
            r.anchorMin = new Vector2(x0, 0f);
            r.anchorMax = new Vector2(x1, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.offsetMin = new Vector2(x0 == 0f ? 25f : 8f, 25f);
            r.offsetMax = new Vector2(x1 == 1f ? -25f : -8f, 125f);
            TMP_Text t = b.GetComponentInChildren<TMP_Text>();
            t.enableAutoSizing = true;
            t.fontSizeMin = 22;
            t.fontSizeMax = 34;
            return b;
        }

        /// <summary>Kolom ke-'column' dari 'count' kolom sama lebar, menempel di atas induknya.</summary>
        internal static void Columns(RectTransform r, int column, int count, float gap)
        {
            r.anchorMin = new Vector2((float)column / count, 1f);
            r.anchorMax = new Vector2((float)(column + 1) / count, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(-gap * 2f, 0f);
        }

        internal static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T existing) ? existing : go.AddComponent<T>();

        internal static void SetArray(SerializedObject so, string field, Object[] values)
        {
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
