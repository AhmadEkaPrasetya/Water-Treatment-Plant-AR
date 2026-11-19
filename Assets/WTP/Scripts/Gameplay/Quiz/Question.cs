using System;
using UnityEngine;

namespace WTP.Gameplay.Quiz
{
    /// <summary>
    /// Satu soal pilihan ganda + pembahasannya.
    /// Tulis jawaban benar di options[correctIndex]; urutan pilihan diacak saat dimainkan.
    /// </summary>
    [Serializable]
    public class Question
    {
        [TextArea(2, 4)] public string text;
        public string[] options = new string[4];
        public int correctIndex;
        [TextArea(2, 5)] public string explanation;
    }
}
