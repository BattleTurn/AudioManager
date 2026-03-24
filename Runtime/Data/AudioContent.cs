using System;
using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    [Serializable]
    public class AudioContent
    {
        [NoSpace]
        [Invalid(typeof(CSharpKeywords), nameof(CSharpKeywords.All), autoFix: false)]
        [Tooltip("Unique name for this audio content. Used as a key to retrieve the AudioClip.")]
        public string name;
        public AudioClip clip;
    }
}
