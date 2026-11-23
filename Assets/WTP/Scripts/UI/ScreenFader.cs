using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WTP.UI
{
    /// <summary>
    /// Transisi pindah scene: layar menggelap -> scene baru dimuat -> layar terang lagi.
    /// Dibuat sendiri lewat kode (kanvas paling atas + gambar hitam), ikut pindah scene.
    /// Dipanggil SceneLoader.Load(), jadi script lain tidak perlu memakainya langsung.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        const float FadeSeconds = 0.35f;

        static ScreenFader instance;

        CanvasGroup group;
        bool busy;

        static ScreenFader Instance
        {
            get
            {
                if (instance == null) instance = Create();
                return instance;
            }
        }

        static ScreenFader Create()
        {
            var go = new GameObject(nameof(ScreenFader));
            DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;              // di atas semua UI game
            go.AddComponent<GraphicRaycaster>();     // supaya tap tertahan selama transisi

            var image = new GameObject("Black").AddComponent<Image>();
            image.transform.SetParent(go.transform, false);
            image.color = new Color(0.02f, 0.08f, 0.14f);  // biru tua, senada tema air
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var fader = go.AddComponent<ScreenFader>();
            fader.group = go.AddComponent<CanvasGroup>();
            fader.group.alpha = 0f;
            fader.group.blocksRaycasts = false;
            return fader;
        }

        /// <summary>Gelapkan layar, muat scene, lalu terangkan lagi.</summary>
        public static void LoadScene(string sceneName)
        {
            if (Instance.busy) return;   // cegah tombol ditekan dua kali
            Instance.StartCoroutine(Instance.FadeAndLoad(sceneName));
        }

        IEnumerator FadeAndLoad(string sceneName)
        {
            busy = true;
            group.blocksRaycasts = true;
            yield return Fade(0f, 1f);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone) yield return null;
            yield return null;   // satu frame lagi supaya Start() scene baru sudah jalan

            yield return Fade(1f, 0f);
            group.blocksRaycasts = false;
            busy = false;
        }

        // unscaledDeltaTime: tetap jalan walau game sedang dijeda (Time.timeScale = 0).
        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / FadeSeconds);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
