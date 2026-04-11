using System;
using System.Collections.Generic;
using BattleTurn.AudioManager.Runtime;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioTypeGenerator
    {
        public static bool Build(AudioAlbumManagerSO audioDataManager)
        {
            if (audioDataManager == null)
                return false;

            CodeGenerationUtils.EnsureFolderExists(CodeGenerationUtils.GENERATED_SCRIPT_PATH);

            var source = GenerateAudioTypeSource(audioDataManager);
            return GenerateFileUtil.GenerateFile(source, CodeGenerationUtils.GENERATED_SCRIPT_PATH + "/" + GetClassName() + ".cs");
        }

        private static string GenerateAudioTypeSource(AudioAlbumManagerSO audioDataManager)
        {
            var members = new List<string>();

            foreach (var audioData in EnumerateAudioDatas(audioDataManager))
            {
                if (string.IsNullOrWhiteSpace(audioData?.Name))
                    continue;

                members.Add(audioData.Name);
            }

            return GenerateFileUtil.GenerateEnum(members, GetClassName(), includeNoneMember: false);
        }

        private static IEnumerable<AudioAlbumBaseSO> EnumerateAudioDatas(AudioAlbumManagerSO audioDataManager)
        {
            if (audioDataManager?.AudioAlbums == null)
                yield break;

            var seen = new HashSet<AudioAlbumBaseSO>();
            foreach (var audioData in audioDataManager.AudioAlbums)
            {
                if (audioData == null || !seen.Add(audioData))
                    continue;

                yield return audioData;
            }
        }

        private static string GetClassName()
        {
            if (nameof(AudioTypeGenerator).EndsWith("Generator", StringComparison.Ordinal))
                return nameof(AudioTypeGenerator).Substring(0, nameof(AudioTypeGenerator).Length - "Generator".Length);
            return nameof(AudioTypeGenerator).Replace("Generator", string.Empty);
        }
    }
}