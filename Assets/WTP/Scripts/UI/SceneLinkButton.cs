using UnityEngine;
using UnityEngine.UI;
using WTP.Core;

namespace WTP.UI
{
    /// <summary>Pasang di Button: saat ditekan, pindah ke scene yang ditulis di Inspector.</summary>
    [RequireComponent(typeof(Button))]
    public class SceneLinkButton : MonoBehaviour
    {
        [SerializeField] string sceneName = SceneLoader.MainMenu;

        void Awake() => GetComponent<Button>().onClick.AddListener(() => SceneLoader.Load(sceneName));
    }
}
