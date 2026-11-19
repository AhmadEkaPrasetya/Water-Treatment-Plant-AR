using UnityEngine;
using WTP.Gameplay.Quiz;
using WTP.Plant;
using WTP.Story;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Isi satu level Bagian 1 (satu unit WTP): penjelasan Tirta, dialog penutup, dan 3 soal kuis.
    /// Aset ada di Assets/WTP/Data/Levels. Buat baru: Create > WTP > Level Data.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Level Data", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        public UnitType unit;
        public string title;
        [Tooltip("Penjelasan sebelum mini-game")]
        public DialogueLine[] intro;
        [Tooltip("Dialog setelah unit berhasil diperbaiki")]
        public DialogueLine[] outro;
        public Question[] questions;
    }
}
