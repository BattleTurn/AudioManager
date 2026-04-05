using System;
using System.Collections.Generic;
using System.Linq;
using BattleTurn.AudioManager.Runtime;
using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioDataDropdownUtil
    {
        internal static List<string> GetCategoryNames(SerializedProperty property, string audioTypeFieldName)
        {
            var audioData = ResolveAudioData(property, audioTypeFieldName);
            if (audioData?.AudioCategories == null)
                return new List<string>();

            var results = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var category in audioData.AudioCategories)
            {
                if (category == null || string.IsNullOrWhiteSpace(category.Name) || !unique.Add(category.Name))
                    continue;

                results.Add(category.Name);
            }

            return results.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        internal static List<string> GetAudioNames(SerializedProperty property, string audioTypeFieldName, string categoryFieldName)
        {
            var audioData = ResolveAudioData(property, audioTypeFieldName);
            if (audioData == null)
                return new List<string>();

            string categoryName = GetSiblingStringValue(property, categoryFieldName);
            if (string.IsNullOrWhiteSpace(categoryName))
                return new List<string>();

            AudioCategoryBaseSO category;
            try
            {
                category = audioData[categoryName];
            }
            catch (Exception)
            {
                return new List<string>();
            }

            if (category?.AudioClips == null)
                return new List<string>();

            var results = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var clip in category.AudioClips)
            {
                if (clip == null || string.IsNullOrWhiteSpace(clip.Name) || !unique.Add(clip.Name))
                    continue;

                results.Add(clip.Name);
            }

            return results.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        private static AudioDataBaseSO ResolveAudioData(SerializedProperty property, string audioTypeFieldName)
        {
            string audioDataName = GetSiblingEnumName(property, audioTypeFieldName);
            if (string.IsNullOrWhiteSpace(audioDataName))
                return null;

            var manager = LoadAudioDataManager();
            if (manager == null)
                return null;

            try
            {
                return manager[audioDataName];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static AudioDataManagerSO LoadAudioDataManager()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(AudioDataManagerSO)}");
            if (guids == null)
                return null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var manager = AssetDatabase.LoadAssetAtPath<AudioDataManagerSO>(path);
                if (manager != null)
                    return manager;
            }

            return null;
        }

        private static string GetSiblingEnumName(SerializedProperty property, string fieldName)
        {
            var sibling = FindSiblingProperty(property, fieldName);
            if (sibling == null || sibling.propertyType != SerializedPropertyType.Enum)
                return null;

            var names = sibling.enumNames;
            var index = sibling.enumValueIndex;
            if (names == null || index < 0 || index >= names.Length)
                return null;

            return names[index];
        }

        private static string GetSiblingStringValue(SerializedProperty property, string fieldName)
        {
            var sibling = FindSiblingProperty(property, fieldName);
            return sibling != null && sibling.propertyType == SerializedPropertyType.String
                ? sibling.stringValue
                : null;
        }

        private static SerializedProperty FindSiblingProperty(SerializedProperty property, string fieldName)
        {
            if (property == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            var path = property.propertyPath;
            var lastDotIndex = path.LastIndexOf('.');
            var siblingPath = lastDotIndex >= 0
                ? $"{path.Substring(0, lastDotIndex)}.{fieldName}"
                : fieldName;

            return property.serializedObject.FindProperty(siblingPath);
        }
    }
}