using System.Collections.Generic;
using UnityEditor;
using BattleTurn.AudioManager.Runtime;
using UnityEngine;
using NaughtyAttributes.Editor;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(AudioAlbumSO))]
    public class AudioAlbumSOEditor : NaughtyInspector
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            var audioData = target as AudioAlbumBaseSO;
            if (audioData == null)
                return;

            DrawValidation(audioData);

            serializedObject.ApplyModifiedProperties();
        }

        internal static void DrawValidation(AudioAlbumBaseSO audioData)
        {
            bool isValid = true;
            var names = new HashSet<string>();

            if (audioData.AudioCategories == null || audioData.AudioCategories.Length == 0)
            {
                EditorGUILayout.HelpBox("Audio contents cannot be empty", MessageType.Error);
                isValid = false;
            }
            else
            {
                foreach (var content in audioData.AudioCategories)
                {
                    if (string.IsNullOrEmpty(content.name))
                    {
                        EditorGUILayout.HelpBox("Audio content name cannot be empty", MessageType.Error);
                        isValid = false;
                        break;
                    }
                    if (CodeGenerationUtils.CSharpKeywords.Contains(content.name))
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

            if (isValid)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("AudioData constants are now generated from AudioDataManagerSO. Save or apply changes on the manager asset to regenerate code.", MessageType.Info);
            }
        }
    }
}
