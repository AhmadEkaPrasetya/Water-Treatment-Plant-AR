using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WTP.AR;
using WTP.Plant;
using WTP.Progress;

namespace WTP.Core
{
    /// <summary>
    /// Pengatur alur game. Hanya ada satu dan tetap hidup saat pindah scene.
    /// Dibuat otomatis saat game mulai (tidak perlu ditaruh di scene).
    ///
    /// Tugas:
    /// - menyimpan status game (Menu / Scanning / Playing / Paused / Result)
    /// - tahu WTP mana yang sedang ditaruh di AR (Plant)
    /// - mencatat level yang selesai dan membuka level berikutnya
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        public PlantController Plant { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<PlantController> PlantReady;

        ARPlacementManager placement;

        // Jalan otomatis sekali setelah scene pertama dimuat, di scene apa pun.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance == null) new GameObject(nameof(GameManager)).AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            Plant = null;

            if (placement != null)
            {
                placement.PlantPlaced -= OnPlantPlaced;
                placement.PlacementReset -= OnPlacementReset;
            }

            // Scene AR punya ARPlacementManager; scene menu tidak.
            placement = FindFirstObjectByType<ARPlacementManager>();
            if (placement != null)
            {
                placement.PlantPlaced += OnPlantPlaced;
                placement.PlacementReset += OnPlacementReset;
                SetState(GameState.Scanning);
            }
            else
            {
                SetState(GameState.Menu);
            }
        }

        void OnPlantPlaced(GameObject plantObject)
        {
            Plant = plantObject.GetComponent<PlantController>();
            // Tampilkan kondisi WTP sesuai progres pemain: unit yang levelnya sudah selesai berfungsi.
            Plant.SetWorkingUnitCount(CompletedLevelCount, instant: true);
            SetState(GameState.Playing);
            PlantReady?.Invoke(Plant);
        }

        void OnPlacementReset()
        {
            Plant = null;
            SetState(GameState.Scanning);
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            StateChanged?.Invoke(newState);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f; // semua yang memakai Time.deltaTime berhenti
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>Jumlah level Belajar yang sudah selesai berurutan dari level 1.</summary>
        public int CompletedLevelCount
        {
            get
            {
                int[] stars = SaveSystem.Data.belajarStars;
                int count = 0;
                while (count < stars.Length && stars[count] > 0) count++;
                return count;
            }
        }

        /// <summary>
        /// Dipanggil LevelController saat level Belajar selesai.
        /// Menyimpan bintang, lalu unit yang baru diperbaiki langsung berfungsi di AR.
        /// </summary>
        public void CompleteLevel(int levelIndex, int stars)
        {
            SaveSystem.Data.SetStars(levelIndex, stars);
            BadgeManager.Evaluate();   // mis. Penjaga Intake (level 1 bintang 3)
            SaveSystem.Save();
            if (Plant != null) Plant.SetWorkingUnitCount(CompletedLevelCount);
            SetState(GameState.Result);
        }
    }
}
