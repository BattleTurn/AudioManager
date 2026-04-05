using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    public interface IAudioCategory
    {
        string Name { get; }
        AudioContentBaseSO[] AudioClips { get; }
        AudioClip this[string audioName] { get; }

        public bool TryGetClip(string audioName, out AudioClip clip);
    }
}