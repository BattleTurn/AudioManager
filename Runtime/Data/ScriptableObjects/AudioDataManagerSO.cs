using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Runtime
{
    [CreateAssetMenu(fileName = TYPE_NAME, menuName = CREATE_ASSET_PATH)]
    public sealed class AudioDataManagerSO : ScriptableObject
    {
        private const string CREATE_ASSET_PATH = ScriptableConstants.ASSET_MENU_PATH + TYPE_NAME;
        private const string TYPE_NAME = nameof(AudioDataManagerSO);

        [Expandable]
        [SerializeField] private AudioDataBaseSO[] _audioDatas;

        [Foldout(GroupConstants.DEBUG)]
        [ReadOnly]
        [SerializeField]
        private AudioMixer _audioMixer;

        [Foldout(GroupConstants.DEBUG)]
        [ReadOnly]
        [SerializeField]
        private AudioMixerGroup _masterGroup;

        private readonly Dictionary<string, AudioDataBaseSO> _audioDataDict = new();

        #region PROPERTIES
        public AudioDataBaseSO SFXData => TryGetAudioData(AudioNameConstants.SFX, out var sfxData) ? sfxData : null;
        public AudioDataBaseSO MFXData => TryGetAudioData(AudioNameConstants.MFX, out var musicData) ? musicData : null;
        public IReadOnlyList<AudioDataBaseSO> AudioDatas => _audioDatas;
        public AudioMixer AudioMixer => _audioMixer;
        public AudioMixerGroup MasterGroup => _masterGroup;

        public AudioDataBaseSO this[string audioDataName]
        {
            get
            {
                if (TryGetAudioData(audioDataName, out var audioData))
                    return audioData;

                Debug.LogWarning($"AudioDataManager: AudioData name '{audioDataName}' not found");
                return null;
            }
        }
        #endregion

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private bool TryGetAudioData(string audioDataName, out AudioDataBaseSO audioData)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(audioDataName))
            {
                throw new ArgumentException("❌ AudioData name cannot be null or empty", nameof(audioDataName));
            }

            if (_audioDataDict.TryGetValue(audioDataName, out audioData))
            {
                if (audioData == null)
                    throw new NullReferenceException($"❌ AudioData '{audioDataName}' not is null");
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(audioDataName), $"❌ AudioData '{audioDataName}' not in {_audioDatas}");
        }

        private void EnsureInitialized()
        {
            if (_audioDataDict != null && IsAudioDataMapInitialized())
                return;

            _audioDataDict.Clear();
            foreach (var audioData in _audioDatas)
            {
                if (audioData == null)
                    continue;

                var name = audioData.Name;
                Debug.Log($"AudioDataManager: Registering AudioData '{name}' from asset {audioData}");
                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogWarning($"AudioData asset with empty name found: {audioData}");
                    continue;
                }

                if (_audioDataDict.ContainsKey(name))
                {
                    Debug.LogWarning($"Duplicate AudioData name detected: {name} in asset {audioData}");
                    continue;
                }

                _audioDataDict[name] = audioData;
            }
        }

        private bool IsAudioDataMapInitialized()
        {
            if (_audioDataDict == null || _audioDataDict.Count == 0)
                return false;

            if (_audioDatas.Length != _audioDataDict.Count)
                return false;

            foreach (var audioData in _audioDatas)
            {
                if (audioData == null)
                    return false;

                var name = audioData.Name;
                if (string.IsNullOrEmpty(name))
                    return false;

                if (!_audioDataDict.ContainsKey(name))
                    return false;

                if (_audioDataDict[name] != audioData)
                    return false;
            }

            return true;
        }
    }
}