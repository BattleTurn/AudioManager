using NaughtyAttributes.Editor;
using System;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManagement.Editor
{
    [CustomEditor(typeof(Runtime.AudioAlbumManagerSO))]
    internal sealed class AudioAlbumManagerSOEditor : NaughtyInspector
    {
        private EditorWindow _lastFocusedWindow;
        private bool _isPromptOpen;

        protected override void OnEnable()
        {
            base.OnEnable();
            _lastFocusedWindow = EditorWindow.focusedWindow;
            EditorApplication.update += HandleFocusChange;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.update -= HandleFocusChange;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button($"Build {nameof(Runtime.AudioAlbumManagerSO)}", GUILayout.Width(180)))
                {
                    var audioManager = (Runtime.AudioAlbumManagerSO)target;
                    bool didBuild = ApplyAudioAlbumChanges(audioManager);
                    Debug.Log(didBuild
                        ? $"Built {nameof(Runtime.AudioAlbumManagerSO)} generated code"
                        : $"{nameof(Runtime.AudioAlbumManagerSO)} generated code is already up to date");
                }
            }
        }

        private void HandleFocusChange()
        {
            if (_isPromptOpen || target == null)
                return;

            EditorWindow currentFocusedWindow = EditorWindow.focusedWindow;
            if (ReferenceEquals(currentFocusedWindow, _lastFocusedWindow))
                return;

            bool inspectorLostFocus = IsInspectorWindow(_lastFocusedWindow) && !IsInspectorWindow(currentFocusedWindow);
            _lastFocusedWindow = currentFocusedWindow;

            if (!inspectorLostFocus)
                return;

            if (!ReferenceEquals(Selection.activeObject, target))
                return;

            if (!EditorUtility.IsDirty(target))
                return;

            _isPromptOpen = true;
            try
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "Apply AudioAlbum changes",
                    $"{nameof(Runtime.AudioAlbumManagerSO)} has unapplied changes. Do you want to apply and generate code before leaving the Inspector?",
                    "Apply",
                    "Keep Editing",
                    "Ignore");

                if (choice == 0)
                {
                    ApplyAudioAlbumChanges((Runtime.AudioAlbumManagerSO)target);
                }
                else if (choice == 1)
                {
                    Selection.activeObject = target;
                    EditorGUIUtility.PingObject(target);
                }
            }
            finally
            {
                _lastFocusedWindow = EditorWindow.focusedWindow;
                _isPromptOpen = false;
            }
        }

        private static bool ApplyAudioAlbumChanges(Runtime.AudioAlbumManagerSO audioManager)
        {
            if (audioManager == null)
                return false;

            bool didConstants = AudioAlbumManagerConstGenerator.Build(audioManager);
            bool didAudioType = AudioTypeGenerator.Build(audioManager);

            EditorUtility.SetDirty(audioManager);
            AssetDatabase.SaveAssetIfDirty(audioManager);
            AssetDatabase.SaveAssets();

            return didConstants || didAudioType;
        }

        private static bool IsInspectorWindow(EditorWindow window)
        {
            return window != null
                   && window.GetType().Name.IndexOf("Inspector", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
