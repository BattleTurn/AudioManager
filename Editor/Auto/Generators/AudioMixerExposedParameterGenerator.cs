using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManagement.Editor
{
    internal static class AudioMixerExposedParameterGenerator
    {
        private const string GAME_MIXER_PATH = CodeGenerationUtils.GENERATED_FOLDER_PATH + "/GameMixer.mixer";

        public static bool BuildFromGameMixer()
        {
            CodeGenerationUtils.EnsureFolderExists(CodeGenerationUtils.GENERATED_FOLDER_PATH);

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(GAME_MIXER_PATH);
            if (mixer == null)
            {
                // First-time package add: create the mixer asset from our template if it doesn't exist.
                // NOTE: This is gated so it only happens once automatically per project.
                AudioMixerGenerator.EnsureGameMixerCreatedOnce();

                if (!AssetDatabase.LoadMainAssetAtPath(GAME_MIXER_PATH))
                    return false;

                mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(GAME_MIXER_PATH);
                if (mixer == null)
                {
                    Debug.LogWarning($"{nameof(AudioMixerExposedParameterGenerator)}: GameMixer exists but could not be loaded as AudioMixer.");
                    return false;
                }
            }

            return BuildFromMixer(mixer);
        }

        public static bool BuildFromMixerPath(string mixerUnityPath)
        {
            if (string.IsNullOrWhiteSpace(mixerUnityPath))
                return false;

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerUnityPath);
            if (mixer == null)
            {
                Debug.LogWarning($"{nameof(AudioMixerExposedParameterGenerator)}: Cannot load AudioMixer at '{mixerUnityPath}'.");
                return false;
            }

            return BuildFromMixer(mixer);
        }

        public static bool BuildFromMixer(AudioMixer mixer)
        {
            if (mixer == null)
                return false;

            CodeGenerationUtils.EnsureFolderExists(CodeGenerationUtils.GENERATED_SCRIPT_PATH);

            var names = AudioMixerUtil.GetExposedParams(mixer, debug: false)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            var source = GenerateFileUtil.GenerateStaticClass(names, GetClassName(), () => "PARAM");
            return GenerateFileUtil.GenerateFile(source, GetScriptPath());
        }

        public static bool EnsureGeneratedIfMissing()
        {
            var absolutePath = GetAbsolutePathFromUnityPath(GetScriptPath());
            if (File.Exists(absolutePath))
                return false;

            return BuildFromGameMixer();
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
            if (nameof(AudioMixerExposedParameterGenerator).EndsWith("Generator", StringComparison.Ordinal))
                return nameof(AudioMixerExposedParameterGenerator).Substring(0, nameof(AudioMixerExposedParameterGenerator).Length - "Generator".Length);
            return nameof(AudioMixerExposedParameterGenerator).Replace("Generator", string.Empty);
        }
    }
}
