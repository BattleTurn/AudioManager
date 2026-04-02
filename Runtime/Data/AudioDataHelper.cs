using System.Collections.Generic;
using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    internal static class AudioDataHelper
    {
        internal static Dictionary<string, AudioClip> BuildDictionary(AudioContent[] audioContents)
        {
            var dict = new Dictionary<string, AudioClip>();

            if (audioContents == null)
                return dict;

            foreach (var content in audioContents)
            {
                if (content == null)
                    continue;

                if (string.IsNullOrWhiteSpace(content.Name))
                    continue;

                if (!dict.ContainsKey(content.Name))
                    dict.Add(content.Name, content.Clip);
                else
                    Debug.LogWarning($"AudioData: Duplicate audio content name '{content.name}'");
            }

            return dict;
        }

        internal static bool TryGetClip(string audioName, Dictionary<string, AudioClip> dict, out AudioClip clip)
        {
            if (string.IsNullOrEmpty(audioName))
            {
                clip = null;
                return false;
            }

            if (dict.TryGetValue(audioName, out clip))
            {
                if (clip == null)
                    throw new EmptyAudioClipException(audioName);
                return true;
            }

            throw new EmptyAudioClipException(audioName);
        }
    }
}
