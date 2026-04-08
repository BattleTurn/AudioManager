using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using System;
using BattleTurn.AudioManager.Runtime;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioManagerMixerAutoRef
    {
        private const string AUDIO_DATA_MANAGER_PATH = CodeGenerationUtils.GENERATED_SCRIPTABLE_OBJECT_PATH
        + "/Managers";
        private const string DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH = AUDIO_DATA_MANAGER_PATH + "/" + nameof(AudioDataManagerSO) + ".asset";

        internal static bool WireAllAudioDataManagerAssets(AudioMixer mixer)
        {
            if (mixer == null)
                return false;

            var masterGroup = FindGroup(mixer, "Master");
            var sfxGroup = FindGroup(mixer, "SoundEffect", "SFX");
            var mfxGroup = FindGroup(mixer, "MusicEffect", "MFX");

            var guids = AssetDatabase.FindAssets($"t:{nameof(AudioDataManagerSO)}");
            if (guids == null || guids.Length == 0)
            {
                var created = EnsureDefaultAudioDataManagerAssetExists();
                if (created == null)
                    return false;

                guids = AssetDatabase.FindAssets($"t:{nameof(AudioDataManagerSO)}");
                if (guids == null || guids.Length == 0)
                    return false;
            }

            var anyChanged = false;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AudioDataManagerSO>(path);
                if (asset == null)
                    continue;

                if (WireAudioDataManager(asset, mixer, masterGroup, sfxGroup, mfxGroup))
                {
                    anyChanged = true;
                }
            }

            if (anyChanged)
                AssetDatabase.SaveAssets();

            return anyChanged;
        }

        private static bool WireAudioDataManager(AudioDataManagerSO asset, AudioMixer mixer, AudioMixerGroup masterGroup, AudioMixerGroup sfxGroup, AudioMixerGroup mfxGroup)
        {
            var so = new SerializedObject(asset);
            var changedThis = false;

            // Wire main references
            changedThis |= TrySetObjectReference(so, "_audioMixer", mixer);
            changedThis |= TrySetObjectReference(so, "_masterGroup", masterGroup);

            // Wire audio data entries
            var audioDatasProp = so.FindProperty("_audioDatas");
            if (audioDatasProp != null && audioDatasProp.isArray)
            {
                changedThis |= WireAudioDataArray(audioDatasProp, sfxGroup, mfxGroup);
            }

            if (changedThis)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            return changedThis;
        }

        private static bool WireAudioDataArray(SerializedProperty audioDatasProp, AudioMixerGroup sfxGroup, AudioMixerGroup mfxGroup)
        {
            var anyChanged = false;

            for (int i = 0; i < audioDatasProp.arraySize; i++)
            {
                var audioDataRefProp = audioDatasProp.GetArrayElementAtIndex(i);
                var audioDataSO = audioDataRefProp.objectReferenceValue as AudioAlbumSO;
                if (audioDataSO == null)
                    continue;

                if (WireAudioDataMixerGroup(audioDataSO, sfxGroup, mfxGroup))
                {
                    anyChanged = true;
                }
            }

            return anyChanged;
        }

        private static bool WireAudioDataMixerGroup(AudioAlbumSO audioDataSO, AudioMixerGroup sfxGroup, AudioMixerGroup mfxGroup)
        {
            var audioDataSerializedObj = new SerializedObject(audioDataSO);
            var nameProp = audioDataSerializedObj.FindProperty("_name");

            if (nameProp == null || string.IsNullOrEmpty(nameProp.stringValue))
                return false;

            bool changed = false;

            if (string.Equals(nameProp.stringValue, AudioNameConstants.SFX, StringComparison.Ordinal))
            {
                changed |= TrySetObjectReference(audioDataSerializedObj, "_mixerGroup", sfxGroup);
            }
            else if (string.Equals(nameProp.stringValue, AudioNameConstants.MFX, StringComparison.Ordinal))
            {
                changed |= TrySetObjectReference(audioDataSerializedObj, "_mixerGroup", mfxGroup);
            }

            if (changed)
            {
                audioDataSerializedObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(audioDataSO);
            }

            return changed;
        }

        private static bool TrySetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            if (value == null)
                return false;

            var prop = serializedObject.FindProperty(propertyName);
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = value;
                return true;
            }

            return false;
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, params string[] preferredNames)
        {
            if (mixer == null)
                return null;

            var groups = mixer.FindMatchingGroups(string.Empty);
            if (groups == null || groups.Length == 0)
                return null;

            if (preferredNames != null)
            {
                foreach (var name in preferredNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    for (var i = 0; i < groups.Length; i++)
                    {
                        var g = groups[i];
                        if (g != null && string.Equals(g.name, name, StringComparison.Ordinal))
                            return g;
                    }
                }

                foreach (var name in preferredNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    for (var i = 0; i < groups.Length; i++)
                    {
                        var g = groups[i];
                        if (g == null || string.IsNullOrWhiteSpace(g.name))
                            continue;

                        if (g.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                            return g;
                    }
                }
            }

            return null;
        }

        private static AudioDataManagerSO EnsureDefaultAudioDataManagerAssetExists()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioDataManagerSO>(DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH);
            if (existing != null)
                return existing;

            EnsureFolderExists(AUDIO_DATA_MANAGER_PATH);

            var instance = ScriptableObject.CreateInstance<AudioDataManagerSO>();
            if (instance == null)
            {
                Debug.LogWarning("AudioManagerMixerAutoRef: Failed to create AudioDataManager instance.");
                return null;
            }

            AssetDatabase.CreateAsset(instance, DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioDataManagerSO>(DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH);
        }

        private static void EnsureFolderExists(string unityFolderPath)
        {
            if (AssetDatabase.IsValidFolder(unityFolderPath))
                return;

            var parts = unityFolderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
