using UnityEngine;

namespace WTP.Audio
{
    /// <summary>
    /// (ScriptableObject) Kumpulan semua klip suara. Asetnya HARUS ada di
    /// Assets/WTP/Resources/AudioLibrary.asset supaya AudioManager bisa memuatnya sendiri.
    /// Diisi otomatis lewat menu WTP > Setup Audio & Effects.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Tooltip("Urutan sama dengan enum Sfx")]
        public AudioClip[] sfx = new AudioClip[0];
        [Tooltip("Urutan sama dengan enum Music (indeks 0 = None, dibiarkan kosong)")]
        public AudioClip[] music = new AudioClip[0];

        public AudioClip Get(Sfx s) => (int)s < sfx.Length ? sfx[(int)s] : null;
        public AudioClip Get(Music m) => (int)m < music.Length ? music[(int)m] : null;
    }
}
