using System.Collections.Generic;
using System.Text;
using BattleTurn.AudioManager.Runtime;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioDataManagerConstGenerator
    {
        private const string GENERATED_FOLDER_PATH = CodeGenerationUtils.GENERATED_FOLDER_PATH + "/Managers";

        public static bool Build(AudioDataManagerSO audioDataManager)
        {
            if (audioDataManager == null)
                return false;

            CodeGenerationUtils.EnsureFolderExists(CodeGenerationUtils.GENERATED_FOLDER_PATH);
            CodeGenerationUtils.EnsureFolderExists(GENERATED_FOLDER_PATH);

            string className = GetRootClassName();
            string filePath = $"{GENERATED_FOLDER_PATH}/{className}.cs";
            string source = GenerateSource(audioDataManager, className);
            bool didGenerateConstants = GenerateFileUtil.GenerateFile(source, filePath);

            return didGenerateConstants;
        }

        internal static string GenerateSource(AudioDataManagerSO audioDataManager, string rootClassName = null)
        {
            rootClassName ??= GetRootClassName();

            var sb = new StringBuilder(2048);
            sb.AppendLine(CodeGenerationUtils.AUTO_GENERATED_MARKER);
            sb.AppendLine(CodeGenerationUtils.DO_NOT_MODIFY_MARKER);
            sb.AppendLine();
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine($"namespace {CodeGenerationUtils.GENERATED_RUNTIME_NAMESPACE}");
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
            string audioDataClassName = GenerateFileUtil.MakeTypeIdentifier(audioData?.Name, "AudioData");
            audioDataClassName = GenerateFileUtil.MakeUniqueName(audioDataClassName, usedAudioDataClassNames);
            GenerateFileUtil.AppendStaticClassStart(sb, audioDataClassName, indentLevel);

            var usedCategoryClassNames = new HashSet<string>(System.StringComparer.Ordinal);
            AppendCategoryNamesClass(sb, audioData, indentLevel + 1);

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

        private static void AppendCategoryNamesClass(StringBuilder sb, AudioDataBaseSO audioData, int indentLevel)
        {
            GenerateFileUtil.AppendStaticClassStart(sb, "Categories", indentLevel);

            var categoryNames = new List<string>();
            if (audioData?.AudioCategories != null)
            {
                foreach (var category in audioData.AudioCategories)
                {
                    if (category == null || string.IsNullOrWhiteSpace(category.Name))
                        continue;

                    categoryNames.Add(category.Name);
                }
            }

            GenerateFileUtil.AppendConstFields(sb, categoryNames, indentLevel + 1, () => "CATEGORY");
            GenerateFileUtil.AppendStaticClassEnd(sb, indentLevel);
        }

        private static IEnumerable<AudioDataBaseSO> EnumerateAudioDatas(AudioDataManagerSO audioDataManager)
        {
            if (audioDataManager?.AudioDatas == null)
                yield break;

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