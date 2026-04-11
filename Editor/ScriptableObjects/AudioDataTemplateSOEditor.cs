
using BattleTurn.AudioManager.Runtime;
using NaughtyAttributes.Editor;
using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    [CustomEditor(typeof(AudioTemplateAlbumSO))]
    internal class AudioDataTemplateSOEditor : NaughtyInspector
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            var audioData = target as AudioAlbumBaseSO;
            if (audioData == null)
                return;

            AudioAlbumSOEditor.DrawValidation(audioData);

            serializedObject.ApplyModifiedProperties();
        }
    }
}