using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WTP.Core;
using WTP.Progress;

namespace WTP.Audio
{
    /// <summary>
    /// Pemutar semua suara game. Hanya ada satu, dibuat otomatis saat game mulai, ikut pindah scene
    /// (sama seperti GameManager). Modul lain cukup memanggil fungsi statis:
    ///   AudioManager.Play(Sfx.Correct);
    ///   AudioManager.PlayMusic(Music.Shift);
    ///
    /// Otomatis:
    /// - musik latar sesuai scene (MainMenu = Menu, ARPlay = Play), berganti dengan fade
    /// - semua Button di scene berbunyi "klik" saat ditekan
    /// - volume musik & efek diambil dari PlayerData (diatur di layar Pengaturan)
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const float FadeSeconds = 0.8f;

        static AudioManager instance;

        AudioLibrary library;
        AudioSource musicSource;
        AudioSource sfxSource;
        AudioListener ownListener;
        Music currentMusic = Music.None;
        Coroutine fadeRoutine;
        float musicVolume = 1f;
        float sfxVolume = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance == null) new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            library = Resources.Load<AudioLibrary>("AudioLibrary");
            if (library == null) Debug.LogWarning("[WTP] AudioLibrary tidak ada di Resources. Jalankan menu WTP > Setup Audio & Effects.");

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            // "Telinga" cadangan: scene MainMenu tidak punya kamera ber-AudioListener, jadi tanpa ini musik tidak terdengar.
            ownListener = gameObject.AddComponent<AudioListener>();

            musicVolume = SaveSystem.Data.musicVolume;
            sfxVolume = SaveSystem.Data.sfxVolume;
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
        void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (this != instance) return;
            UpdateListener();
            PlayMusic(scene.name == SceneLoader.MainMenu ? Music.Menu : Music.Play);
            AddClickSounds();
        }

        // Pakai AudioListener sendiri hanya kalau scene tidak punya (mis. ARPlay sudah punya di kamera AR).
        // Dua listener aktif sekaligus membuat Unity memberi peringatan terus-menerus.
        void UpdateListener()
        {
            bool sceneHasListener = false;
            foreach (AudioListener l in FindObjectsByType<AudioListener>())
                if (l != ownListener && l.enabled) sceneHasListener = true;
            ownListener.enabled = !sceneHasListener;
        }

        // Pasang bunyi klik ke semua tombol di scene, termasuk yang sedang tersembunyi.
        static void AddClickSounds()
        {
            foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include))
                b.onClick.AddListener(() => Play(Sfx.Click));
        }

        // ------------------------------------------------------------ API statis

        /// <summary>Putar efek suara sekali. pitch &gt; 1 = lebih tinggi (mis. bintang ke-2, ke-3).</summary>
        public static void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
        {
            if (instance == null || instance.library == null) return;
            AudioClip clip = instance.library.Get(sfx);
            if (clip == null) return;
            // PlayOneShot tidak bisa mengubah pitch per klip, jadi untuk pitch lain pakai AudioSource sementara.
            if (Mathf.Approximately(pitch, 1f))
            {
                instance.sfxSource.PlayOneShot(clip, volume * instance.sfxVolume);
                return;
            }
            var temp = instance.gameObject.AddComponent<AudioSource>();
            temp.clip = clip;
            temp.pitch = pitch;
            temp.volume = volume * instance.sfxVolume;
            temp.Play();
            Destroy(temp, clip.length / pitch + 0.1f);
        }

        /// <summary>Ganti musik latar (fade keluar-masuk). Music.None = hening.</summary>
        public static void PlayMusic(Music music)
        {
            if (instance == null || instance.currentMusic == music) return;
            instance.currentMusic = music;
            if (instance.fadeRoutine != null) instance.StopCoroutine(instance.fadeRoutine);
            AudioClip clip = instance.library != null ? instance.library.Get(music) : null;
            instance.fadeRoutine = instance.StartCoroutine(instance.SwitchMusic(clip));
        }

        public static void SetMusicVolume(float volume)
        {
            if (instance == null) return;
            instance.musicVolume = volume;
            if (instance.fadeRoutine == null) instance.musicSource.volume = volume;
        }

        public static void SetSfxVolume(float volume)
        {
            if (instance != null) instance.sfxVolume = volume;
        }

        // ------------------------------------------------------------ fade musik

        IEnumerator SwitchMusic(AudioClip clip)
        {
            // Turunkan volume musik lama ke 0...
            float start = musicSource.volume;
            for (float t = 0f; t < FadeSeconds && musicSource.isPlaying; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(start, 0f, t / FadeSeconds);
                yield return null;
            }
            musicSource.Stop();

            // ...lalu naikkan musik baru dari 0.
            if (clip != null)
            {
                musicSource.clip = clip;
                musicSource.volume = 0f;
                musicSource.Play();
                for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
                {
                    musicSource.volume = Mathf.Lerp(0f, musicVolume, t / FadeSeconds);
                    yield return null;
                }
                musicSource.volume = musicVolume;
            }
            fadeRoutine = null;
        }
    }
}
