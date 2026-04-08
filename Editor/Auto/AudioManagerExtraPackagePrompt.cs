using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    [InitializeOnLoad]
    internal static class AudioManagerExtraPackagePrompt
    {
        private const string KEY_PREFIX = nameof(BattleTurn) + "." + nameof(AudioManager) + ".ExtraPackage::";

        private static string InstalledKey => KEY_PREFIX + "Installed::" + Application.dataPath;
        private static string PromptShownKey => KEY_PREFIX + "PromptShown::" + Application.dataPath;
        private static string LastPackagePathKey => KEY_PREFIX + "LastPackagePath::" + Application.dataPath;
        private static string PendingImportKey => KEY_PREFIX + "PendingImport::" + Application.dataPath;

        static AudioManagerExtraPackagePrompt()
        {
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
        }

        internal static void TryShowPromptOnStartup()
        {
            if (IsExtraInstalled() || EditorPrefs.GetBool(PromptShownKey, false))
                return;

            EditorPrefs.SetBool(PromptShownKey, true);
            EditorApplication.delayCall += AudioManagerExtraPackageWindow.ShowWindow;
        }

        internal static bool IsExtraInstalled()
        {
            return EditorPrefs.GetBool(InstalledKey, false);
        }

        internal static void MarkExtraInstalled()
        {
            EditorPrefs.SetBool(InstalledKey, true);
            EditorPrefs.SetBool(PromptShownKey, true);
            SessionState.SetBool(PendingImportKey, false);
        }

        internal static string GetDefaultPackagePath()
        {
            var savedPath = GetLastPackagePath();
            if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
                return savedPath;

            var autoDetectedPath = FindPackageUnderAudioManager();
            if (!string.IsNullOrWhiteSpace(autoDetectedPath))
            {
                SetLastPackagePath(autoDetectedPath);
                return autoDetectedPath;
            }

            return string.Empty;
        }

        internal static void SetLastPackagePath(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
                return;

            EditorPrefs.SetString(LastPackagePathKey, packagePath);
        }

        internal static bool TryImportExtraPackage(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                EditorUtility.DisplayDialog("AudioManager Extra", "Cannot find the selected .unitypackage file.", "OK");
                return false;
            }

            SetLastPackagePath(packagePath);
            SessionState.SetBool(PendingImportKey, true);
            AssetDatabase.ImportPackage(packagePath, true);
            return true;
        }

        private static string GetLastPackagePath()
        {
            return EditorPrefs.GetString(LastPackagePathKey, string.Empty);
        }

        private static string FindPackageUnderAudioManager()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                return string.Empty;

            var packageRoot = Path.Combine(projectRoot, "Packages", "AudioManager");
            if (!Directory.Exists(packageRoot))
                return string.Empty;

            var unityPackages = Directory.GetFiles(packageRoot, "*.unitypackage", SearchOption.AllDirectories);
            if (unityPackages == null || unityPackages.Length == 0)
                return string.Empty;

            Array.Sort(unityPackages, StringComparer.OrdinalIgnoreCase);

            foreach (var unityPackage in unityPackages)
            {
                var fileName = Path.GetFileName(unityPackage);
                if (fileName.IndexOf("Extra", StringComparison.OrdinalIgnoreCase) >= 0)
                    return unityPackage;
            }

            return unityPackages[0];
        }

        private static void OnImportPackageCompleted(string packageName)
        {
            if (!SessionState.GetBool(PendingImportKey, false))
                return;

            MarkExtraInstalled();
            AudioManagerExtraPackageWindow.CloseIfOpen();
            EditorUtility.DisplayDialog("AudioManager Extra", $"Import completed: {packageName}", "OK");
        }

        private static void OnImportPackageCancelled(string packageName)
        {
            if (!SessionState.GetBool(PendingImportKey, false))
                return;

            SessionState.SetBool(PendingImportKey, false);
            Debug.LogWarning($"AudioManager Extra import was cancelled: {packageName}");
        }

        private static void OnImportPackageFailed(string packageName, string errorMessage)
        {
            if (!SessionState.GetBool(PendingImportKey, false))
                return;

            SessionState.SetBool(PendingImportKey, false);
            Debug.LogError($"AudioManager Extra import failed: {packageName}. {errorMessage}");
        }
    }

    internal sealed class AudioManagerExtraPackageWindow : EditorWindow
    {
        private const float BUTTON_HEIGHT = 28f;

        private string _packagePath;

        [MenuItem("Audio/Import Extra Package")]
        internal static void ShowWindow()
        {
            var window = GetWindow<AudioManagerExtraPackageWindow>(true, "AudioManager Extra", true);
            window.minSize = new Vector2(540f, 220f);
            window.Show();
        }

        internal static void CloseIfOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll<AudioManagerExtraPackageWindow>();
            foreach (var window in windows)
            {
                if (window != null)
                    window.Close();
            }
        }

        private void OnEnable()
        {
            _packagePath = AudioManagerExtraPackagePrompt.GetDefaultPackagePath();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioManager Extra", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This project has not marked AudioManager Extra as installed yet. Import the .unitypackage below, or confirm that you already installed it manually.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Package Path", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(_packagePath ?? string.Empty);
                }

                if (GUILayout.Button("Browse", GUILayout.Width(90f)))
                    BrowsePackage();
            }

            if (string.IsNullOrWhiteSpace(_packagePath))
            {
                EditorGUILayout.HelpBox(
                    "No .unitypackage was auto-detected under Packages/AudioManager. Use Browse to select the Extra package manually.",
                    MessageType.Warning);
            }
            else if (!File.Exists(_packagePath))
            {
                EditorGUILayout.HelpBox(
                    "The saved .unitypackage path no longer exists. Select a valid file before importing.",
                    MessageType.Warning);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Import Extra", GUILayout.Height(BUTTON_HEIGHT)))
                ImportSelectedPackage();

            if (GUILayout.Button("Already Installed", GUILayout.Height(BUTTON_HEIGHT)))
            {
                AudioManagerExtraPackagePrompt.MarkExtraInstalled();
                Close();
            }
        }

        private void ImportSelectedPackage()
        {
            if (string.IsNullOrWhiteSpace(_packagePath) || !File.Exists(_packagePath))
            {
                BrowsePackage();
                if (string.IsNullOrWhiteSpace(_packagePath) || !File.Exists(_packagePath))
                    return;
            }

            if (AudioManagerExtraPackagePrompt.TryImportExtraPackage(_packagePath))
                Close();
        }

        private void BrowsePackage()
        {
            var startingDirectory = GetStartingDirectory();
            var selectedPath = EditorUtility.OpenFilePanel("Select AudioManager Extra", startingDirectory, "unitypackage");

            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            _packagePath = selectedPath;
            AudioManagerExtraPackagePrompt.SetLastPackagePath(selectedPath);
            Repaint();
        }

        private string GetStartingDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_packagePath) && File.Exists(_packagePath))
                return Path.GetDirectoryName(_packagePath) ?? string.Empty;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                return string.Empty;

            var audioManagerFolder = Path.Combine(projectRoot, "Packages", "AudioManager");
            return Directory.Exists(audioManagerFolder) ? audioManagerFolder : projectRoot;
        }
    }
}