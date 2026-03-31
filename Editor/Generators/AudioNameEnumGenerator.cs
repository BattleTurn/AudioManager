using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BattleTurn.AudioManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    internal static class AudioNameEnumGenerator
    {
        private const string AUDIO_GENERATED_FILE_NAME = "AudioName.cs";
        private const string SFX_GENERATED_FILE_NAME = "SFXNameEnum.cs";
        private const string MFX_GENERATED_FILE_NAME = "MFXNameEnum.cs";
        private const string AUDIO_NAME_ENUM_NAME = "AudioName";
        private const string SFX_ENUM_NAME = "SFXNameEnum";
        private const string MFX_ENUM_NAME = "MFXNameEnum";

        public static bool BuildFromAllAudioDataAssets()
        {
            // If the user has multiple AudioManager assets, we merge all SFX names into SFXNameEnum
            // and all Music names into MFXNameEnum, plus a global AudioName enum (union) for compatibility.
            var guids = AssetDatabase.FindAssets($"t:{nameof(AudioDataManagerSO)}");
            if (guids == null || guids.Length == 0)
                return false;

            var sfxRaw = new List<string>();
            var mfxRaw = new List<string>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var audioManager = AssetDatabase.LoadAssetAtPath<AudioDataManagerSO>(path);
                if (audioManager == null)
                    continue;

                sfxRaw.AddRange(ExtractAudioNames(audioManager.SFXData));
                mfxRaw.AddRange(ExtractAudioNames(audioManager.MFXData));
            }

            var did = false;
            did |= BuildSfxEnumFromNames(sfxRaw);
            did |= BuildMfxEnumFromNames(mfxRaw);
            did |= BuildAudioNameEnumFromNames(sfxRaw.Concat(mfxRaw).ToList());
            return did;
        }

        public static bool BuildFromAllAudioDataAssets(AudioDataManagerSO audioManager)
        {
            if (audioManager == null)
                return false;

            var sfxRaw = ExtractAudioNames(audioManager.SFXData);
            var mfxRaw = ExtractAudioNames(audioManager.MFXData);

            var did = false;
            did |= BuildSfxEnumFromNames(sfxRaw);
            did |= BuildMfxEnumFromNames(mfxRaw);
            did |= BuildAudioNameEnumFromNames(sfxRaw.Concat(mfxRaw).ToList());
            return did;
        }

        private static List<string> ExtractAudioNames(AudioDataBaseSO audioData)
        {
            var rawNames = new List<string>();

            if (audioData == null || audioData.AudioContents == null)
                return rawNames;

            foreach (var content in audioData.AudioContents)
            {
                if (content == null)
                    continue;

                rawNames.Add(content.name);
            }

            return rawNames;
        }

        public static bool BuildFromAudioData(AudioDataBaseSO audioData)
        {
            if (audioData == null)
                return BuildAudioNameEnumFromNames(Array.Empty<string>());

            var rawNames = new List<string>();
            if (audioData.AudioContents != null)
            {
                foreach (var content in audioData.AudioContents)
                {
                    if (content == null)
                        continue;

                    rawNames.Add(content.name);
                }
            }

            return BuildAudioNameEnumFromNames(rawNames);
        }

        public static bool BuildSfxFromAudioData(AudioDataBaseSO audioData)
        {
            return BuildSfxEnumFromNames(ExtractAudioNames(audioData));
        }

        public static bool BuildMfxFromAudioData(AudioDataBaseSO audioData)
        {
            return BuildMfxEnumFromNames(ExtractAudioNames(audioData));
        }

        private static bool BuildSfxEnumFromNames(IReadOnlyList<string> rawNames)
        {
            return BuildFromNames(rawNames, SFX_GENERATED_FILE_NAME, SFX_ENUM_NAME);
        }

        private static bool BuildMfxEnumFromNames(IReadOnlyList<string> rawNames)
        {
            return BuildFromNames(rawNames, MFX_GENERATED_FILE_NAME, MFX_ENUM_NAME);
        }

        private static bool BuildAudioNameEnumFromNames(IReadOnlyList<string> rawNames)
        {
            return BuildFromNames(rawNames, AUDIO_GENERATED_FILE_NAME, AUDIO_NAME_ENUM_NAME);
        }

        private static bool BuildFromNames(IReadOnlyList<string> rawNames, string generatedFileName, string enumName, string @namespace = Util.GENERATED_RUNTIME_NAMESPACE)
        {
            var canonicalUnityPath = GetGeneratedFileAssetPath(generatedFileName);

            // If the user moved the generated enum, remove the moved/duplicate copy and regenerate at the canonical path.
            var didDelete = DeleteRelocatedOrDuplicateGeneratedFiles(canonicalUnityPath, generatedFileName, enumName);

            var enumMembers = MakeEnumMembers(rawNames);
            var newSource = GenerateSource(enumMembers, enumName, @namespace);

            var fileAssetPath = canonicalUnityPath;
            var fileAbsolutePath = GetAbsolutePathFromUnityPath(fileAssetPath);

            if (File.Exists(fileAbsolutePath))
            {
                var oldSource = File.ReadAllText(fileAbsolutePath);
                var unchanged = string.Equals(oldSource, newSource, StringComparison.Ordinal);
                // Never touch the file when unchanged; domain reload loops are often triggered by rewriting the same content.
                if (unchanged && !didDelete)
                    return false;
            }
            else
            {
                // Ensure folder exists for first-time generation.
                Util.EnsureFolderExists(Util.GENERATED_FOLDER_PATH);
            }

            Util.EnsureFolderExists(Util.GENERATED_FOLDER_PATH);
            File.WriteAllText(fileAbsolutePath, newSource, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            AssetDatabase.ImportAsset(fileAssetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static List<string> MakeEnumMembers(IReadOnlyList<string> rawNames)
        {
            // Deterministic output to avoid rewrite/recompile loops.
            // Strategy: sanitize -> group counts -> output sorted keys -> suffix duplicates with _2, _3...
            if (rawNames == null || rawNames.Count == 0)
                return new List<string>();

            var sanitized = rawNames
                .Select(SanitizeToIdentifier)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in sanitized)
            {
                if (counts.TryGetValue(s, out var c))
                    counts[s] = c + 1;
                else
                    counts[s] = 1;
            }

            var result = new List<string>();
            foreach (var key in counts.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                if (string.Equals(key, "None", StringComparison.Ordinal))
                {
                    // Reserve None for the explicit enum member.
                    var n = counts[key];
                    for (var i = 1; i <= n; i++)
                        result.Add(i == 1 ? "None_1" : $"None_{i}");
                    continue;
                }

                var nKey = counts[key];
                if (nKey == 1)
                {
                    result.Add(key);
                }
                else
                {
                    result.Add(key);
                    for (var i = 2; i <= nKey; i++)
                        result.Add($"{key}_{i}");
                }
            }

            return result;
        }

        private static string SanitizeToIdentifier(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var trimmed = raw.Trim();

            var sb = new StringBuilder(trimmed.Length);
            foreach (var ch in trimmed)
            {
                sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
            }

            var id = Regex.Replace(sb.ToString(), "_+", "_");
            id = id.Trim('_');

            if (string.IsNullOrEmpty(id))
                return string.Empty;

            if (char.IsDigit(id[0]))
                id = "_" + id;

            if (Util.CSharpKeywords.Contains(id))
                id = "_" + id;

            return id;
        }

        private static string GenerateSource(IReadOnlyList<string> members, string enumName, string @namespace)
        {
            var sb = new StringBuilder(512);
            sb.AppendLine(Util.AUTO_GENERATED_MARKER);
            sb.AppendLine(Util.DO_NOT_MODIFY_MARKER);
            sb.AppendLine("#pragma warning disable 1591");
            sb.Append("namespace ").Append(@namespace).AppendLine();
            sb.AppendLine("{");
            sb.Append("    public enum ").Append(enumName).AppendLine();
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");

            if (members != null)
            {
                var value = 1;
                foreach (var member in members)
                {
                    sb.Append("        ").Append(member).Append(" = ").Append(value).AppendLine(",");
                    value++;
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static bool DeleteRelocatedOrDuplicateGeneratedFiles(string canonicalUnityPath, string generatedFileName, string enumName)
        {
            var didDelete = false;

            var searchName = Path.GetFileNameWithoutExtension(generatedFileName);

            // Clean up any moved copies under Assets/ (searched via AssetDatabase)
            var guids = AssetDatabase.FindAssets($"{searchName} t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("/" + generatedFileName, StringComparison.Ordinal))
                    continue;

                if (string.Equals(path, canonicalUnityPath, StringComparison.Ordinal))
                    continue;

                // Only delete files that look like our generated output to avoid nuking user scripts.
                if (IsGeneratedEnumFile(path, enumName))
                {
                    didDelete |= AssetDatabase.DeleteAsset(path);
                }
            }

            // Clean up any legacy location in Packages/ that we previously created.
            var legacyPackagePath = "Packages/AudioManager/Runtime/AudioName.cs";
            if (string.Equals(enumName, AUDIO_NAME_ENUM_NAME, StringComparison.Ordinal) &&
                !string.Equals(legacyPackagePath, canonicalUnityPath, StringComparison.Ordinal) &&
                File.Exists(GetAbsolutePathFromUnityPath(legacyPackagePath)) &&
                IsGeneratedEnumFile(legacyPackagePath, AUDIO_NAME_ENUM_NAME))
            {
                didDelete |= AssetDatabase.DeleteAsset(legacyPackagePath);
            }

            return didDelete;
        }

        private static bool IsGeneratedEnumFile(string unityPath, string enumName)
        {
            try
            {
                var absolutePath = GetAbsolutePathFromUnityPath(unityPath);
                if (!File.Exists(absolutePath))
                    return false;

                var text = File.ReadAllText(absolutePath);
                return text.Contains(Util.AUTO_GENERATED_MARKER, StringComparison.Ordinal) &&
                       text.Contains(Util.DO_NOT_MODIFY_MARKER, StringComparison.Ordinal) &&
                       text.Contains("enum " + enumName, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static string GetGeneratedFileAssetPath(string generatedFileName)
        {
            return $"{Util.GENERATED_FOLDER_PATH}/{generatedFileName}";
        }

        private static void EnsureFolderExists(string folderUnityPath)
        {
            var folderAbsolutePath = GetAbsolutePathFromUnityPath(folderUnityPath);
            Directory.CreateDirectory(folderAbsolutePath);
        }

        private static string GetAbsolutePathFromUnityPath(string unityPath)
        {
            if (string.IsNullOrEmpty(unityPath))
                throw new ArgumentException("unityPath is null or empty", nameof(unityPath));

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (unityPath == "Assets")
                return Application.dataPath;

            if (unityPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                var relative = unityPath.Substring("Assets/".Length);
                return Path.Combine(Application.dataPath, relative);
            }

            if (unityPath.StartsWith("Packages/", StringComparison.Ordinal))
                return Path.Combine(projectRoot, unityPath);

            throw new ArgumentException($"Expected a Unity path starting with 'Assets/' or 'Packages/' but got '{unityPath}'", nameof(unityPath));
        }
    }
}
