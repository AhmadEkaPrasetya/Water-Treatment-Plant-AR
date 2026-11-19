using System;
using UnityEngine;

namespace WTP.Story
{
    /// <summary>Satu baris dialog: siapa yang bicara + teksnya. {player} diganti nama pemain.</summary>
    [Serializable]
    public class DialogueLine
    {
        public Character speaker;
        [TextArea(2, 5)] public string text;
    }
}
