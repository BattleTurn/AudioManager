using UnityEditor;
using BattleTurn.AudioManager.Runtime;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(AudioData))]
    public class AudioDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _audioContentsProp;

        private void OnEnable()
        {
            _audioContentsProp = serializedObject.FindProperty("audioContents");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_audioContentsProp, includeChildren: true);

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Build AudioName Enum", GUILayout.Width(180)))
                {
                    serializedObject.ApplyModifiedProperties();
                    AudioNameEnumGenerator.BuildFromAllAudioDataAssets(force: true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}