using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Runtime
{
    public abstract class AudioDataBaseSO : ScriptableObject
    {
        [Expandable]
        [SerializeField] protected AudioCategoryBaseSO[] _audioCategories;
        [SerializeField] protected AudioMixerGroup _mixerGroup;

        private Dictionary<string, AudioCategoryBaseSO> _audioCategoryDict;

        #region PROPERTIES
        public abstract string Name { get; }
        public AudioCategoryBaseSO[] AudioCategories => _audioCategories;
        public AudioMixerGroup MixerGroup => _mixerGroup;
        #endregion

        public AudioCategoryBaseSO this[string categoryName]
        {
            get
            {
                _audioCategoryDict ??= AudioDataHelper.BuildCategoryDictionary(_audioCategories);
                return _audioCategoryDict[categoryName];
            }
        }

        public AudioClip this[string categoryName, string audioName]
        {
            get
            {
                if (TryGetClip(categoryName, audioName, out var clip))
                    return clip;

                Debug.LogWarning($"AudioData: Audio name '{audioName}' in category '{categoryName}' not found");
                return null;
            }
        }

        public bool TryGetClip(string categoryName, string audioName, out AudioClip clip)
        {
            _audioCategoryDict ??= AudioDataHelper.BuildCategoryDictionary(_audioCategories);
            if (_audioCategoryDict.TryGetValue(categoryName, out var category))
                return AudioDataHelper.TryGetClip(audioName, category, out clip);
            else
            {
                clip = null;
                return false;
            }
        }
    }
}