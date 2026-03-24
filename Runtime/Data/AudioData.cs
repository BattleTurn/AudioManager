using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Runtime
{
    [Serializable]
    public sealed class AudioData
    {
        [SerializeField]
        private AudioContent[] audioContents;
        [SerializeField]
        private AudioMixerGroup mixerGroup;

        private Dictionary<string, AudioClip> audioContentDict;

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

        public AudioContent[] AudioContents => audioContents;
        public AudioMixerGroup MixerGroup => mixerGroup;

        public bool TryGetClip(string audioName, out AudioClip clip)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(audioName))
            {
                clip = null;
                return false;
            }

            if (audioContentDict.TryGetValue(audioName, out clip))
            {
                if (clip == null)
                    throw new EmptyAudioClipException(audioName);
                return true;
            }

            throw new EmptyAudioClipException(audioName);
        }

        private void EnsureInitialized()
        {
            if (audioContentDict != null)
                return;

            audioContentDict = new Dictionary<string, AudioClip>();

            if (audioContents == null)
                return;

            foreach (var content in audioContents)
            {
                if (content == null)
                    continue;

                if (string.IsNullOrWhiteSpace(content.name))
                    continue;

                if (!audioContentDict.ContainsKey(content.name))
                    audioContentDict.Add(content.name, content.clip);
                else
                    Debug.LogWarning($"AudioData: Duplicate audio content name '{content.name}'");
            }
        }
    }
}
