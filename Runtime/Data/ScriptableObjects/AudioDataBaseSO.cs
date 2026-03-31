using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Runtime
{
    public abstract class AudioDataBaseSO : ScriptableObject
    {
        [SerializeField] private AudioContent[] _audioContents;
        [SerializeField] private AudioMixerGroup _mixerGroup;

        private Dictionary<string, AudioClip> _audioContentDict;

        public abstract string Name { get; }
        public AudioContent[] AudioContents => _audioContents;
        public AudioMixerGroup MixerGroup => _mixerGroup;

        public AudioClip this[string audioName]
        {
            get
            {
                if (TryGetClip(audioName, out var clip))
                    return clip;

                Debug.LogWarning($"AudioData: Audio name '{audioName}' not found");
                return null;
            }
        }

        public bool TryGetClip(string audioName, out AudioClip clip)
        {
            _audioContentDict ??= AudioDataHelper.BuildDictionary(_audioContents);
            return AudioDataHelper.TryGetClip(audioName, _audioContentDict, out clip);
        }
    }
}