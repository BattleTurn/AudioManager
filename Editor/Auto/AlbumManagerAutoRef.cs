using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using System;
using BattleTurn.AudioManager.Runtime;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AlbumManagerAutoRef
    {
        private const string AUDIO_DATA_MANAGER_PATH = CodeGenerationUtils.GENERATED_SCRIPTABLE_OBJECT_PATH
        + "/Managers";
        private const string AUDIO_ALBUM_PATH = CodeGenerationUtils.GENERATED_SCRIPTABLE_OBJECT_PATH
        + "/Albums";
        private const string DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH = AUDIO_DATA_MANAGER_PATH + "/" + nameof(AudioAlbumManagerSO) + ".asset";
        private const string DEFAULT_SFX_AUDIO_ALBUM_ASSET_PATH = AUDIO_ALBUM_PATH + "/SFXSO.asset";
        private const string DEFAULT_MFX_AUDIO_ALBUM_ASSET_PATH = AUDIO_ALBUM_PATH + "/MFXSO.asset";

        internal static bool WireAllAudioDataManagerAssets(AudioMixer mixer)
        {
            if (mixer == null)
                return false;

            var masterGroup = FindGroup(mixer, "Master");
            var sfxGroup = FindGroup(mixer, "SoundEffect", "SFX");
            var mfxGroup = FindGroup(mixer, "MusicEffect", "MFX");

            var guids = AssetDatabase.FindAssets($"t:{nameof(AudioAlbumManagerSO)}");
            if (guids == null || guids.Length == 0)
            {
                var created = EnsureDefaultAudioDataManagerAssetExists();
                if (created == null)
                    return false;

                guids = AssetDatabase.FindAssets($"t:{nameof(AudioAlbumManagerSO)}");
                if (guids == null || guids.Length == 0)
                    return false;
            }

            var anyChanged = false;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AudioAlbumManagerSO>(path);
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

        private static bool WireAudioDataManager(AudioAlbumManagerSO asset, AudioMixer mixer, AudioMixerGroup masterGroup, AudioMixerGroup sfxGroup, AudioMixerGroup mfxGroup)
        {
            var so = new SerializedObject(asset);
            var changedThis = false;

            // Wire main references
            changedThis |= TrySetObjectReference(so, "_audioMixer", mixer);
            changedThis |= TrySetObjectReference(so, "_masterGroup", masterGroup);

            // Wire audio data entries
            var audioAlbumsProp = so.FindProperty("_audioAlbums");
            if (audioAlbumsProp != null && audioAlbumsProp.isArray)
            {
                changedThis |= EnsureDefaultAudioAlbumsExist(audioAlbumsProp);
                changedThis |= WireAudioDataArray(audioAlbumsProp, sfxGroup, mfxGroup);
            }

            if (changedThis)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            return changedThis;
        }

        private static bool EnsureDefaultAudioAlbumsExist(SerializedProperty audioAlbumsProp)
        {
            var changed = false;

            var sfxAlbum = EnsureDefaultAudioAlbumExists(AudioNameConstants.SFX, DEFAULT_SFX_AUDIO_ALBUM_ASSET_PATH);
            var mfxAlbum = EnsureDefaultAudioAlbumExists(AudioNameConstants.MFX, DEFAULT_MFX_AUDIO_ALBUM_ASSET_PATH);

            changed |= EnsureAudioAlbumInArray(audioAlbumsProp, sfxAlbum);
            changed |= EnsureAudioAlbumInArray(audioAlbumsProp, mfxAlbum);

            return changed;
        }

        private static bool WireAudioDataArray(SerializedProperty audioAlbumsProp, AudioMixerGroup sfxGroup, AudioMixerGroup mfxGroup)
        {
            var anyChanged = false;

            for (int i = 0; i < audioAlbumsProp.arraySize; i++)
            {
                var audioDataRefProp = audioAlbumsProp.GetArrayElementAtIndex(i);
                var audioAlbum = audioDataRefProp.objectReferenceValue as AudioAlbumBaseSO;
                if (audioAlbum == null)
                    continue;

                if (WireAudioDataMixerGroup(audioAlbum, sfxGroup, mfxGroup))
                {
                    anyChanged = true;
                }
            }

            return anyChanged;
        }

        private static bool EnsureAudioAlbumInArray(SerializedProperty audioAlbumsProp, AudioAlbumBaseSO album)
        {
            if (audioAlbumsProp == null || !audioAlbumsProp.isArray || album == null)
                return false;

            for (var index = 0; index < audioAlbumsProp.arraySize; index++)
            {
                var element = audioAlbumsProp.GetArrayElementAtIndex(index);
                if (ReferenceEquals(element.objectReferenceValue, album))
                    return false;
            }

            audioAlbumsProp.arraySize++;
            audioAlbumsProp.GetArrayElementAtIndex(audioAlbumsProp.arraySize - 1).objectReferenceValue = album;
            return true;
        }

        private static AudioAlbumBaseSO EnsureDefaultAudioAlbumExists(string albumName, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioTemplateAlbumSO>(assetPath);
            if (existing != null)
            {
                EnsureAudioAlbumName(existing, albumName);
                return existing;
            }

            EnsureFolderExists(AUDIO_ALBUM_PATH);

            var instance = ScriptableObject.CreateInstance<AudioTemplateAlbumSO>();
            if (instance == null)
            {
                Debug.LogWarning($"AudioManagerMixerAutoRef: Failed to create {albumName} AudioTemplateAlbumSO instance.");
                return null;
            }

            AssetDatabase.CreateAsset(instance, assetPath);
            EnsureAudioAlbumName(instance, albumName);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            return AssetDatabase.LoadAssetAtPath<AudioTemplateAlbumSO>(assetPath);
        }

        private static void EnsureAudioAlbumName(AudioAlbumBaseSO audioAlbum, string albumName)
        {
            if (audioAlbum == null || string.IsNullOrWhiteSpace(albumName))
                return;

            var serializedObject = new SerializedObject(audioAlbum);
            var nameProp = serializedObject.FindProperty("_name");
            if (nameProp == null || string.Equals(nameProp.stringValue, albumName, StringComparison.Ordinal))
                return;

            nameProp.stringValue = albumName;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(audioAlbum);
        }

        private static bool WireAudioDataMixerGroup(AudioAlbumBaseSO audioDataSO, AudioMixerGroup sfxGroup, AudioMixerGroup mfxGroup)
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

        private static AudioAlbumManagerSO EnsureDefaultAudioDataManagerAssetExists()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioAlbumManagerSO>(DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH);
            if (existing != null)
                return existing;

            EnsureFolderExists(AUDIO_DATA_MANAGER_PATH);

            var instance = ScriptableObject.CreateInstance<AudioAlbumManagerSO>();
            if (instance == null)
            {
                Debug.LogWarning("AudioManagerMixerAutoRef: Failed to create AudioDataManager instance.");
                return null;
            }

            AssetDatabase.CreateAsset(instance, DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioAlbumManagerSO>(DEFAULT_AUDIO_DATA_MANAGER_ASSET_PATH);
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
