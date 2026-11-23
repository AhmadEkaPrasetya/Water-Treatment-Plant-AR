using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Audio;

namespace WTP.Gameplay.Quiz
{
    /// <summary>
    /// Menjalankan kuis pilihan ganda:
    /// tampilkan soal -> pemain pilih jawaban -> tandai benar (hijau) / salah (merah)
    /// -> tampilkan pembahasan -> tombol Lanjut -> soal berikutnya.
    /// Setelah soal terakhir, onFinished dipanggil dengan jumlah jawaban benar.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        static readonly Color NormalColor = new Color(0.18f, 0.5f, 0.76f);
        static readonly Color CorrectColor = new Color(0.2f, 0.7f, 0.35f);
        static readonly Color WrongColor = new Color(0.85f, 0.3f, 0.3f);

        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text progressText;
        [SerializeField] TMP_Text questionText;
        [SerializeField] Button[] optionButtons = new Button[4];
        [SerializeField] GameObject explanationBox;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text explanationText;
        [SerializeField] Button nextButton;

        IReadOnlyList<Question> questions;
        Action<int> onFinished;
        int current;
        int correctCount;
        // order[i] = indeks pilihan asli yang ditampilkan di tombol ke-i (hasil acak)
        readonly int[] order = { 0, 1, 2, 3 };

        void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int buttonIndex = i; // salin ke variabel lokal supaya lambda menangkap nilai yang benar
                optionButtons[i].onClick.AddListener(() => OnAnswer(buttonIndex));
            }
            nextButton.onClick.AddListener(OnNext);
            panel.SetActive(false);
        }

        public void StartQuiz(IReadOnlyList<Question> quizQuestions, Action<int> onFinished)
        {
            questions = quizQuestions;
            this.onFinished = onFinished;
            current = 0;
            correctCount = 0;
            panel.SetActive(true);
            ShowQuestion();
        }

        public void Stop()
        {
            onFinished = null;
            panel.SetActive(false);
        }

        void ShowQuestion()
        {
            Question q = questions[current];
            progressText.text = $"Soal {current + 1} dari {questions.Count}";
            questionText.text = q.text;

            Shuffle(order);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button b = optionButtons[i];
                b.GetComponentInChildren<TMP_Text>().text = q.options[order[i]];
                b.image.color = NormalColor;
                b.interactable = true;
            }
            explanationBox.SetActive(false);
        }

        void OnAnswer(int buttonIndex)
        {
            Question q = questions[current];
            bool correct = order[buttonIndex] == q.correctIndex;
            if (correct) correctCount++;
            AudioManager.Play(correct ? Sfx.Correct : Sfx.Wrong);

            // Kunci semua tombol, warnai jawaban benar hijau dan pilihan salah merah.
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = false;
                if (order[i] == q.correctIndex) optionButtons[i].image.color = CorrectColor;
                else if (i == buttonIndex) optionButtons[i].image.color = WrongColor;
            }

            resultText.text = correct ? "Benar!" : "Kurang tepat...";
            resultText.color = correct ? CorrectColor : WrongColor;
            explanationText.text = q.explanation;
            explanationBox.SetActive(true);
        }

        void OnNext()
        {
            current++;
            if (current < questions.Count)
            {
                ShowQuestion();
                return;
            }
            panel.SetActive(false);
            Action<int> callback = onFinished;
            onFinished = null;
            callback?.Invoke(correctCount);
        }

        // Acak Fisher-Yates: tukar tiap elemen dengan elemen acak di depannya.
        static void Shuffle(int[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
