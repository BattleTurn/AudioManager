using System.Collections.Generic;
using System.Text;
using BattleTurn.AudioManager.Runtime;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioDataManagerConstGenerator
    {
        private const string GENERATED_FOLDER_PATH = Util.GENERATED_FOLDER_PATH + "/Managers";

        public static bool Build(AudioDataManagerSO audioDataManager)
        {
            if (audioDataManager == null)
                return false;

            Util.EnsureFolderExists(GENERATED_FOLDER_PATH);

            string className = GetRootClassName();
            string filePath = $"{GENERATED_FOLDER_PATH}/{className}.cs";
            string source = GenerateSource(audioDataManager, className);
            return GenerateFileUtil.GenerateFile(source, filePath);
        }

        internal static string GenerateSource(AudioDataManagerSO audioDataManager, string rootClassName = null)
        {
            rootClassName ??= GetRootClassName();

            var sb = new StringBuilder(2048);
            sb.AppendLine(Util.AUTO_GENERATED_MARKER);
            sb.AppendLine(Util.DO_NOT_MODIFY_MARKER);
            sb.AppendLine();
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine($"namespace {Util.GENERATED_RUNTIME_NAMESPACE}");
            sb.AppendLine("{");

            GenerateFileUtil.AppendStaticClassStart(sb, rootClassName, 1);

            var usedAudioDataClassNames = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (var audioData in EnumerateAudioDatas(audioDataManager))
            {
                AppendAudioDataClass(sb, audioData, 2, usedAudioDataClassNames);
            }

            GenerateFileUtil.AppendStaticClassEnd(sb, 1);
            sb.AppendLine("}");
            sb.AppendLine();
            return sb.ToString();
        }

        private static void AppendAudioDataClass(StringBuilder sb, AudioDataBaseSO audioData, int indentLevel, HashSet<string> usedAudioDataClassNames)
        {
            string audioDataClassName = GenerateFileUtil.MakeTypeIdentifier(audioData?.Name, "AudioData") + "Categories";
            audioDataClassName = GenerateFileUtil.MakeUniqueName(audioDataClassName, usedAudioDataClassNames);
            GenerateFileUtil.AppendStaticClassStart(sb, audioDataClassName, indentLevel);

            var usedCategoryClassNames = new HashSet<string>(System.StringComparer.Ordinal);

            if (audioData?.AudioCategories != null)
            {
                foreach (var category in audioData.AudioCategories)
                {
                    if (category == null)
                        continue;

                    string categoryClassName = AudioCategoryConstGenerator.GetClassName(category);
                    categoryClassName = GenerateFileUtil.MakeUniqueName(categoryClassName, usedCategoryClassNames);
                    AudioCategoryConstGenerator.AppendNestedClass(sb, category, indentLevel + 1, categoryClassName);
                }
            }

            GenerateFileUtil.AppendStaticClassEnd(sb, indentLevel);
        }

        private static IEnumerable<AudioDataBaseSO> EnumerateAudioDatas(AudioDataManagerSO audioDataManager)
        {
            var seen = new HashSet<AudioDataBaseSO>();
            foreach (var audioData in audioDataManager.AudioDatas)
            {
                if (audioData == null || !seen.Add(audioData))
                    continue;

                yield return audioData;
            }
        }

        private static string GetRootClassName()
        {
            return "AudioNames";
        }
    }
}