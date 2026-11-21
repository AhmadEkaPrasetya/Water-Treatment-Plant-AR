using System;
using UnityEngine;
using WTP.Core;
using WTP.Gameplay.Quiz;
using WTP.Plant;
using WTP.Progress;
using WTP.Story;
using WTP.UI.Panels;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Alur Bagian 1 (Belajar), dibuka SectionController lewat Open():
    ///   pilih level (atau Kembali ke Pilih Bagian)
    ///   -> penjelasan Tirta -> mini-game -> kuis 3 soal -> hasil & bintang
    ///   -> unit jadi berfungsi di AR -> dialog penutup -> kembali ke pilih level
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [Header("Konten (Assets/WTP/Data)")]
        [Tooltip("7 level urut: Intake ... Reservoir")]
        [SerializeField] LevelData[] levels = new LevelData[7];
        [Tooltip("Mini-game per level (urutan sama). Kosong = langsung ke kuis.")]
        [SerializeField] MiniGame[] miniGames = new MiniGame[7];

        [Header("Komponen di scene")]
        [SerializeField] DialogueManager dialogue;
        [SerializeField] QuizManager quiz;
        [SerializeField] LevelSelectPanel levelSelect;
        [SerializeField] LevelResultPanel resultPanel;

        PlantController plant;
        Action onExit;
        int currentLevel = -1;
        int miniGameScore;

        static string PlayerName => SaveSystem.Data.HasName ? SaveSystem.Data.playerName : "kawan";

        // Start, bukan OnEnable: GameManager dibuat otomatis setelah Awake/OnEnable scene pertama.
        void Start()
        {
            GameManager.Instance.StateChanged += OnStateChanged;
            levelSelect.Hide();
            resultPanel.Hide();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StateChanged -= OnStateChanged;
        }

        /// <summary>Dipanggil SectionController saat pemain memilih Belajar.</summary>
        public void Open(PlantController placedPlant, Action onExitSection)
        {
            plant = placedPlant;
            onExit = onExitSection;
            ShowLevelSelect();
        }

        void Exit()
        {
            levelSelect.Hide();
            Action callback = onExit;
            onExit = null;
            callback?.Invoke();
        }

        // WTP ditaruh ulang -> batalkan apa pun yang sedang berjalan.
        void OnStateChanged(GameState state)
        {
            if (state != GameState.Scanning) return;
            dialogue.Stop();
            quiz.Stop();
            levelSelect.Hide();
            resultPanel.Hide();
            if (currentLevel >= 0 && miniGames[currentLevel] != null) miniGames[currentLevel].Cancel();
            currentLevel = -1;
            plant = null;
            onExit = null;
        }

        void ShowLevelSelect()
        {
            int count = levels.Length;
            var titles = new string[count];
            var stars = new int[count];
            var unlocked = new bool[count];
            for (int i = 0; i < count; i++)
            {
                titles[i] = $"{i + 1}. {levels[i].title}";
                stars[i] = SaveSystem.Data.belajarStars[i];
                unlocked[i] = SaveSystem.Data.IsLevelUnlocked(i);
            }
            levelSelect.Show(titles, stars, unlocked, StartLevel, Exit);
        }

        void StartLevel(int levelIndex)
        {
            levelSelect.Hide();
            currentLevel = levelIndex;
            dialogue.Play(levels[levelIndex].intro, PlayerName, RunMiniGame);
        }

        void RunMiniGame()
        {
            MiniGame game = miniGames[currentLevel];
            if (game == null)
            {
                // Mini-game unit ini belum dibuat: anggap nilai penuh, lanjut kuis.
                miniGameScore = ScoreManager.MaxMiniGameScore;
                StartQuiz();
                return;
            }

            game.Finished += OnMiniGameFinished;
            game.Begin(plant.GetUnit(levels[currentLevel].unit));
        }

        void OnMiniGameFinished(int score)
        {
            miniGames[currentLevel].Finished -= OnMiniGameFinished;
            miniGameScore = score;
            StartQuiz();
        }

        void StartQuiz() => quiz.StartQuiz(levels[currentLevel].questions, ShowResult);

        void ShowResult(int correctAnswers)
        {
            int score = ScoreManager.LevelScore(miniGameScore, correctAnswers);
            int stars = ScoreManager.StarsFor(score);

            // Simpan progres + unit ini langsung berfungsi (air di AR mulai jernih).
            GameManager.Instance.CompleteLevel(currentLevel, stars);

            string detail = $"Mini-game: {miniGameScore}/{ScoreManager.MaxMiniGameScore}\n" +
                            $"Kuis: {correctAnswers}/{ScoreManager.QuizQuestionsPerLevel} benar";
            resultPanel.Show($"{levels[currentLevel].title} diperbaiki!", score, ScoreManager.MaxLevelScore,
                             stars, detail, PlayOutro);
        }

        void PlayOutro()
        {
            resultPanel.Hide();
            dialogue.Play(levels[currentLevel].outro, PlayerName, () =>
            {
                currentLevel = -1;
                GameManager.Instance.SetState(GameState.Playing);
                ShowLevelSelect();
            });
        }
    }
}
