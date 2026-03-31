using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BattleTurn.AudioManager.Editor;
using UnityEngine;
using UnityEditor;

public sealed class GenerateFileUtil
{
    public delegate string OnEmptyName();

    public static string GenerateSource(IReadOnlyList<string> groupNames, string generatedClassName, OnEmptyName onEmptyName)
    {
        var sb = new StringBuilder(1024);
        sb.AppendLine(Util.AUTO_GENERATED_MARKER);
        sb.AppendLine(Util.DO_NOT_MODIFY_MARKER);
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {Util.GENERATED_RUNTIME_NAMESPACE}");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {generatedClassName}");
        sb.AppendLine("    {");

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var groupName in groupNames)
        {
            var constName = MakeIdentifier(groupName, onEmptyName);
            constName = MakeUnique(constName, used);
            used.Add(constName);

            sb.AppendLine($"        public const string {constName} = \"{Escape(groupName)}\";");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        return sb.ToString();
    }

    public static bool GenerateFile(string source, string generatedFilePath)
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

    private static string MakeIdentifier(string raw, OnEmptyName onEmptyName)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "EMPTY";

        // Turn into snake-ish identifier first, then force FULL_UPPER_CASE.
        var cleaned = Regex.Replace(raw.Trim(), "[^a-zA-Z0-9]+", "_");
        cleaned = Regex.Replace(cleaned, "_{2,}", "_").Trim('_');
        if (string.IsNullOrEmpty(cleaned))
            cleaned = onEmptyName.Invoke();
        // cleaned = "PARAM";

        cleaned = cleaned.ToUpperInvariant();

        if (char.IsDigit(cleaned[0]))
            cleaned = "_" + cleaned;

        // Avoid keywords (best-effort)
        if (IsCSharpKeyword(cleaned))
            cleaned = cleaned + "_";

        return cleaned;
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
        // Minimal set; good enough for generated constants.
        return s.ToLowerInvariant() is "class" or "namespace" or "public" or "private" or "protected" or "internal" or "static" or "string" or "const";
    }
}