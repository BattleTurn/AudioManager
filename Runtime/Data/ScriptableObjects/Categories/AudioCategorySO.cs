using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    [CreateAssetMenu(fileName = "New " + TYPE_NAME, menuName = CREATE_ASSET_PATH)]
    public class AudioCategorySO : AudioCategoryBaseSO
    {
        private const string CREATE_ASSET_PATH = ScriptableConstants.ASSET_MENU_PATH + TYPE_NAME;
        private const string TYPE_NAME = nameof(AudioCategorySO);

        [Expandable]
        [SerializeField] private AudioContentBaseSO[] _audioContents;

        private Dictionary<string, AudioClip> _audioContentDict;

        #region PROPERTIES
        public override AudioContentBaseSO[] AudioClips => _audioContents;
        public override AudioClip this[string audioName]
        {
            get
            {
                if (TryGetClip(audioName, out var clip))
                    return clip;

                Debug.LogWarning($"AudioData: Audio name '{audioName}' not found");
                return null;
            }
        }
        #endregion

        public override bool TryGetClip(string audioName, out AudioClip clip)
        {
            _audioContentDict ??= AudioDataHelper.BuildDictionary(_audioContents);
            return AudioDataHelper.TryGetClip(audioName, _audioContentDict, out clip);
        }
    }
}