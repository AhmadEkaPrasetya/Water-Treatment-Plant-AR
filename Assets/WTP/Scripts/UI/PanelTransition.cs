using System.Collections;
using UnityEngine;

namespace WTP.UI
{
    /// <summary>
    /// Animasi muncul untuk panel UI: setiap kali panel diaktifkan (SetActive(true)),
    /// panel memudar masuk sambil membesar sedikit (92% -> 100%).
    /// Dipasang otomatis ke panel-panel game lewat menu WTP > Setup Audio & Effects.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelTransition : MonoBehaviour
    {
        [SerializeField] float duration = 0.2f;
        [SerializeField] float startScale = 0.92f;

        CanvasGroup group;
        RectTransform rect;
        Vector3 baseScale;   // skala asli panel (tidak selalu 1)

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            baseScale = rect.localScale;
        }

        void OnEnable() => StartCoroutine(Appear());

        void OnDisable()
        {
            // Kembalikan ke kondisi normal supaya panel tidak "nyangkut" setengah transparan.
            group.alpha = 1f;
            rect.localScale = baseScale;
        }

        IEnumerator Appear()
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                // Ease-out: cepat di awal, melambat di akhir.
                float k = 1f - (1f - t / duration) * (1f - t / duration);
                group.alpha = k;
                rect.localScale = baseScale * Mathf.Lerp(startScale, 1f, k);
                yield return null;
            }
            group.alpha = 1f;
            rect.localScale = baseScale;
        }
    }
}
