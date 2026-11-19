using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Mini-game Filtrasi: susun 4 lapisan saringan dari BAWAH ke ATAS
    /// (kerikil besar -> kerikil kecil -> pasir kasar -> pasir halus).
    /// Kartu lapisan diacak; tiap lapisan yang benar langsung muncul di model Filtrasi di AR
    /// (objek Layer_1..4 dari model FBX). Skor = 100 - 15 per salah, minimal 40.
    /// </summary>
    public class FiltrationGame : MiniGame
    {
        [Tooltip("4 tombol kartu lapisan (urutan di layar akan diacak)")]
        [SerializeField] Button[] cards = new Button[4];
        [Tooltip("Nama lapisan, urut dari bawah (Layer_1) ke atas (Layer_4)")]
        [SerializeField] string[] layerNames = { "Kerikil Besar", "Kerikil Kecil", "Pasir Kasar", "Pasir Halus" };
        [Tooltip("Fungsi tiap lapisan, tampil setelah dipasang (urutan sama dengan layerNames)")]
        [SerializeField] string[] layerInfos =
        {
            "Penyangga paling bawah, air bersih mengalir lewat sini ke pipa keluar.",
            "Menahan pasir supaya tidak jatuh ke kerikil besar.",
            "Menyaring partikel yang agak besar.",
            "Lapisan teratas, menangkap partikel paling halus."
        };
        [SerializeField] TMP_Text stackText;
        [SerializeField] TMP_Text feedbackText;

        [SerializeField] Color cardColor = new Color(0.18f, 0.5f, 0.76f);
        [SerializeField] Color doneColor = new Color(0.2f, 0.65f, 0.35f);
        [SerializeField] Color wrongColor = new Color(0.85f, 0.25f, 0.25f);

        readonly List<GameObject> modelLayers = new List<GameObject>();
        readonly int[] cardLayer = new int[4];   // kartu ke-i berisi lapisan nomor berapa
        int placed;
        int mistakes;

        void Awake()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int cardIndex = i;
                cards[i].onClick.AddListener(() => OnCard(cardIndex));
            }
        }

        protected override void OnBegin()
        {
            // Cari Layer_1..4 di model unit, urut nama = urut dari bawah ke atas, lalu sembunyikan.
            modelLayers.Clear();
            foreach (Transform t in Unit.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Layer_")) modelLayers.Add(t.gameObject);
            modelLayers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (GameObject layer in modelLayers) layer.SetActive(false);

            // Acak lapisan ke kartu (Fisher-Yates).
            for (int i = 0; i < cardLayer.Length; i++) cardLayer[i] = i;
            for (int i = cardLayer.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (cardLayer[i], cardLayer[j]) = (cardLayer[j], cardLayer[i]);
            }
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].GetComponentInChildren<TMP_Text>().text = layerNames[cardLayer[i]];
                cards[i].image.color = cardColor;
                cards[i].interactable = true;
            }

            placed = 0;
            mistakes = 0;
            feedbackText.text = "Pilih lapisan PALING BAWAH dulu.";
            UpdateStack();
        }

        void OnCard(int cardIndex)
        {
            if (placed >= layerNames.Length) return;
            int layer = cardLayer[cardIndex];

            if (layer != placed)
            {
                mistakes++;
                feedbackText.text = "Belum tepat. Ingat: makin ke bawah, butirannya makin besar.";
                StartCoroutine(FlashWrong(cards[cardIndex]));
                return;
            }

            cards[cardIndex].image.color = doneColor;
            cards[cardIndex].interactable = false;
            if (layer < modelLayers.Count) modelLayers[layer].SetActive(true);
            feedbackText.text = $"{layerNames[layer]}: {layerInfos[layer]}";
            placed++;
            UpdateStack();

            if (placed == layerNames.Length) StartCoroutine(Done());
        }

        // Tampilkan tumpukan dari atas ke bawah, slot kosong ditandai "?".
        void UpdateStack()
        {
            var sb = new System.Text.StringBuilder("Saringan (atas)\n");
            for (int i = layerNames.Length - 1; i >= 0; i--)
                sb.Append(i < placed ? $"[ {layerNames[i]} ]\n" : "[ ? ]\n");
            sb.Append("(bawah)");
            stackText.text = sb.ToString();
        }

        IEnumerator FlashWrong(Button card)
        {
            card.image.color = wrongColor;
            yield return new WaitForSeconds(0.5f);
            if (card.interactable) card.image.color = cardColor;
        }

        IEnumerator Done()
        {
            feedbackText.text += "\nSaringan siap dipakai!";
            yield return new WaitForSeconds(2.5f);
            Finish(Mathf.Max(40, 100 - 15 * mistakes));
        }

        public override void Cancel()
        {
            StopAllCoroutines();
            ShowAllLayers();
            base.Cancel();
        }

        // Lapisan model dikembalikan supaya mode lain tetap melihat filter utuh.
        void ShowAllLayers()
        {
            foreach (GameObject layer in modelLayers)
                if (layer != null) layer.SetActive(true);
        }

        void OnDisable() => ShowAllLayers();
    }
}
