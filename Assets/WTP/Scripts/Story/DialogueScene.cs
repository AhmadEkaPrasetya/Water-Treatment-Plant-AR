using UnityEngine;

namespace WTP.Story
{
    /// <summary>
    /// Satu adegan cerita berisi beberapa baris dialog (mis. prolog, tamat).
    /// Buat aset baru: Create > WTP > Dialogue Scene.
    /// </summary>
    [CreateAssetMenu(menuName = "WTP/Dialogue Scene", fileName = "DialogueScene")]
    public class DialogueScene : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
