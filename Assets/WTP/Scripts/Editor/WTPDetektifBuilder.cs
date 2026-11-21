using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WTP.Core;
using WTP.Gameplay.Detektif;
using WTP.Plant;
using WTP.Story;
using WTP.UI.Panels;
using static WTP.EditorTools.WTPMiniGameBuilder;
using static WTP.EditorTools.WTPRakitBuilder;

namespace WTP.EditorTools
{
    /// <summary>
    /// Menu WTP > Build Detektif UI: membangun ulang semua layar Bagian 3 (pilih kasus, laporan,
    /// tombol 7 unit, data inspeksi, pilihan perbaikan) di HUD scene ARPlay, lalu memasang
    /// DetectiveManager di objek "Gameplay" dan menyambungkannya ke SectionController.
    /// Jalankan setelah WTP > Build Section & Rakit UI. Aman dijalankan berkali-kali.
    /// </summary>
    public static class WTPDetektifBuilder
    {
        const string ScenePath = "Assets/WTP/Scenes/ARPlay.unity";
        const string CaseDataDir = "Assets/WTP/Data/Detektif";

        [MenuItem("WTP/Build Detektif UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Transform hud = null;
            SectionController section = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "HUD") hud = root.transform;
                if (section == null) section = root.GetComponentInChildren<SectionController>(true);
            }
            if (hud == null || section == null || hud.Find("RakitPanel") == null)
            {
                Debug.LogError("[WTP] HUD/SectionController/RakitPanel tidak ditemukan. Jalankan WTP > Build Section & Rakit UI dulu.");
                return;
            }
            GameObject gameplay = section.gameObject;

            Transform old = hud.Find("DetektifPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root2 = NewRect("DetektifPanel", hud);
            Stretch(root2);
            root2.SetSiblingIndex(hud.Find("RakitPanel").GetSiblingIndex() + 1);

            DetectiveManager manager = GetOrAdd<DetectiveManager>(gameplay);
            var so = new SerializedObject(manager);
            BuildCaseSelect(root2, so);
            BuildTopPanel(root2, so);
            BuildBottomPanel(root2, so);

            var cases = AssetDatabase.FindAssets("t:CaseData", new[] { CaseDataDir })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<CaseData>).ToArray();
            SetArray(so, "cases", cases);
            so.FindProperty("tirta").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Character>("Assets/WTP/Data/Characters/Tirta.asset");
            so.FindProperty("dialogue").objectReferenceValue = gameplay.GetComponent<DialogueManager>();
            so.FindProperty("resultPanel").objectReferenceValue = gameplay.GetComponent<LevelResultPanel>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var sso = new SerializedObject(section);
            sso.FindProperty("detektif").objectReferenceValue = manager;
            sso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"[WTP] Detektif UI dibangun ulang ({cases.Length} kasus).");
        }

        // ---------------- Pilih kasus ----------------

        static void BuildCaseSelect(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("CaseSelect", root, top: false, height: 1130);
            Text(card, "Title", "Detektif: Pilih Kasus", -30, 70, 48, TitleColor);
            var buttons = new Button[6];
            var labels = new TMP_Text[6];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = RowButton(card, $"Case{i + 1}", -125 - i * 140, 125);
                labels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
                labels[i].enableAutoSizing = true;
                labels[i].fontSizeMin = 24;
                labels[i].fontSizeMax = 36;
            }
            Button back = MakeButton(card, "BackButton", "Kembali", new Vector2(0f, 30f), new Vector2(400f, 110f),
                                     ButtonYellow, DarkText);

            so.FindProperty("caseSelectPanel").objectReferenceValue = card.gameObject;
            SetArray(so, "caseButtons", buttons);
            SetArray(so, "caseLabels", labels);
            so.FindProperty("caseBackButton").objectReferenceValue = back;
            card.gameObject.SetActive(false);
        }

        // ---------------- Panel atas: judul, laporan warga, status ----------------

        static void BuildTopPanel(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("DetektifTop", root, top: true, height: 300);
            TMP_Text title = Text(card, "Title", "", -15, 55, 38, TitleColor);
            TMP_Text report = Text(card, "Report", "", -75, 130, 28, Color.white);
            report.enableAutoSizing = true;
            report.fontSizeMin = 20;
            report.fontSizeMax = 28;
            TMP_Text status = Text(card, "Status", "", -210, 80, 26, InfoColor);

            so.FindProperty("topPanel").objectReferenceValue = card.gameObject;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("reportText").objectReferenceValue = report;
            so.FindProperty("statusText").objectReferenceValue = status;
            card.gameObject.SetActive(false);
        }

        // ---------------- Panel bawah: 7 unit, data, tuduh / perbaikan ----------------

        static void BuildBottomPanel(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("DetektifBottom", root, top: false, height: 900);
            Text(card, "UnitLabel", "Periksa unit (urut dari sungai ke kota):", -15, 40, 26, InfoColor);

            RectTransform row = NewRect("UnitRow", card);
            SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(-30f, 110f));
            var unitButtons = new Button[7];
            for (int i = 0; i < unitButtons.Length; i++)
            {
                Button b = MakeButton(row, $"Unit{i + 1}", ((UnitType)i).ToString(), Vector2.zero, Vector2.zero,
                                      ButtonBlue, Color.white);
                var r = (RectTransform)b.transform;
                Columns(r, i, unitButtons.Length, 4f);
                r.anchorMin = new Vector2(r.anchorMin.x, 0f);
                r.anchorMax = new Vector2(r.anchorMax.x, 1f);
                r.offsetMin = new Vector2(r.offsetMin.x, 0f);
                r.offsetMax = new Vector2(r.offsetMax.x, 0f);
                TMP_Text t = b.GetComponentInChildren<TMP_Text>();
                var tr = (RectTransform)t.transform;
                tr.offsetMin = new Vector2(4f, 4f);
                tr.offsetMax = new Vector2(-4f, -4f);
                t.enableAutoSizing = true;
                t.fontSizeMin = 13;
                t.fontSizeMax = 24;
                unitButtons[i] = b;
            }

            // Mode periksa: data unit + tombol.
            RectTransform inspect = NewRect("Inspect", card);
            Stretch(inspect);
            TMP_Text dataTitle = Text(inspect, "DataTitle", "", -190, 55, 36, TitleColor);
            TMP_Text dataText = Text(inspect, "DataText", "", -250, 490, 30, Color.white);
            dataText.alignment = TextAlignmentOptions.TopLeft;
            dataText.enableAutoSizing = true;
            dataText.fontSizeMin = 20;
            dataText.fontSizeMax = 30;
            Button exit = SmallButton(inspect, "ExitButton", "Keluar", 0f, 0.32f, ButtonBlue, Color.white);
            Button accuse = SmallButton(inspect, "AccuseButton", "Tuduh unit ini", 0.32f, 1f, ButtonYellow, DarkText);

            // Mode perbaikan: pertanyaan + 3 pilihan.
            RectTransform fix = NewRect("Fix", card);
            Stretch(fix);
            TMP_Text question = Text(fix, "Question", "", -190, 100, 32, TitleColor);
            var fixButtons = new Button[3];
            var fixLabels = new TMP_Text[3];
            for (int i = 0; i < fixButtons.Length; i++)
            {
                fixButtons[i] = RowButton(fix, $"Fix{i + 1}", -310 - i * 155, 140);
                fixLabels[i] = fixButtons[i].GetComponentInChildren<TMP_Text>();
                fixLabels[i].alignment = TextAlignmentOptions.Center;
                fixLabels[i].enableAutoSizing = true;
                fixLabels[i].fontSizeMin = 22;
                fixLabels[i].fontSizeMax = 32;
            }
            fix.gameObject.SetActive(false);

            so.FindProperty("bottomPanel").objectReferenceValue = card.gameObject;
            SetArray(so, "unitButtons", unitButtons);
            so.FindProperty("inspectRoot").objectReferenceValue = inspect.gameObject;
            so.FindProperty("dataTitle").objectReferenceValue = dataTitle;
            so.FindProperty("dataText").objectReferenceValue = dataText;
            so.FindProperty("accuseButton").objectReferenceValue = accuse;
            so.FindProperty("accuseLabel").objectReferenceValue = accuse.GetComponentInChildren<TMP_Text>();
            so.FindProperty("exitButton").objectReferenceValue = exit;
            so.FindProperty("fixRoot").objectReferenceValue = fix.gameObject;
            so.FindProperty("fixQuestion").objectReferenceValue = question;
            SetArray(so, "fixButtons", fixButtons);
            SetArray(so, "fixLabels", fixLabels);
            card.gameObject.SetActive(false);
        }
    }
}
