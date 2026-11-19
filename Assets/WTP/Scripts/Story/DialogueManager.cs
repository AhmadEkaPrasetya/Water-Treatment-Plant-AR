using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WTP.Story
{
    /// <summary>
    /// Menampilkan dialog baris per baris di kotak dialog bawah layar.
    /// Teks muncul huruf demi huruf (efek mesin ketik). Tap kotak dialog:
    /// - saat teks masih muncul -> langsung tampilkan semua
    /// - saat teks sudah lengkap -> lanjut ke baris berikutnya
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button tapArea;
        [SerializeField] Image nameTag;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text lineText;
        [SerializeField] GameObject nextIndicator;
        [Tooltip("Huruf per detik")]
        [SerializeField] float typingSpeed = 45f;

        IReadOnlyList<DialogueLine> lines;
        int index;
        string playerName;
        Action onFinished;
        Coroutine typing;

        public bool IsPlaying => panel.activeSelf;

        void Awake()
        {
            tapArea.onClick.AddListener(OnTap);
            panel.SetActive(false);
        }

        /// <summary>Putar dialog. onFinished dipanggil setelah baris terakhir ditutup.</summary>
        public void Play(IReadOnlyList<DialogueLine> dialogueLines, string playerName, Action onFinished)
        {
            lines = dialogueLines;
            this.playerName = playerName;
            this.onFinished = onFinished;
            index = 0;

            if (lines == null || lines.Count == 0)
            {
                Finish();
                return;
            }
            panel.SetActive(true);
            ShowLine();
        }

        /// <summary>Hentikan dialog tanpa memanggil onFinished (mis. saat WTP ditaruh ulang).</summary>
        public void Stop()
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            onFinished = null;
            panel.SetActive(false);
        }

        void ShowLine()
        {
            DialogueLine line = lines[index];
            Character speaker = line.speaker;
            nameText.text = speaker != null ? speaker.displayName : "";
            nameTag.color = speaker != null ? speaker.nameColor : Color.gray;
            lineText.text = line.text.Replace("{player}", playerName);

            if (typing != null) StopCoroutine(typing);
            typing = StartCoroutine(TypeRoutine());
        }

        IEnumerator TypeRoutine()
        {
            nextIndicator.SetActive(false);
            lineText.ForceMeshUpdate(); // supaya jumlah huruf (characterCount) sudah dihitung
            int total = lineText.textInfo.characterCount;
            float shown = 0f;
            lineText.maxVisibleCharacters = 0;

            while (shown < total)
            {
                shown += typingSpeed * Time.unscaledDeltaTime; // tetap jalan walau game dijeda
                lineText.maxVisibleCharacters = Mathf.Min(total, (int)shown);
                yield return null;
            }
            CompleteTyping();
        }

        void CompleteTyping()
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            lineText.maxVisibleCharacters = int.MaxValue;
            nextIndicator.SetActive(true);
        }

        void OnTap()
        {
            if (typing != null)
            {
                CompleteTyping();
                return;
            }

            index++;
            if (index < lines.Count) ShowLine();
            else Finish();
        }

        void Finish()
        {
            panel.SetActive(false);
            Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }
    }
}
