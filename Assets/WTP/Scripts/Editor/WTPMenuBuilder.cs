using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WTP.Progress;
using WTP.UI.Panels;
using static WTP.EditorTools.WTPMiniGameBuilder;
using static WTP.EditorTools.WTPRakitBuilder;

namespace WTP.EditorTools
{
    /// <summary>
    /// Menu WTP > Build Badge & Leaderboard UI:
    ///   - scene MainMenu: layar Koleksi Badge + layar Leaderboard (overlay di tengah), disambungkan ke MainMenuPanel
    ///   - scene ARPlay: notifikasi "Badge baru!" (BadgeToast) di atas HUD
    ///   - membuat Assets/WTP/Data/Badges/BadgeCatalog.asset kalau belum ada (isi awal 9 badge)
    /// Aman dijalankan berkali-kali.
    /// </summary>
    public static class WTPMenuBuilder
    {
        const string MenuScene = "Assets/WTP/Scenes/MainMenu.unity";
        const string PlayScene = "Assets/WTP/Scenes/ARPlay.unity";
        const string CatalogPath = "Assets/WTP/Data/Badges/BadgeCatalog.asset";
        const int LeaderboardRows = 10;
        const int BadgeRows = 9;

        static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);

        [MenuItem("WTP/Build Badge & Leaderboard UI")]
        public static void Build()
        {
            BadgeCatalog catalog = GetOrCreateCatalog();
            BuildMainMenu(catalog);
            BuildToast(catalog);
            Debug.Log("[WTP] Layar Badge, Leaderboard, dan notifikasi badge dibangun ulang.");
        }

        // ---------------- Katalog badge ----------------

        static BadgeCatalog GetOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BadgeCatalog>(CatalogPath);
            if (catalog != null) return catalog;   // jangan menimpa hasil edit tim

            if (!AssetDatabase.IsValidFolder("Assets/WTP/Data/Badges"))
                AssetDatabase.CreateFolder("Assets/WTP/Data", "Badges");
            catalog = ScriptableObject.CreateInstance<BadgeCatalog>();
            catalog.badges = new[]
            {
                B(BadgeManager.PenjagaIntake, "Penjaga Intake", "Dapat 3 bintang di level Intake.", new Color(0.35f, 0.7f, 0.85f)),
                B(BadgeManager.AhliKoagulasi, "Ahli Koagulasi", "Dapat 3 bintang di level Koagulasi.", new Color(0.55f, 0.75f, 0.95f)),
                B(BadgeManager.OperatorMuda, "Operator Muda", "Selesaikan ke-7 level Belajar.", new Color(0.3f, 0.75f, 0.45f)),
                B(BadgeManager.BintangSempurna, "Bintang Tirtapura", "Kumpulkan 21 bintang di Bagian Belajar.", new Color(1f, 0.84f, 0.29f)),
                B(BadgeManager.AntiGapura, "Anti Gapura", "Selesaikan satu level Rakit tanpa memasang kartu jebakan Pak Wacana.", new Color(0.95f, 0.55f, 0.3f)),
                B(BadgeManager.ArsitekWTP, "Arsitek WTP", "Selesaikan ke-3 level Rakit WTP.", new Color(0.7f, 0.55f, 0.9f)),
                B(BadgeManager.DetektifJeli, "Detektif Jeli", "Pecahkan satu kasus Detektif dengan skor 100.", new Color(0.9f, 0.4f, 0.55f)),
                B(BadgeManager.DetektifHulu, "Detektif Hulu", "Bongkar sumber pencemaran di hulu sungai (kasus terakhir).", new Color(0.55f, 0.45f, 0.3f)),
                B(BadgeManager.PenyelamatTirtapura, "Penyelamat Tirtapura", "Selesaikan Operator Shift musim asap.", new Color(0.2f, 0.6f, 1f)),
            };
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        static BadgeInfo B(string id, string title, string description, Color color) =>
            new BadgeInfo { id = id, title = title, description = description, color = color };

        // ---------------- Menu utama ----------------

        static void BuildMainMenu(BadgeCatalog catalog)
        {
            var scene = OpenScene(MenuScene, out bool openedHere);
            MainMenuPanel menu = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (menu == null) menu = root.GetComponentInChildren<MainMenuPanel>(true);
            if (menu == null)
            {
                Debug.LogError("[WTP] MainMenuPanel tidak ditemukan di MainMenu.");
                return;
            }
            Transform canvas = menu.GetComponentInParent<Canvas>().transform;
            foreach (string old in new[] { "BadgePanel", "LeaderboardPanel" })
            {
                Transform t = canvas.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            // Komponen dipasang di Canvas (selalu aktif) supaya Awake-nya pasti jalan.
            var badges = BuildBadgePanel(canvas, catalog);
            var board = BuildLeaderboardPanel(canvas);

            // Pesan singkat (Toast) tetap paling atas.
            Transform toast = canvas.Find("Toast");
            if (toast != null) toast.SetAsLastSibling();

            var so = new SerializedObject(menu);
            so.FindProperty("badgePanel").objectReferenceValue = badges;
            so.FindProperty("leaderboardPanel").objectReferenceValue = board;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndClose(scene, openedHere);
        }

        static BadgeCollectionPanel BuildBadgePanel(Transform canvas, BadgeCatalog catalog)
        {
            RectTransform box = Overlay("BadgePanel", canvas, out GameObject overlay);
            Text(box, "Title", "Koleksi Badge", -30, 70, 50, TitleColor);
            TMP_Text count = Text(box, "Count", "", -105, 50, 30, InfoColor);

            var icons = new Image[BadgeRows];
            var letters = new TMP_Text[BadgeRows];
            var titles = new TMP_Text[BadgeRows];
            var descs = new TMP_Text[BadgeRows];
            for (int i = 0; i < BadgeRows; i++)
            {
                RectTransform row = NewRect($"Badge{i + 1}", box);
                SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -170f - i * 140f), new Vector2(-60f, 130f));

                RectTransform icon = NewRect("Icon", row);
                icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0f, 0.5f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(110f, 110f);
                icons[i] = AddImage(icon, Color.gray);
                icons[i].sprite = Knob;
                letters[i] = Text(icon, "Letter", "?", 0, 0, 52, DarkText);
                Stretch(letters[i].rectTransform);

                titles[i] = RowText(row, "Title", 0.52f, 1f, 34);
                titles[i].fontStyle = FontStyles.Bold;
                descs[i] = RowText(row, "Description", 0f, 0.52f, 25);
            }
            Button close = MakeButton(box, "CloseButton", "Tutup", new Vector2(0f, 30f), new Vector2(400f, 110f), ButtonYellow, DarkText);

            var panel = GetOrAdd<BadgeCollectionPanel>(canvas.gameObject);
            var so = new SerializedObject(panel);
            so.FindProperty("panel").objectReferenceValue = overlay;
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("countText").objectReferenceValue = count;
            SetArray(so, "icons", icons);
            SetArray(so, "iconLetters", letters);
            SetArray(so, "titles", titles);
            SetArray(so, "descriptions", descs);
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();
            overlay.SetActive(false);
            return panel;
        }

        /// <summary>Teks di kanan ikon badge, menempati pita vertikal [y0..y1] dari tinggi baris.</summary>
        static TMP_Text RowText(RectTransform row, string name, float y0, float y1, float size)
        {
            RectTransform r = NewRect(name, row);
            r.anchorMin = new Vector2(0f, y0);
            r.anchorMax = new Vector2(1f, y1);
            r.offsetMin = new Vector2(130f, 0f);
            r.offsetMax = Vector2.zero;
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = TMP_Settings.defaultFontAsset;
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMin = size * 0.7f;
            t.fontSizeMax = size;
            t.alignment = y0 > 0f ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.TopLeft;
            t.raycastTarget = false;
            return t;
        }

        static LeaderboardPanel BuildLeaderboardPanel(Transform canvas)
        {
            RectTransform box = Overlay("LeaderboardPanel", canvas, out GameObject overlay);
            Text(box, "Title", "Leaderboard", -30, 70, 50, TitleColor);

            Button detektif = SmallButton(box, "TabDetektif", "Detektif", 0f, 0.5f, ButtonBlue, Color.white);
            Button shift = SmallButton(box, "TabShift", "Operator Shift", 0.5f, 1f, ButtonBlue, Color.white);
            foreach (Button tab in new[] { detektif, shift })
            {
                var r = (RectTransform)tab.transform;
                r.anchorMin = new Vector2(r.anchorMin.x, 1f);
                r.anchorMax = new Vector2(r.anchorMax.x, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.offsetMin = new Vector2(r.offsetMin.x, -210f);
                r.offsetMax = new Vector2(r.offsetMax.x, -115f);
            }
            TMP_Text note = Text(box, "Note", "", -225, 50, 28, InfoColor);

            var names = new TMP_Text[LeaderboardRows];
            var scores = new TMP_Text[LeaderboardRows];
            for (int i = 0; i < LeaderboardRows; i++)
            {
                RectTransform row = NewRect($"Row{i + 1}", box);
                SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -295f - i * 100f), new Vector2(-80f, 90f));
                AddImage(row, new Color(1f, 1f, 1f, i % 2 == 0 ? 0.06f : 0.02f)).raycastTarget = false;
                names[i] = Text(row, "Name", "", 0, 0, 36, Color.white);
                SideAlign(names[i].rectTransform, 0f, 0.72f, 20f);
                names[i].alignment = TextAlignmentOptions.Left;
                scores[i] = Text(row, "Score", "", 0, 0, 36, Color.white);
                SideAlign(scores[i].rectTransform, 0.72f, 1f, -20f);
                scores[i].alignment = TextAlignmentOptions.Right;
            }
            Button close = MakeButton(box, "CloseButton", "Tutup", new Vector2(0f, 30f), new Vector2(400f, 110f), ButtonYellow, DarkText);

            var panel = GetOrAdd<LeaderboardPanel>(canvas.gameObject);
            var so = new SerializedObject(panel);
            so.FindProperty("panel").objectReferenceValue = overlay;
            so.FindProperty("detektifTab").objectReferenceValue = detektif;
            so.FindProperty("shiftTab").objectReferenceValue = shift;
            so.FindProperty("noteText").objectReferenceValue = note;
            SetArray(so, "nameTexts", names);
            SetArray(so, "scoreTexts", scores);
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();
            overlay.SetActive(false);
            return panel;
        }

        static void SideAlign(RectTransform r, float x0, float x1, float pad)
        {
            r.anchorMin = new Vector2(x0, 0f);
            r.anchorMax = new Vector2(x1, 1f);
            r.offsetMin = new Vector2(pad > 0 ? pad : 0f, 0f);
            r.offsetMax = new Vector2(pad < 0 ? pad : 0f, 0f);
        }

        /// <summary>Latar gelap selayar penuh (menahan tap ke tombol di belakangnya) + kotak di tengah.</summary>
        static RectTransform Overlay(string name, Transform canvas, out GameObject overlay)
        {
            RectTransform bg = NewRect(name, canvas);
            Stretch(bg);
            AddImage(bg, Dim);   // raycastTarget aktif: tap di luar kotak tidak tembus
            overlay = bg.gameObject;

            RectTransform box = NewRect("Box", bg);
            Stretch(box);
            box.offsetMin = new Vector2(40f, 100f);
            box.offsetMax = new Vector2(-40f, -100f);
            // Kotak tidak tembus pandang supaya tulisan menu di belakangnya tidak mengganggu.
            AddImage(box, new Color(CardColor.r, CardColor.g, CardColor.b, 1f), sliced: true);
            return box;
        }

        // ---------------- Notifikasi badge di ARPlay ----------------

        static void BuildToast(BadgeCatalog catalog)
        {
            var scene = OpenScene(PlayScene, out bool openedHere);
            Transform hud = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "HUD") hud = root.transform;
            if (hud == null)
            {
                Debug.LogError("[WTP] HUD tidak ditemukan di ARPlay.");
                return;
            }
            Transform old = hud.Find("BadgeToast");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            RectTransform card = NewRect("BadgeToast", hud);
            SetRect(card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(900f, 170f));
            AddImage(card, new Color(0.98f, 0.79f, 0.25f, 0.97f), sliced: true).raycastTarget = false;   // tidak menghalangi tap
            TMP_Text title = Text(card, "Title", "", -15, 60, 38, DarkText);
            title.fontStyle = FontStyles.Bold;
            TMP_Text desc = Text(card, "Description", "", -80, 75, 28, DarkText);
            card.SetAsLastSibling();   // di atas semua panel

            var toast = GetOrAdd<BadgeToast>(hud.gameObject);
            var so = new SerializedObject(toast);
            so.FindProperty("panel").objectReferenceValue = card.gameObject;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("descriptionText").objectReferenceValue = desc;
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);

            SaveAndClose(scene, openedHere);
        }

        // ---------------- Helper scene ----------------

        static UnityEngine.SceneManagement.Scene OpenScene(string path, out bool openedHere)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            return scene;
        }

        static void SaveAndClose(UnityEngine.SceneManagement.Scene scene, bool openedHere)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
