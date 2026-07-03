using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManagement.Runtime
{
    [CreateAssetMenu(fileName = TYPE_NAME, menuName = CREATE_ASSET_PATH)]
    public sealed class AudioAlbumManagerSO : ScriptableObject
    {
        private const string CREATE_ASSET_PATH = ScriptableConstants.ASSET_MENU_PATH + TYPE_NAME;
        private const string TYPE_NAME = nameof(AudioAlbumManagerSO);

        [Expandable]
        [SerializeField] private AudioAlbumBaseSO[] _audioAlbums;

        [Foldout(GroupConstants.DEBUG)]
        [ReadOnly]
        [SerializeField]
        private AudioMixer _audioMixer;

        [Foldout(GroupConstants.DEBUG)]
        [ReadOnly]
        [SerializeField]
        private AudioMixerGroup _masterGroup;

        private readonly Dictionary<string, AudioAlbumBaseSO> _audioDataDict = new();

        #region PROPERTIES
        public AudioAlbumBaseSO SFXData => TryGetAudioData(AudioNameConstants.SFX, out var sfxData) ? sfxData : null;
        public AudioAlbumBaseSO MFXData => TryGetAudioData(AudioNameConstants.MFX, out var musicData) ? musicData : null;
        public IReadOnlyList<AudioAlbumBaseSO> AudioAlbums => _audioAlbums;
        public AudioMixer AudioMixer => _audioMixer;
        public AudioMixerGroup MasterGroup => _masterGroup;

        public AudioAlbumBaseSO this[string audioDataName]
        {
            get
            {
                if (TryGetAudioData(audioDataName, out var audioData))
                    return audioData;

                Debug.LogWarning($"AudioAlbumManager: Album name '{audioDataName}' not found");
                return null;
            }
        }
        #endregion

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private bool TryGetAudioData(string audioDataName, out AudioAlbumBaseSO audioData)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(audioDataName))
            {
                throw new ArgumentException("❌ Audio album name cannot be null or empty", nameof(audioDataName));
            }

            if (_audioDataDict.TryGetValue(audioDataName, out audioData))
            {
                if (audioData == null)
                    throw new NullReferenceException($"❌ Audio album '{audioDataName}' is null");
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(audioDataName), $"❌ Audio album '{audioDataName}' was not found in {_audioAlbums}");
        }

        private void EnsureInitialized()
        {
            if (_audioDataDict != null && IsAudioDataMapInitialized())
                return;

            _audioDataDict.Clear();
            foreach (var audioData in _audioAlbums)
            {
                if (audioData == null)
                    continue;

                var name = audioData.Name;
                Debug.Log($"AudioAlbumManager: Registering album '{name}' from asset {audioData}");
                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogWarning($"Audio album asset with empty name found: {audioData}");
                    continue;
                }

                if (_audioDataDict.ContainsKey(name))
                {
                    Debug.LogWarning($"Duplicate audio album name detected: {name} in asset {audioData}");
                    continue;
                }

                _audioDataDict[name] = audioData;
            }
        }

        private bool IsAudioDataMapInitialized()
        {
            if (_audioDataDict == null || _audioDataDict.Count == 0)
                return false;

            if (_audioAlbums.Length != _audioDataDict.Count)
                return false;

            foreach (var audioData in _audioAlbums)
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