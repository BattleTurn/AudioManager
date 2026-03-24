using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Editor
{
    public static class AudioMixerGenerator
    {
        private const string TEMPLATE_MIXER_UNITY_PATH = "Packages/AudioManager/Editor/Template/AudioMixer.mixer";
        private const string GENERATED_FOLDER_UNITY_PATH = "Assets/Plugins/BattleTurn/Generated/AudioManager";
        private const string GAME_MIXER_UNITY_PATH = GENERATED_FOLDER_UNITY_PATH + "/GameMixer.mixer";
        private const string MIXER_CREATED_ONCE_KEY_PREFIX = "BattleTurn.AudioManager.GameMixerCreatedOnce::";

        private const string OUTPUT_PATH = GENERATED_FOLDER_UNITY_PATH + "/GameMixer.mixer";

        public static void EnsureGameMixerCreatedOnce()
        {
            var key = GetMixerCreatedOnceKey();
            if (EditorPrefs.GetBool(key, defaultValue: false))
                return;

            // If mixer already exists (e.g., committed to repo), mark as done and don't try to recreate.
            var existing = AssetDatabase.LoadMainAssetAtPath(GAME_MIXER_UNITY_PATH);
            if (existing != null)
            {
                EditorPrefs.SetBool(key, true);
                return;
            }

            if (TryCreateGameMixerFromTemplate())
                EditorPrefs.SetBool(key, true);
        }

        [MenuItem("Tools/Audio/Create Game Mixer")]
        public static void CreateMixer()
        {
            var templatePath = FindTemplateMixerPath();
            if (string.IsNullOrEmpty(templatePath))
            {
                Debug.LogError("❌ Cannot find template mixer.");
                return;
            }

            string folder = System.IO.Path.GetDirectoryName(OUTPUT_PATH);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                CreateFolderRecursive(folder);
            }

            var existing = AssetDatabase.LoadMainAssetAtPath(OUTPUT_PATH);
            if (existing != null)
            {
                if (!AssetDatabase.DeleteAsset(OUTPUT_PATH))
                {
                    Debug.LogError("❌ Cannot delete existing mixer at: " + OUTPUT_PATH);
                    return;
                }

                AssetDatabase.Refresh();
            }

            if (!AssetDatabase.CopyAsset(templatePath, OUTPUT_PATH))
            {
                Debug.LogError($"❌ Cannot copy template mixer from '{templatePath}' to '{OUTPUT_PATH}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OUTPUT_PATH, ImportAssetOptions.ForceUpdate);

            var newMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(OUTPUT_PATH);
            if (newMixer == null)
            {
                Debug.LogError("❌ Copied mixer asset exists but could not be loaded as AudioMixer.");
                return;
            }

            Debug.Log($"✅ Created AudioMixer at: {OUTPUT_PATH}");

            AudioManagerMixerAutoRef.WireAllAudioManagerAssets(newMixer);

            AudioMixerExposedParameterGenerator.BuildFromMixerPath(OUTPUT_PATH, force: true);
            AudioMixerGroupNameGenerator.BuildFromMixerPath(OUTPUT_PATH, force: true);

            // 6. Optional: focus asset
            Selection.activeObject = newMixer;
            EditorGUIUtility.PingObject(newMixer);
        }

        [MenuItem("Tools/Audio/Update Mixer Exposed Parameter")]
        public static void UpdateMixerExposedParameter()
        {
            var did = AudioMixerExposedParameterGenerator.BuildFromMixerPath(OUTPUT_PATH, force: true);
            Debug.Log(did
                ? "✅ Updated AudioMixerExposedParameter"
                : "ℹ️ AudioMixerExposedParameter is already up to date or mixer missing");
        }

        [MenuItem("Tools/Audio/Update Mixer Group Names")]
        public static void UpdateMixerGroupNames()
        {
            var did = AudioMixerGroupNameGenerator.BuildFromMixerPath(OUTPUT_PATH, force: true);
            Debug.Log(did
                ? "✅ Updated AudioMixerGroupName"
                : "ℹ️ AudioMixerGroupName is already up to date or mixer missing");
        }

        [MenuItem("Tools/Audio/Auto Wire AudioManager AudioMixer")]
        public static void AutoWireAudioManagerMixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(OUTPUT_PATH);
            var did = AudioManagerMixerAutoRef.WireAllAudioManagerAssets(mixer);
            Debug.Log(did
                ? "✅ Wired AudioManager.audioMixer"
                : "ℹ️ No AudioManager assets needed wiring (or GameMixer missing)");
        }

        private static bool TryCreateGameMixerFromTemplate()
        {
            // Respect the one-time auto-create rule.
            var key = GetMixerCreatedOnceKey();
            if (EditorPrefs.GetBool(key, defaultValue: false))
                return false;

            Util.EnsureFolderExists(GENERATED_FOLDER_UNITY_PATH);

            var template = AssetDatabase.LoadAssetAtPath<AudioMixer>(TEMPLATE_MIXER_UNITY_PATH);
            if (template == null)
            {
                Debug.LogWarning($"AudioMixerExposedParameterGenerator: Cannot find template mixer at '{TEMPLATE_MIXER_UNITY_PATH}'.");
                return false;
            }

            var existing = AssetDatabase.LoadMainAssetAtPath(GAME_MIXER_UNITY_PATH);
            if (existing != null)
                return true;

            if (!AssetDatabase.CopyAsset(TEMPLATE_MIXER_UNITY_PATH, GAME_MIXER_UNITY_PATH))
            {
                Debug.LogWarning($"AudioMixerGenerator: Failed to copy template mixer from '{TEMPLATE_MIXER_UNITY_PATH}' to '{GAME_MIXER_UNITY_PATH}'.");
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(GAME_MIXER_UNITY_PATH, ImportAssetOptions.ForceUpdate);

            var newMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(GAME_MIXER_UNITY_PATH);
            if (newMixer == null)
            {
                Debug.LogWarning("AudioMixerGenerator: Copied GameMixer could not be loaded as AudioMixer.");
                return false;
            }

            AudioManagerMixerAutoRef.WireAllAudioManagerAssets(newMixer);
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