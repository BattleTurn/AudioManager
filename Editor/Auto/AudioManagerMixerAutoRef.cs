using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using System;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioManagerMixerAutoRef
    {
        private const string GeneratedFolderUnityPath = "Assets/Plugins/BattleTurn/Generated/AudioManager";
        private const string DefaultAudioManagerAssetPath = GeneratedFolderUnityPath + "/AudioManager.asset";

        internal static bool WireAllAudioManagerAssets(AudioMixer mixer)
        {
            if (mixer == null)
                return false;

            var masterGroup = FindGroup(mixer, "Master");
            var sfxGroup = FindGroup(mixer, "SoundEffect", "SFX");
            var mfxGroup = FindGroup(mixer, "MusicEffect", "MFX");

            var guids = AssetDatabase.FindAssets("t:AudioManager");
            if (guids == null || guids.Length == 0)
            {
                var created = EnsureDefaultAudioManagerAssetExists();
                if (created == null)
                    return false;

                // Re-query so we wire using the same flow.
                guids = AssetDatabase.FindAssets("t:AudioManager");
                if (guids == null || guids.Length == 0)
                    return false;
            }

            var anyChanged = false;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Runtime.AudioManager>(path);
                if (asset == null)
                    continue;

                var so = new SerializedObject(asset);

                var changedThis = false;

                var mixerProp = so.FindProperty("_audioMixer");
                if (mixerProp != null && mixerProp.objectReferenceValue == null)
                {
                    mixerProp.objectReferenceValue = mixer;
                    changedThis = true;
                }

                var masterGroupProp = so.FindProperty("_masterGroup");
                if (masterGroupProp != null && masterGroupProp.objectReferenceValue == null && masterGroup != null)
                {
                    masterGroupProp.objectReferenceValue = masterGroup;
                    changedThis = true;
                }

                // Nested AudioData (serializable class) lives inside AudioManager asset.
                var sfxDataProp = so.FindProperty("_sfxData");
                if (sfxDataProp != null && sfxGroup != null)
                {
                    var sfxMixerGroupProp = sfxDataProp.FindPropertyRelative("mixerGroup");
                    if (sfxMixerGroupProp != null && sfxMixerGroupProp.objectReferenceValue == null)
                    {
                        sfxMixerGroupProp.objectReferenceValue = sfxGroup;
                        changedThis = true;
                    }
                }

                var musicDataProp = so.FindProperty("_musicData");
                if (musicDataProp != null && mfxGroup != null)
                {
                    var mfxMixerGroupProp = musicDataProp.FindPropertyRelative("mixerGroup");
                    if (mfxMixerGroupProp != null && mfxMixerGroupProp.objectReferenceValue == null)
                    {
                        mfxMixerGroupProp.objectReferenceValue = mfxGroup;
                        changedThis = true;
                    }
                }

                if (changedThis)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                    anyChanged = true;
                }
            }

            if (anyChanged)
                AssetDatabase.SaveAssets();

            return anyChanged;
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

        private static Runtime.AudioManager EnsureDefaultAudioManagerAssetExists()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Runtime.AudioManager>(DefaultAudioManagerAssetPath);
            if (existing != null)
                return existing;

            EnsureFolderExists(GeneratedFolderUnityPath);

            var instance = ScriptableObject.CreateInstance<Runtime.AudioManager>();
            if (instance == null)
            {
                Debug.LogWarning("AudioManagerMixerAutoRef: Failed to create AudioManager instance.");
                return null;
            }

            AssetDatabase.CreateAsset(instance, DefaultAudioManagerAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(DefaultAudioManagerAssetPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Runtime.AudioManager>(DefaultAudioManagerAssetPath);
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
