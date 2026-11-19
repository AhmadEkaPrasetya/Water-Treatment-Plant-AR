using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Core;
using WTP.Progress;

namespace WTP.UI.Panels
{
    /// <summary>
    /// Layar menu utama: isi nama pemain, tombol Main / Leaderboard / Badge / Pengaturan / Keluar.
    /// Fungsi On...() dipanggil oleh tombol (diatur di Inspector, bagian On Click).
    /// </summary>
    public class MainMenuPanel : MonoBehaviour
    {
        const int MaxNameLength = 16;

        [Header("Nama pemain")]
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] TMP_Text warningText;
        [SerializeField] TMP_Text progressText;

        [Header("Pengaturan")]
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;

        [Header("Pesan singkat")]
        [SerializeField] TMP_Text toastText;
        [SerializeField] float toastDuration = 2f;

        Coroutine toastRoutine;

        void Start()
        {
            PlayerData data = SaveSystem.Data;

            nameInput.characterLimit = MaxNameLength;
            nameInput.text = data.playerName;
            musicSlider.value = data.musicVolume;
            sfxSlider.value = data.sfxVolume;

            progressText.text = data.HasName
                ? $"Halo, {data.playerName}!  Bintang: {data.TotalStars}/{PlayerData.BelajarLevelCount * 3}"
                : "Halo, calon operator WTP!";

            warningText.gameObject.SetActive(false);
            settingsPanel.SetActive(false);
            toastText.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ tombol

        public void OnPlay()
        {
            string playerName = nameInput.text.Trim();
            if (playerName.Length == 0)
            {
                // Nama wajib diisi karena dipakai di dialog dan leaderboard.
                warningText.text = "Isi nama kamu dulu ya!";
                warningText.gameObject.SetActive(true);
                nameInput.ActivateInputField();
                return;
            }

            SaveSystem.Data.playerName = playerName;
            SaveSystem.Save();
            SceneLoader.Load(SceneLoader.ARPlay);
        }

        // Layar Leaderboard & Badge dikerjakan di Minggu 3.
        public void OnLeaderboard() => ShowToast("Leaderboard segera hadir!");
        public void OnBadges() => ShowToast("Koleksi badge segera hadir!");

        public void OnOpenSettings() => settingsPanel.SetActive(true);

        public void OnCloseSettings()
        {
            SaveSystem.Data.musicVolume = musicSlider.value;
            SaveSystem.Data.sfxVolume = sfxSlider.value;
            SaveSystem.Save();
            settingsPanel.SetActive(false);
        }

        public void OnQuit()
        {
            SaveSystem.Save();
            Application.Quit(); // tidak berpengaruh di Editor, hanya di HP
        }

        // ------------------------------------------------------------ pesan singkat

        void ShowToast(string message)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(message));
        }

        IEnumerator ToastRoutine(string message)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            toastText.gameObject.SetActive(false);
        }
    }
}
