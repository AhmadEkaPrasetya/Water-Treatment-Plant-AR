using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].color = i < stars ? StarOn : StarOff;
            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);
    }
}
