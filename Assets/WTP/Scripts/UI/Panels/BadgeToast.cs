using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WTP.Progress;

namespace WTP.UI.Panels
{
    /// <summary>
    /// Notifikasi kecil "Badge baru!" di atas layar ARPlay setiap kali BadgeManager memberi badge baru.
    /// Kalau beberapa badge didapat sekaligus, ditampilkan bergantian.
    /// </summary>
    public class BadgeToast : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] BadgeCatalog catalog;
        [SerializeField] float showSeconds = 3f;

        readonly Queue<string> waiting = new Queue<string>();
        Coroutine routine;

        void Awake() => panel.SetActive(false);
        void OnEnable() => BadgeManager.BadgeEarned += OnBadgeEarned;
        void OnDisable() => BadgeManager.BadgeEarned -= OnBadgeEarned;

        void OnBadgeEarned(string id)
        {
            waiting.Enqueue(id);
            if (routine == null) routine = StartCoroutine(ShowQueue());
        }

        IEnumerator ShowQueue()
        {
            while (waiting.Count > 0)
            {
                BadgeInfo info = catalog != null ? catalog.Find(waiting.Dequeue()) : null;
                if (info == null) continue;
                titleText.text = $"Badge baru: {info.title}!";
                descriptionText.text = info.description;
                panel.SetActive(true);
                yield return new WaitForSecondsRealtime(showSeconds);   // tetap jalan walau game dijeda
                panel.SetActive(false);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            routine = null;
        }
    }
}
