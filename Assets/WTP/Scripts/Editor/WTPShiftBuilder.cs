using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WTP.Core;
using WTP.Gameplay.Shift;
using WTP.Story;
using WTP.UI.Panels;
using static WTP.EditorTools.WTPMiniGameBuilder;
using static WTP.EditorTools.WTPRakitBuilder;

namespace WTP.EditorTools
{
    /// <summary>
    /// Menu WTP > Build Shift UI: membangun ulang semua layar Bagian 4 Operator Shift di HUD scene ARPlay
    /// (layar mulai, panel atas timer/kepuasan/skor/event, 3 baris parameter + slider, popup inspeksi,
    /// lapisan kabut asap), memasang ShiftManager di objek "Gameplay", dan menyambungkannya ke SectionController.
    /// Jalankan setelah WTP > Build Detektif UI. Aman dijalankan berkali-kali.
    /// </summary>
    public static class WTPShiftBuilder
    {
        const string ScenePath = "Assets/WTP/Scenes/ARPlay.unity";
        const string EventDir = "Assets/WTP/Data/Shift";

        [MenuItem("WTP/Build Shift UI")]
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
            if (hud == null || section == null || hud.Find("DetektifPanel") == null)
            {
                Debug.LogError("[WTP] HUD/SectionController/DetektifPanel tidak ditemukan. Jalankan builder Rakit & Detektif dulu.");
                return;
            }
            GameObject gameplay = section.gameObject;

            Transform old = hud.Find("ShiftPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root2 = NewRect("ShiftPanel", hud);
            Stretch(root2);
            root2.SetSiblingIndex(hud.Find("DetektifPanel").GetSiblingIndex() + 1);

            ShiftManager manager = GetOrAdd<ShiftManager>(gameplay);
            var so = new SerializedObject(manager);
            BuildHaze(root2, so);
            BuildStartPanel(root2, so);
            BuildTopPanel(root2, so);
            BuildBottomPanel(root2, so);
            BuildInspection(root2, so);

            var events = AssetDatabase.FindAssets("t:ShiftEvent", new[] { EventDir })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<ShiftEvent>).ToArray();
            SetArray(so, "events", events);
            so.FindProperty("intro").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueScene>("Assets/WTP/Data/Story/ShiftIntro.asset");
            so.FindProperty("ending").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueScene>("Assets/WTP/Data/Story/Ending.asset");
            so.FindProperty("pakWacana").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Character>("Assets/WTP/Data/Characters/PakWacana.asset");
            so.FindProperty("dialogue").objectReferenceValue = gameplay.GetComponent<DialogueManager>();
            so.FindProperty("resultPanel").objectReferenceValue = gameplay.GetComponent<LevelResultPanel>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var sso = new SerializedObject(section);
            sso.FindProperty("shift").objectReferenceValue = manager;
            sso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"[WTP] Shift UI dibangun ulang ({events.Length} event).");
        }

        static void BuildHaze(RectTransform root, SerializedObject so)
        {
            RectTransform haze = NewRect("SmokeHaze", root);
            Stretch(haze);
            AddImage(haze, new Color(0.55f, 0.53f, 0.5f, 0.5f)).raycastTarget = false;   // tap tetap tembus
            so.FindProperty("hazeOverlay").objectReferenceValue = haze.gameObject;
            haze.gameObject.SetActive(false);
        }

        static void BuildStartPanel(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("ShiftStart", root, top: false, height: 760);
            Text(card, "Title", "Operator Shift: Musim Asap", -30, 70, 46, TitleColor);
            TMP_Text info = Text(card, "Info", "", -120, 430, 30, Color.white);
            info.enableAutoSizing = true;
            info.fontSizeMin = 20;
            info.fontSizeMax = 30;
            Button start = MakeButton(card, "StartButton", "Mulai Shift", new Vector2(170f, 30f), new Vector2(440f, 110f),
                                      ButtonYellow, DarkText);
            Button back = MakeButton(card, "BackButton", "Kembali", new Vector2(-250f, 30f), new Vector2(300f, 110f),
                                     ButtonBlue, Color.white);

            so.FindProperty("startPanel").objectReferenceValue = card.gameObject;
            so.FindProperty("startInfoText").objectReferenceValue = info;
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("startBackButton").objectReferenceValue = back;
            card.gameObject.SetActive(false);
        }

        static void BuildTopPanel(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("ShiftTop", root, top: true, height: 290);
            TMP_Text timer = Text(card, "Timer", "", -15, 50, 36, TitleColor);
            timer.alignment = TextAlignmentOptions.Left;

            Button quit = MakeButton(card, "QuitButton", "Keluar", Vector2.zero, Vector2.zero, ButtonBlue, Color.white);
            var qr = (RectTransform)quit.transform;
            qr.anchorMin = qr.anchorMax = qr.pivot = Vector2.one;
            qr.anchoredPosition = new Vector2(-20f, -12f);
            qr.sizeDelta = new Vector2(190f, 62f);
            quit.GetComponentInChildren<TMP_Text>().fontSize = 28;

            TMP_Text satisfaction = Text(card, "Satisfaction", "", -70, 40, 28, Color.white);
            RectTransform bar = NewRect("SatisfactionBar", card);
            SetRect(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -115f), new Vector2(-80f, 38f));
            AddImage(bar, new Color(0.02f, 0.07f, 0.12f, 1f));
            RectTransform fill = NewRect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.7f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Image fillImage = AddImage(fill, Color.green);

            TMP_Text scoreText = Text(card, "Score", "", -160, 40, 30, TitleColor);
            TMP_Text eventText = Text(card, "Event", "", -202, 80, 25, Color.white);
            eventText.enableAutoSizing = true;
            eventText.fontSizeMin = 18;
            eventText.fontSizeMax = 25;

            so.FindProperty("topPanel").objectReferenceValue = card.gameObject;
            so.FindProperty("timerText").objectReferenceValue = timer;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("satisfactionText").objectReferenceValue = satisfaction;
            so.FindProperty("satisfactionFill").objectReferenceValue = fill;
            so.FindProperty("satisfactionFillImage").objectReferenceValue = fillImage;
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("eventText").objectReferenceValue = eventText;
            card.gameObject.SetActive(false);
        }

        static void BuildBottomPanel(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("ShiftBottom", root, top: false, height: 760);
            // Tiga baris: (nilai, keterangan, label slider, slider)
            string[,] fields =
            {
                { "turbidityText", "rawText", "alumLabel", "alumSlider" },
                { "chlorineText", "chlorineInfoText", "chlorineLabel", "chlorineSlider" },
                { "reservoirText", "demandText", "pumpLabel", "pumpSlider" },
            };
            float[,] ranges = { { 0f, 130f }, { 0f, 4f }, { 0f, 100f } };
            for (int row = 0; row < 3; row++)
            {
                float y = -20f - row * 250f;
                TMP_Text value = Text(card, $"Value{row + 1}", "", y, 50, 32, Color.white);
                value.enableAutoSizing = true;
                value.fontSizeMin = 22;
                value.fontSizeMax = 32;
                TMP_Text info = Text(card, $"Info{row + 1}", "", y - 50, 40, 24, InfoColor);
                TMP_Text label = Text(card, $"Label{row + 1}", "", y - 95, 40, 28, TitleColor);
                Slider slider = MakeSlider(card, y - 150, ranges[row, 0], ranges[row, 1], row != 1);
                slider.name = $"Slider{row + 1}";

                so.FindProperty(fields[row, 0]).objectReferenceValue = value;
                so.FindProperty(fields[row, 1]).objectReferenceValue = info;
                so.FindProperty(fields[row, 2]).objectReferenceValue = label;
                so.FindProperty(fields[row, 3]).objectReferenceValue = slider;
            }
            so.FindProperty("bottomPanel").objectReferenceValue = card.gameObject;
            card.gameObject.SetActive(false);
        }

        static void BuildInspection(RectTransform root, SerializedObject so)
        {
            RectTransform card = Card("ShiftInspection", root, top: false, height: 560);
            Image cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(cardImage.color.r, cardImage.color.g, cardImage.color.b, 1f);   // tutup slider di belakangnya
            RectTransform portrait = NewRect("Portrait", card);
            portrait.anchorMin = portrait.anchorMax = portrait.pivot = new Vector2(0f, 1f);
            portrait.anchoredPosition = new Vector2(25f, -25f);
            portrait.sizeDelta = new Vector2(240f, 240f);
            Image img = AddImage(portrait, Color.white);
            img.preserveAspect = true;
            img.raycastTarget = false;

            TMP_Text text = Text(card, "Text", "", -30, 340, 30, Color.white);
            text.rectTransform.anchorMin = new Vector2(0.28f, 1f);
            text.rectTransform.sizeDelta = new Vector2(-40f, 340f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20;
            text.fontSizeMax = 30;

            Button accept = SmallButton(card, "AcceptButton", "", 0f, 0.5f, ButtonBlue, Color.white);
            Button refuse = SmallButton(card, "RefuseButton", "", 0.5f, 1f, ButtonYellow, DarkText);

            so.FindProperty("inspectionPanel").objectReferenceValue = card.gameObject;
            so.FindProperty("inspectionPortrait").objectReferenceValue = img;
            so.FindProperty("inspectionText").objectReferenceValue = text;
            so.FindProperty("acceptButton").objectReferenceValue = accept;
            so.FindProperty("acceptLabel").objectReferenceValue = accept.GetComponentInChildren<TMP_Text>();
            so.FindProperty("refuseButton").objectReferenceValue = refuse;
            so.FindProperty("refuseLabel").objectReferenceValue = refuse.GetComponentInChildren<TMP_Text>();
            card.gameObject.SetActive(false);
        }
    }
}
