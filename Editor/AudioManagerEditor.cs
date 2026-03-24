using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(Runtime.AudioManager))]
    internal sealed class AudioManagerEditor : NaughtyInspector
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
                    var audioManager = (Runtime.AudioManager)target;
                    var did = AudioNameEnumGenerator.BuildFromAllAudioDataAssets(force: true, audioManager);
                    Debug.Log(did
                        ? "✅ Built AudioData enums (AudioName/SFXNameEnum/MFXNameEnum)"
                        : "ℹ️ AudioData enums already up to date");
                }
            }
        }
    }
}
