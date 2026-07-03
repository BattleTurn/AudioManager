using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManagement.Editor
{
    [InitializeOnLoad]
    internal static class AudioManagerExtraPackagePrompt
    {
        private const string KEY_PREFIX = nameof(BattleTurn) + "." + nameof(AudioManagement) + ".ExtraPackage.V2::";
        private const string EXTRA_PACKAGE_UNITY_PATH = "Packages/AudioManager/Editor/Extras/AudioManager(Extra).unitypackage";
        private const string EXTRA_PACKAGE_RELATIVE_PATH = "Editor/Extras/AudioManager(Extra).unitypackage";

        private static string InstalledKey => KEY_PREFIX + "Installed::" + Application.dataPath;
        private static string PromptShownKey => KEY_PREFIX + "PromptShown::" + Application.dataPath;
        private static string PendingImportKey => KEY_PREFIX + "PendingImport::" + Application.dataPath;
        private static string PromptQueuedKey => KEY_PREFIX + "PromptQueued::" + Application.dataPath;

        static AudioManagerExtraPackagePrompt()
        {
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            EditorApplication.delayCall += TryShowPromptOnStartup;
        }

        internal static void TryShowPromptOnStartup()
        {
            if (IsExtraInstalled() || EditorPrefs.GetBool(PromptShownKey, false))
                return;

            if (SessionState.GetBool(PromptQueuedKey, false))
                return;

            SessionState.SetBool(PromptQueuedKey, true);
            EditorApplication.delayCall += ShowPromptWhenEditorReady;
        }

        internal static void MarkPromptShown()
        {
            EditorPrefs.SetBool(PromptShownKey, true);
            SessionState.SetBool(PromptQueuedKey, false);
        }

        internal static bool IsExtraInstalled()
        {
            return EditorPrefs.GetBool(InstalledKey, false);
        }

        internal static void MarkExtraInstalled()
        {
            EditorPrefs.SetBool(InstalledKey, true);
            EditorPrefs.SetBool(PromptShownKey, true);
            SessionState.SetBool(PromptQueuedKey, false);
            SessionState.SetBool(PendingImportKey, false);
        }

        private static void ShowPromptWhenEditorReady()
        {
            if (IsExtraInstalled() || EditorPrefs.GetBool(PromptShownKey, false))
            {
                SessionState.SetBool(PromptQueuedKey, false);
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ShowPromptWhenEditorReady;
                return;
            }

            SessionState.SetBool(PromptQueuedKey, false);
            AudioManagerExtraPackageWindow.ShowWindow();
        }

        internal static bool TryImportExtraPackage()
        {
            var packagePath = GetExtraPackageAbsolutePath();
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                EditorUtility.DisplayDialog(
                    "AudioManager Extra",
                    $"Cannot find Extra package. Expected relative path: '{EXTRA_PACKAGE_RELATIVE_PATH}'.",
                    "OK");
                return false;
            }

            SessionState.SetBool(PendingImportKey, true);
            AssetDatabase.ImportPackage(packagePath, true);
            return true;
        }

        internal static bool HasImportPackage()
        {
            var packagePath = GetExtraPackageAbsolutePath();
            return !string.IsNullOrWhiteSpace(packagePath) && File.Exists(packagePath);
        }

        private static string GetExtraPackageAbsolutePath()
        {
            try
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AudioManagerExtraPackagePrompt).Assembly);
                if (packageInfo != null && !string.IsNullOrWhiteSpace(packageInfo.resolvedPath))
                {
                    var candidate = Path.GetFullPath(Path.Combine(packageInfo.resolvedPath, EXTRA_PACKAGE_RELATIVE_PATH));
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"AudioManager Extra: Failed to resolve package path from assembly. {exception.Message}");
            }

            var fallbackPath = GetAbsolutePathFromUnityPath(EXTRA_PACKAGE_UNITY_PATH);
            return File.Exists(fallbackPath) ? fallbackPath : string.Empty;
        }

        private static string GetAbsolutePathFromUnityPath(string unityPath)
        {
            if (string.IsNullOrWhiteSpace(unityPath))
                return string.Empty;

            if (unityPath.StartsWith("Packages/", StringComparison.Ordinal))
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", unityPath));

            if (unityPath.StartsWith("Assets/", StringComparison.Ordinal))
                return Path.GetFullPath(Path.Combine(Application.dataPath, unityPath.Substring("Assets/".Length)));

            return unityPath;
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

        [MenuItem("Tools/Audio/📥(Import) Extra 📦(Package)")]
        internal static void ShowWindow()
        {
            AudioManagerExtraPackagePrompt.MarkPromptShown();
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
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioManager Extra", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This project has not marked AudioManager Extra as installed yet. Import the bundled Extra package from the current installed package location, or confirm that you already installed it manually.",
                MessageType.Info);

            if (!AudioManagerExtraPackagePrompt.HasImportPackage())
            {
                EditorGUILayout.HelpBox(
                    "The bundled Extra package could not be found under Editor/Extras/AudioManager(Extra).unitypackage in the installed package path.",
                    MessageType.Warning);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Import Extra", GUILayout.Height(BUTTON_HEIGHT)))
                ImportExtraPackage();

            if (GUILayout.Button("Already Installed", GUILayout.Height(BUTTON_HEIGHT)))
            {
                AudioManagerExtraPackagePrompt.MarkExtraInstalled();
                Close();
            }
        }

        private void ImportExtraPackage()
        {
            if (AudioManagerExtraPackagePrompt.TryImportExtraPackage())
                Close();
        }
    }
}