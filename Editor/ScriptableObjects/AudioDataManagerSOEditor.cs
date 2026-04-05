using NaughtyAttributes.Editor;
using System;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(Runtime.AudioDataManagerSO))]
    internal sealed class AudioDataManagerSOEditor : NaughtyInspector
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

                if (GUILayout.Button("Build AudioData", GUILayout.Width(180)))
                {
                    var audioManager = (Runtime.AudioDataManagerSO)target;
                    bool didBuild = ApplyAudioDataChanges(audioManager);
                    Debug.Log(didBuild
                        ? "Built AudioData generated code"
                        : "AudioData generated code is already up to date");
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
                    "Apply AudioData changes",
                    "AudioDataManagerSO has unapplied changes. Do you want to apply and generate code before leaving the Inspector?",
                    "Apply",
                    "Keep Editing",
                    "Ignore");

                if (choice == 0)
                {
                    ApplyAudioDataChanges((Runtime.AudioDataManagerSO)target);
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

        private static bool ApplyAudioDataChanges(Runtime.AudioDataManagerSO audioManager)
        {
            if (audioManager == null)
                return false;

            bool didConstants = AudioDataManagerConstGenerator.Build(audioManager);
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
