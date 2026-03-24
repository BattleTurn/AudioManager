using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BattleTurn.AudioManager.Editor
{
    /// <summary>
    /// Ensures generated AudioData enums are up to date before building a player.
    /// If generation changes scripts, the build is stopped so Unity can recompile; run build again.
    /// </summary>
    internal sealed class AudioDataEnumBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var did = AudioNameEnumGenerator.BuildFromAllAudioDataAssets(force: false);
            if (!did)
                return;

            AssetDatabase.Refresh();
            throw new BuildFailedException(
                "AudioData enums were regenerated (AudioName/SFXNameEnum/MFXNameEnum). " +
                "Unity needs to recompile scripts; please build again.");
        }
    }
}
