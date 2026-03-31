
using BattleTurn.AudioManager.Runtime;
using NaughtyAttributes.Editor;
using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(AudioDataTemplateSO))]
    internal class AudioDataTemplateSOEditor : NaughtyInspector
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            var audioData = target as AudioDataBaseSO;
            if (audioData == null)
                return;

            AudioDataSOEditor.DrawValidationAndBuild(audioData);

            serializedObject.ApplyModifiedProperties();
        }
    }
}