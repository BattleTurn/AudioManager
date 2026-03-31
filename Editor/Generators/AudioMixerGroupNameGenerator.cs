using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioMixerGroupNameGenerator
    {
        private const string GAME_MIXER_PATH = Util.GENERATED_FOLDER_PATH + "/GameMixer.mixer";
        private const string GENERATED_CLASS_NAME = "AudioMixerGroupName";
        private const string GENERATED_FILE_PATH = Util.GENERATED_FOLDER_PATH + "/" + GENERATED_CLASS_NAME + ".cs";


        public static bool EnsureGeneratedIfMissing()
        {
            var absolutePath = GetAbsolutePathFromUnityPath(GENERATED_FILE_PATH);
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

            Util.EnsureFolderExists(Util.GENERATED_FOLDER_PATH);

            var names = (mixer.FindMatchingGroups(string.Empty) ?? Array.Empty<AudioMixerGroup>())
                .Where(g => g != null)
                .Select(g => g.name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            var source = GenerateFileUtil.GenerateSource(names, GENERATED_CLASS_NAME, () => "GROUP");
            return GenerateFileUtil.GenerateFile(source, GENERATED_FILE_PATH);
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
    }
}
