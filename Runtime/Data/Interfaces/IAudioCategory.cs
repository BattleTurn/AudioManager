using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    public interface IAudioCategory
    {
        string Name { get; }
        AudioContentBaseSO[] AudioClips { get; }
        AudioClip this[string audioName] { get; }

        public bool TryGetClip(string audioName, out AudioClip clip);
    }
}