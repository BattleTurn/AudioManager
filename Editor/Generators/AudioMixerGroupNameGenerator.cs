using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioMixerGroupNameGenerator
    {
        private const string GAME_MIXER_PATH = CodeGenerationUtils.GENERATED_FOLDER_PATH + "/GameMixer.mixer";

        public static bool EnsureGeneratedIfMissing()
        {
            var absolutePath = GetAbsolutePathFromUnityPath(GetScriptPath());
            if (File.Exists(absolutePath))
                return false;

            return BuildFromGameMixer();
        }

        public static bool BuildFromGameMixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(GAME_MIXER_PATH);
            if (mixer == null)
                return false;

            return BuildFromMixer(mixer);
        }

        public static bool BuildFromMixerPath(string mixerUnityPath)
        {
            if (string.IsNullOrWhiteSpace(mixerUnityPath))
                return false;

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerUnityPath);
            if (mixer == null)
                return false;

            return BuildFromMixer(mixer);
        }

        public static bool BuildFromMixer(AudioMixer mixer)
        {
            if (mixer == null)
                return false;

            CodeGenerationUtils.EnsureFolderExists(CodeGenerationUtils.GENERATED_FOLDER_PATH);

            var names = (mixer.FindMatchingGroups(string.Empty) ?? Array.Empty<AudioMixerGroup>())
                .Where(g => g != null)
                .Select(g => g.name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            var source = GenerateFileUtil.GenerateStaticClass(names, GetClassName(), () => "GROUP");
            return GenerateFileUtil.GenerateFile(source, GetScriptPath());
        }

        private static string GetAbsolutePathFromUnityPath(string unityPath)
        {
            if (unityPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                var rel = unityPath.Substring("Assets/".Length);
                return Path.Combine(Application.dataPath, rel);
            }

            throw new ArgumentException($"Expected Assets-relative path but got '{unityPath}'", nameof(unityPath));
        }

        private static string GetScriptPath()
        {
            return CodeGenerationUtils.GENERATED_SCRIPT_PATH + "/" + GetClassName() + ".cs";
        }

        private static string GetClassName()
        {
            if (nameof(AudioMixerGroupNameGenerator).EndsWith("Generator", StringComparison.Ordinal))
                return nameof(AudioMixerGroupNameGenerator).Substring(0, nameof(AudioMixerGroupNameGenerator).Length - "Generator".Length);
            return nameof(AudioMixerGroupNameGenerator).Replace("Generator", string.Empty);
        }
    }
}
