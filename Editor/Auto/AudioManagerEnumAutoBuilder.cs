using System;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    /// <summary>
    /// Auto-generate AudioData enums when an AudioManager asset is created/imported.
    /// Safe: generator is idempotent and will not rewrite unchanged files.
    /// </summary>
    internal sealed class AudioManagerEnumAutoBuilder : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets == null || importedAssets.Length == 0)
                return;

            var anyDid = false;

            foreach (var path in importedAssets)
            {
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset", StringComparison.Ordinal))
                    continue;

                var audioManager = AssetDatabase.LoadAssetAtPath<Runtime.AudioDataManagerSO>(path);
                if (audioManager == null)
                    continue;

                anyDid |= AudioNameEnumGenerator.BuildFromAllAudioDataAssets(audioManager);
            }

            if (anyDid)
                Debug.Log("✅ Auto-built AudioData enums for AudioManager");
        }
    }
}
