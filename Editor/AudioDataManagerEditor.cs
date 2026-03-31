using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(Runtime.AudioDataManagerSO))]
    internal sealed class AudioDataManagerEditor : NaughtyInspector
    {
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
                    var did = AudioNameEnumGenerator.BuildFromAllAudioDataAssets(audioManager);
                    Debug.Log(did
                        ? "✅ Built AudioData enums (AudioName/SFXNameEnum & MFXNameEnum)"
                        : "ℹ️ AudioData enums already up to date");
                }
            }
        }
    }
}
