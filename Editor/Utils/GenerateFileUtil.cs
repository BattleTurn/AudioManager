using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace BattleTurn.AudioManagement.Editor
{
    internal static class GenerateFileUtil
    {
        internal delegate string OnEmptyName();

        internal static string GenerateStaticClass(IReadOnlyList<string> values, string generatedClassName, OnEmptyName onEmptyName, string @namespace = CodeGenerationUtils.GENERATED_RUNTIME_NAMESPACE)
        {
            var className = MakeTypeIdentifier(generatedClassName, "GeneratedConstants");
            var sb = new StringBuilder(1024);
            AppendGeneratedFileHeader(sb, includeNullable: true);
            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine("{");
            AppendStaticClassStart(sb, className, 1);
            AppendConstFields(sb, values, 2, onEmptyName);
            AppendStaticClassEnd(sb, 1);
            sb.AppendLine("}");
            sb.AppendLine();
            return sb.ToString();
        }

        internal static string GenerateEnum(IReadOnlyList<string> members, string enumName, string @namespace = CodeGenerationUtils.GENERATED_RUNTIME_NAMESPACE, bool includeNoneMember = true)
        {
            var sanitizedEnumName = MakeTypeIdentifier(enumName, "GeneratedEnum");
            var sb = new StringBuilder(512);
            AppendGeneratedFileHeader(sb, includeNullable: false);
            sb.AppendLine("#pragma warning disable 1591");
            sb.Append("namespace ").Append(@namespace).AppendLine();
            sb.AppendLine("{");
            sb.Append("    public enum ").Append(sanitizedEnumName).AppendLine();
            sb.AppendLine("    {");

            var value = 0;
            if (includeNoneMember)
            {
                sb.AppendLine("        None = 0,");
                value = 1;
            }

            if (members != null)
            {
                var usedNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var member in members)
                {
                    var sanitizedMember = MakeTypeIdentifier(member, "ITEM");
                    sanitizedMember = MakeUnique(sanitizedMember, usedNames);
                    usedNames.Add(sanitizedMember);
                    sb.Append("        ").Append(sanitizedMember).Append(" = ").Append(value).AppendLine(",");
                    value++;
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        internal static string GenerateConstField(string constantName, string value, int indentLevel = 2)
        {
            return $"{Indent(indentLevel)}public const string {constantName} = \"{Escape(value)}\";";
        }

        internal static bool GenerateFile(string source, string generatedFilePath)
        {
            var absolutePath = GetAbsolutePathFromUnityPath(generatedFilePath);
            if (File.Exists(absolutePath))
            {
                var old = File.ReadAllText(absolutePath);
                if (string.Equals(old, source, StringComparison.Ordinal))
                    return false;
            }

            File.WriteAllText(absolutePath, source, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.ImportAsset(generatedFilePath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        internal static string MakeTypeIdentifier(string raw, string fallback)
        {
            var cleaned = string.IsNullOrWhiteSpace(raw)
                ? fallback
                : Regex.Replace(raw.Trim(), "[^a-zA-Z0-9_]+", "_");

            cleaned = Regex.Replace(cleaned, "_+", "_").Trim('_');
            if (string.IsNullOrEmpty(cleaned))
                cleaned = fallback;

            if (char.IsDigit(cleaned[0]))
                cleaned = "_" + cleaned;

            if (IsCSharpKeyword(cleaned))
                cleaned = "_" + cleaned;

            return cleaned;
        }

        internal static void AppendStaticClassStart(StringBuilder sb, string className, int indentLevel)
        {
            sb.Append(Indent(indentLevel)).Append("public static class ").Append(className).AppendLine();
            sb.Append(Indent(indentLevel)).AppendLine("{");
        }

        internal static void AppendStaticClassEnd(StringBuilder sb, int indentLevel)
        {
            sb.Append(Indent(indentLevel)).AppendLine("}");
        }

        internal static void AppendConstFields(StringBuilder sb, IReadOnlyList<string> values, int indentLevel, OnEmptyName onEmptyName)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            if (values == null)
                return;

            foreach (var value in values)
            {
                var constName = MakeConstIdentifier(value, onEmptyName);
                constName = MakeUnique(constName, used);
                used.Add(constName);

                sb.AppendLine(GenerateConstField(constName, value, indentLevel));
            }
        }

        internal static string MakeUniqueName(string name, HashSet<string> used)
        {
            return MakeUnique(name, used);
        }

        private static void AppendGeneratedFileHeader(StringBuilder sb, bool includeNullable)
        {
            sb.AppendLine(CodeGenerationUtils.AUTO_GENERATED_MARKER);
            sb.AppendLine(CodeGenerationUtils.DO_NOT_MODIFY_MARKER);
            sb.AppendLine();

            if (includeNullable)
            {
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
            }
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

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string MakeConstIdentifier(string raw, OnEmptyName onEmptyName)
        {
            var fallback = onEmptyName?.Invoke() ?? "EMPTY";
            var cleaned = MakeTypeIdentifier(raw, fallback).ToUpperInvariant();
            return IsCSharpKeyword(cleaned) ? cleaned + "_" : cleaned;
        }

        private static string Indent(int level)
        {
            return new string(' ', level * 4);
        }

        private static string MakeUnique(string name, HashSet<string> used)
        {
            if (!used.Contains(name))
                return name;

            var i = 2;
            while (used.Contains(name + "_" + i))
                i++;

            return name + "_" + i;
        }

        private static bool IsCSharpKeyword(string s)
        {
            return CodeGenerationUtils.CSharpKeywords.Contains(s)
                   || CodeGenerationUtils.CSharpKeywords.Contains(s.ToLowerInvariant());
        }
    }
}