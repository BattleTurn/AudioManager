using System.Collections.Generic;
using UnityEditor;
using BattleTurn.AudioManager.Runtime;
using UnityEngine;
using NaughtyAttributes.Editor;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(AudioDataSO))]
    public class AudioDataSOEditor : NaughtyInspector
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            var audioData = target as AudioDataBaseSO;
            if (audioData == null)
                return;

            DrawValidationAndBuild(audioData);

            serializedObject.ApplyModifiedProperties();
        }

        internal static void DrawValidationAndBuild(AudioDataBaseSO audioData)
        {
            bool isValid = true;
            var names = new HashSet<string>();

            if (audioData.AudioContents == null || audioData.AudioContents.Length == 0)
            {
                EditorGUILayout.HelpBox("Audio contents cannot be empty", MessageType.Error);
                isValid = false;
            }
            else
            {
                foreach (var content in audioData.AudioContents)
                {
                    if (string.IsNullOrEmpty(content.name))
                    {
                        EditorGUILayout.HelpBox("Audio content name cannot be empty", MessageType.Error);
                        isValid = false;
                        break;
                    }
                    if (Util.CSharpKeywords.Contains(content.name))
                    {
                        EditorGUILayout.HelpBox($"Audio content name is a C# keyword: {content.name}", MessageType.Error);
                        isValid = false;
                        break;
                    }
                    if (!names.Add(content.name))
                    {
                        EditorGUILayout.HelpBox($"Duplicate audio content name detected: {content.name}", MessageType.Error);
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
                    if (GUILayout.Button("Build", GUILayout.Width(180)))
                    {
                        AudioNameEnumGenerator.BuildFromAudioData(audioData);
                    }
                }
            }
        }
    }
}
