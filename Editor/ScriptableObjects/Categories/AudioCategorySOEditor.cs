using System.Collections.Generic;
using BattleTurn.AudioManagement.Runtime;
using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManagement.Editor
{
    [CustomEditor(typeof(AudioCategorySO))]
    internal sealed class AudioCategorySOEditor : NaughtyInspector
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            var category = target as AudioCategorySO;
            if (category == null)
                return;

            DrawValidationAndBuild(category);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawValidationAndBuild(AudioCategorySO category)
        {
            bool isValid = true;
            var names = new HashSet<string>();

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                EditorGUILayout.HelpBox("Category name cannot be empty", MessageType.Error);
                isValid = false;
            }

            if (category.AudioClips == null || category.AudioClips.Length == 0)
            {
                EditorGUILayout.HelpBox("Audio contents cannot be empty", MessageType.Error);
                isValid = false;
            }
            else
            {
                foreach (var audioContent in category.AudioClips)
                {
                    if (audioContent == null)
                    {
                        EditorGUILayout.HelpBox("Audio content entry cannot be null", MessageType.Error);
                        isValid = false;
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(audioContent.Name))
                    {
                        EditorGUILayout.HelpBox("Audio content name cannot be empty", MessageType.Error);
                        isValid = false;
                        break;
                    }

                    if (!names.Add(audioContent.Name))
                    {
                        EditorGUILayout.HelpBox($"Duplicate audio content name detected: {audioContent.Name}", MessageType.Error);
                        isValid = false;
                        break;
                    }
                }
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledGroupScope(!isValid))
                {
                    if (GUILayout.Button("Build Constants", GUILayout.Width(180)))
                    {
                        bool did = AudioCategoryConstGenerator.Build(category);
                        Debug.Log(did
                            ? $"Built audio constants for category '{category.Name}'"
                            : $"Audio constants for category '{category.Name}' are already up to date");
                    }
                }
            }
        }
    }
}