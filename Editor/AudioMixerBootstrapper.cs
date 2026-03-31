using UnityEditor;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Editor
{
    [InitializeOnLoad]
    internal static class AudioMixerBootstrapper
    {
        static AudioMixerBootstrapper()
        {
            EditorApplication.delayCall += Ensure;
        }

        private static void Ensure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Ensure;
                return;
            }

            // Create GameMixer only once (first time package is added to this project).
            AudioMixerGenerator.EnsureGameMixerCreatedOnce();

            AudioMixerExposedParameterGenerator.EnsureGeneratedIfMissing();
            AudioMixerGroupNameGenerator.EnsureGeneratedIfMissing();

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>($"{Util.GENERATED_FOLDER_PATH}/GameMixer.mixer");
            AudioManagerMixerAutoRef.WireAllAudioDataManagerAssets(mixer);
        }
    }
}
