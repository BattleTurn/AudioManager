using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    [CreateAssetMenu(fileName = "AudioContent", menuName = ScriptableConstants.ASSET_MENU_PATH + "AudioContent")]
    public class AudioContentSO : AudioContentBaseSO
    {
        [NoSpace]
        [Invalid(typeof(CSharpKeywords), nameof(CSharpKeywords.All), autoFix: false)]
        [Tooltip("Unique name for this audio content. Used as a key to retrieve the AudioClip.")]
        [SerializeField] private string _name;
        [SerializeField] private AudioClip _clip;

        public override string Name => _name;
        public override AudioClip Clip => _clip;
    }

    public abstract class AudioContentBaseSO : ScriptableObject
    {
        public abstract string Name { get; }
        public abstract AudioClip Clip { get; }
    }
}
