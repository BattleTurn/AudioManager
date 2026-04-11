using System.Linq;
using System.Collections.Generic;
using BattleTurn.AudioManager.Runtime;
using System.Text;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioCategoryConstGenerator
    {
        public static bool Build(AudioCategorySO category)
        {
            if (category == null)
                return false;

            CodeGenerationUtils.EnsureFolderExists(CodeGenerationUtils.GENERATED_SCRIPT_PATH);

            var className = GetClassName(category);
            var filePath = CodeGenerationUtils.GENERATED_SCRIPT_PATH + "/" + className + ".cs";
            var source = GenerateSource(category);
            return GenerateFileUtil.GenerateFile(source, filePath);
        }

        internal static string GenerateSource(AudioCategoryBaseSO category)
        {
            var className = GetClassName(category);
            var source = GenerateFileUtil.GenerateStaticClass(ExtractAudioNames(category), className, () => "AUDIO");
            return source;
        }

        internal static void AppendNestedClass(StringBuilder sb, AudioCategoryBaseSO category, int indentLevel, string className = null)
        {
            if (category == null)
                return;

            className ??= GetClassName(category);
            GenerateFileUtil.AppendStaticClassStart(sb, className, indentLevel);
            GenerateFileUtil.AppendConstFields(sb, ExtractAudioNames(category), indentLevel + 1, () => "AUDIO");
            GenerateFileUtil.AppendStaticClassEnd(sb, indentLevel);
        }

        internal static string GetClassName(AudioCategoryBaseSO category)
        {
            return GenerateFileUtil.MakeTypeIdentifier(category?.Name, "AudioCategory");
        }

        private static List<string> ExtractAudioNames(AudioCategoryBaseSO category)
        {
            if (category?.AudioClips == null)
                return new List<string>();

            return category.AudioClips
                .Where(content => content != null && !string.IsNullOrWhiteSpace(content.Name))
                .Select(content => content.Name)
                .ToList();
        }
    }
}