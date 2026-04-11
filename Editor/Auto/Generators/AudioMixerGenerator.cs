using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Editor
{
    public static class AudioMixerGenerator
    {
        private const string TEMPLATE_MIXER_UNITY_PATH = "Packages/AudioManager/Editor/Template/AudioMixer.mixer";
        private const string GAME_MIXER_UNITY_PATH = CodeGenerationUtils.GENERATED_FOLDER_PATH + "/GameMixer.mixer";
        private const string MIXER_CREATED_ONCE_KEY_PREFIX = nameof(BattleTurn) + "." + nameof(AudioManager) + ".GameMixerCreatedOnce::";

        private const string OUTPUT_PATH = CodeGenerationUtils.GENERATED_FOLDER_PATH + "/GameMixer.mixer";

        public static void EnsureGameMixerCreatedOnce()
        {
            var key = GetMixerCreatedOnceKey();
            var alreadyMarkedCreated = EditorPrefs.GetBool(key, defaultValue: false);
            var existing = AssetDatabase.LoadMainAssetAtPath(GAME_MIXER_UNITY_PATH);

            if (existing != null && IsMixerAssetHealthy(GAME_MIXER_UNITY_PATH, out _))
            {
                if (!alreadyMarkedCreated)
                    EditorPrefs.SetBool(key, true);

                return;
            }

            if (TryCreateGameMixerFromTemplate(forceOverwriteExisting: existing != null || alreadyMarkedCreated, logFailures: true))
                EditorPrefs.SetBool(key, true);
        }

        [MenuItem("Tools/Audio/Create Game Mixer")]
        public static void CreateMixer()
        {
            if (!TryCreateGameMixerFromTemplate(forceOverwriteExisting: true, logFailures: true))
            {
                return;
            }

            var newMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(OUTPUT_PATH);
            if (newMixer == null)
            {
                Debug.LogError("❌ Copied mixer asset exists but could not be loaded as AudioMixer.");
                return;
            }

            Debug.Log($"✅ Created AudioMixer at: {OUTPUT_PATH}");

            AlbumManagerAutoRef.WireAllAudioAlbumManagerAssets(newMixer);

            AudioMixerExposedParameterGenerator.BuildFromMixerPath(OUTPUT_PATH);
            AudioMixerGroupNameGenerator.BuildFromMixerPath(OUTPUT_PATH);

            // 6. Optional: focus asset
            Selection.activeObject = newMixer;
            EditorGUIUtility.PingObject(newMixer);
        }

        [MenuItem("Tools/Audio/Update Mixer Exposed Parameter")]
        public static void UpdateMixerExposedParameter()
        {
            var did = AudioMixerExposedParameterGenerator.BuildFromMixerPath(OUTPUT_PATH);
            Debug.Log(did
                ? "✅ Updated AudioMixerExposedParameter"
                : "ℹ️ AudioMixerExposedParameter is already up to date or mixer missing");
        }

        [MenuItem("Tools/Audio/Update Mixer Group Names")]
        public static void UpdateMixerGroupNames()
        {
            var did = AudioMixerGroupNameGenerator.BuildFromMixerPath(OUTPUT_PATH);
            Debug.Log(did
                ? "✅ Updated AudioMixerGroupName"
                : "ℹ️ AudioMixerGroupName is already up to date or mixer missing");
        }

        [MenuItem("Tools/Audio/Auto Wire AudioAlbumManager AudioMixer")]
        public static void AutoWireAudioAlbumManagerMixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(OUTPUT_PATH);
            var did = AlbumManagerAutoRef.WireAllAudioAlbumManagerAssets(mixer);
            Debug.Log(did
                ? "✅ Wired AudioManager.audioMixer"
                : "ℹ️ No AudioManager assets needed wiring (or GameMixer missing)");
        }

        private static bool TryCreateGameMixerFromTemplate(bool forceOverwriteExisting, bool logFailures)
        {
            // Respect the one-time auto-create rule.
            var key = GetMixerCreatedOnceKey();
            if (!forceOverwriteExisting && EditorPrefs.GetBool(key, defaultValue: false))
                return false;

            CodeGenerationUtils.EnsureFolderExists(CodeGenerationUtils.GENERATED_FOLDER_PATH);

            var templatePath = FindTemplateMixerPath();
            if (string.IsNullOrEmpty(templatePath))
            {
                if (logFailures)
                    Debug.LogError("❌ Cannot find template mixer.");

                return false;
            }

            var existing = AssetDatabase.LoadMainAssetAtPath(GAME_MIXER_UNITY_PATH);
            if (existing != null && !forceOverwriteExisting)
            {
                if (IsMixerAssetHealthy(GAME_MIXER_UNITY_PATH, out _))
                    return true;

                if (logFailures)
                    Debug.LogWarning("AudioMixerGenerator: Existing GameMixer is invalid. Rebuilding it from the template.");
            }

            if (!CopyMixerFilePreservingMeta(templatePath, GAME_MIXER_UNITY_PATH, out var copyError))
            {
                if (logFailures)
                    Debug.LogError(copyError);

                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                GAME_MIXER_UNITY_PATH,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            var newMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(GAME_MIXER_UNITY_PATH);
            if (newMixer == null)
            {
                if (logFailures)
                    Debug.LogError("AudioMixerGenerator: Copied GameMixer could not be loaded as AudioMixer.");

                return false;
            }

            if (!IsMixerAssetHealthy(GAME_MIXER_UNITY_PATH, out var validationError))
            {
                if (logFailures)
                    Debug.LogError($"AudioMixerGenerator: Generated GameMixer is invalid. {validationError}");

                return false;
            }

            AlbumManagerAutoRef.WireAllAudioAlbumManagerAssets(newMixer);
            return true;
        }

        private static string FindTemplateMixerPath()
        {
            var template = AssetDatabase.LoadAssetAtPath<AudioMixer>(TEMPLATE_MIXER_UNITY_PATH);
            if (template != null)
                return TEMPLATE_MIXER_UNITY_PATH;

            string[] guids = AssetDatabase.FindAssets("t:AudioMixer");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Editor/Template/", System.StringComparison.Ordinal) &&
                    path.EndsWith(".mixer", System.StringComparison.Ordinal))
                {
                    Debug.Log("✅ Found template: " + path);
                    return path;
                }
            }

            return null;
        }

        private static string GetMixerCreatedOnceKey()
        {
            return MIXER_CREATED_ONCE_KEY_PREFIX + Application.dataPath;
        }

        private static bool CopyMixerFilePreservingMeta(string sourceUnityPath, string destinationUnityPath, out string error)
        {
            error = null;

            try
            {
                var sourceAbsolutePath = GetAbsolutePathFromUnityPath(sourceUnityPath);
                var destinationAbsolutePath = GetAbsolutePathFromUnityPath(destinationUnityPath);
                var destinationFolder = Path.GetDirectoryName(destinationAbsolutePath);

                if (string.IsNullOrWhiteSpace(destinationFolder))
                {
                    error = $"AudioMixerGenerator: Invalid destination path '{destinationUnityPath}'.";
                    return false;
                }

                Directory.CreateDirectory(destinationFolder);

                if (!File.Exists(sourceAbsolutePath))
                {
                    error = $"AudioMixerGenerator: Template mixer file does not exist at '{sourceUnityPath}'.";
                    return false;
                }

                File.Copy(sourceAbsolutePath, destinationAbsolutePath, overwrite: true);

                if (!TrySetCopiedMixerAssetName(destinationAbsolutePath, Path.GetFileNameWithoutExtension(destinationAbsolutePath), out error))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                error = $"AudioMixerGenerator: Failed to copy template mixer. {ex.Message}";
                return false;
            }
        }

        private static bool IsMixerAssetHealthy(string mixerUnityPath, out string reason)
        {
            reason = null;

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerUnityPath);
            if (mixer == null)
            {
                reason = $"Cannot load AudioMixer at '{mixerUnityPath}'.";
                return false;
            }

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(mixerUnityPath);
            var snapshotCount = allAssets.Count(asset => asset is AudioMixerSnapshot);
            if (snapshotCount == 0)
            {
                reason = "No AudioMixerSnapshot sub-asset was imported.";
                return false;
            }

            var groupCount = allAssets.Count(asset => asset is AudioMixerGroup);
            if (groupCount == 0)
            {
                reason = "No AudioMixerGroup sub-asset was imported.";
                return false;
            }

            var groups = mixer.FindMatchingGroups(string.Empty);
            if (groups == null || groups.Length == 0)
            {
                reason = "The mixer does not expose any mixer groups after import.";
                return false;
            }

            var hasMasterGroup = groups.Any(group => group != null && string.Equals(group.name, "Master", StringComparison.Ordinal));
            if (!hasMasterGroup)
            {
                reason = "The imported mixer is missing the Master group.";
                return false;
            }

            var serializedObject = new SerializedObject(mixer);
            var snapshotsProperty = serializedObject.FindProperty("m_Snapshots");
            if (snapshotsProperty == null || snapshotsProperty.arraySize == 0)
            {
                reason = "The mixer has no serialized snapshots.";
                return false;
            }

            return true;
        }

        private static bool TrySetCopiedMixerAssetName(string mixerAbsolutePath, string targetName, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(mixerAbsolutePath) || string.IsNullOrWhiteSpace(targetName))
            {
                error = "AudioMixerGenerator: Missing mixer path or target asset name.";
                return false;
            }

            try
            {
                var lines = File.ReadAllLines(mixerAbsolutePath);
                var insideAudioMixerController = false;

                for (var i = 0; i < lines.Length; i++)
                {
                    var trimmedLine = lines[i].Trim();
                    if (trimmedLine == "AudioMixerController:")
                    {
                        insideAudioMixerController = true;
                        continue;
                    }

                    if (!insideAudioMixerController)
                        continue;

                    if (trimmedLine.StartsWith("m_Name:", StringComparison.Ordinal))
                    {
                        lines[i] = "  m_Name: " + targetName;
                        File.WriteAllLines(mixerAbsolutePath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                        return true;
                    }

                    if (trimmedLine.StartsWith("--- !u!", StringComparison.Ordinal))
                        break;
                }

                error = "AudioMixerGenerator: Could not find AudioMixerController name field to normalize.";
                return false;
            }
            catch (Exception ex)
            {
                error = $"AudioMixerGenerator: Failed to normalize mixer asset name. {ex.Message}";
                return false;
            }
        }

        private static string GetAbsolutePathFromUnityPath(string unityPath)
        {
            if (unityPath.StartsWith("Assets/", StringComparison.Ordinal))
                return Path.Combine(Application.dataPath, unityPath.Substring("Assets/".Length));

            if (unityPath.StartsWith("Packages/", StringComparison.Ordinal))
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", unityPath));

            throw new ArgumentException($"Unsupported Unity asset path '{unityPath}'.", nameof(unityPath));
        }

        private static void CreateFolderRecursive(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

    }
}