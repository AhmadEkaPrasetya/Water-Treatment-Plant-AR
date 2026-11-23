using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Audio;

namespace WTP.UI.Panels
{
    /// <summary>Layar hasil level: judul, skor, rincian, 3 bintang, tombol Lanjut.</summary>
    public class LevelResultPanel : MonoBehaviour
    {
        static readonly Color StarOn = new Color(1f, 0.84f, 0.29f);
        static readonly Color StarOff = new Color(1f, 1f, 1f, 0.2f);

        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] Image[] starImages = new Image[3];
        [SerializeField] Button continueButton;

        Action onContinue;

        void Awake() => continueButton.onClick.AddListener(() =>
        {
            Action callback = onContinue;
            onContinue = null;
            callback?.Invoke();
        });

        public void Show(string title, int score, int maxScore, int stars, string detail, Action onContinueClicked)
        {
            onContinue = onContinueClicked;
            titleText.text = title;
            scoreText.text = $"Skor {score} / {maxScore}";
            detailText.text = detail;
            panel.SetActive(true);
            if (isActiveAndEnabled) StartCoroutine(RevealStars(stars));
            else for (int i = 0; i < starImages.Length; i++) starImages[i].color = i < stars ? StarOn : StarOff;
        }

        /// <summary>Bintang muncul satu per satu (membal + bunyi makin tinggi), lalu suara menang/gagal.</summary>
        IEnumerator RevealStars(int stars)
        {
            foreach (Image star in starImages)
            {
                star.color = StarOff;
                star.rectTransform.localScale = Vector3.one;
            }
            AudioManager.Play(stars > 0 ? Sfx.Win : Sfx.Fail);
            yield return new WaitForSecondsRealtime(0.5f);

            for (int i = 0; i < stars && i < starImages.Length; i++)
            {
                starImages[i].color = StarOn;
                AudioManager.Play(Sfx.Star, 0.8f, 1f + 0.12f * i);
                RectTransform r = starImages[i].rectTransform;
                // Membal: 1.5x lalu mengecil kembali ke ukuran normal.
                for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
                {
                    r.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, t / 0.25f);
                    yield return null;
                }
                r.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        public void Hide() => panel.SetActive(false);
    }
}
