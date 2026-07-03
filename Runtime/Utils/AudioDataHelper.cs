using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    internal static class AudioDataHelper
    {
        internal static Dictionary<string, AudioCategoryBaseSO> BuildCategoryDictionary(AudioCategoryBaseSO[] audioCategories)
        {
            var dict = new Dictionary<string, AudioCategoryBaseSO>();

            if (audioCategories == null)
                return dict;

            foreach (var category in audioCategories)
            {
                if (category == null)
                    continue;

                if (string.IsNullOrWhiteSpace(category.Name))
                    continue;

                if (!dict.ContainsKey(category.Name))
                    dict.Add(category.Name, category);
                else
                    Debug.LogWarning($"AudioData: Duplicate audio category name '{category.Name}'");
            }

            return dict;
        }

        internal static Dictionary<string, AudioClip> BuildDictionary(AudioContentBaseSO[] audioContents)
        {
            if (audioContents == null)
                throw new NullReferenceException("Audio contents parameter is null: " + nameof(audioContents));

            var dict = new Dictionary<string, AudioClip>();
            foreach (var content in audioContents)
            {
                if (content == null)
                    continue;

                if (string.IsNullOrWhiteSpace(content.Name) || string.IsNullOrEmpty(content.Name))
                    continue;

                if (!dict.ContainsKey(content.Name))
                    dict.Add(content.Name, content.Clip);
                else
                    throw new DuplicateNameException($"AudioData: Duplicate audio content name '{content.Name}'");
            }

            return dict;
        }

        internal static bool TryGetClip(string audioName, AudioCategoryBaseSO category, out AudioClip clip)
        {
            if (category == null)
            {
                throw new NullReferenceException("Category parameter is null: " + nameof(category));
            }
            if (category.AudioClips == null)
            {
                throw new NullArrayException(nameof(category) + $".{nameof(category.AudioClips)} is null");
            }

            if (category.TryGetClip(audioName, out clip))
            {
                return true;
            }

            return false;
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
                    throw new NullReferenceException(audioName);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
