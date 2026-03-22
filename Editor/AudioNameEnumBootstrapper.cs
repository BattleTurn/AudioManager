using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    [InitializeOnLoad]
    internal static class AudioNameEnumBootstrapper
    {
        static AudioNameEnumBootstrapper()
        {
            // Auto-generate on project load so AudioName always exists (at minimum: None).
            // Use delayCall to avoid re-entrant import/compile loops during domain reload.
            EditorApplication.delayCall += () => AudioNameEnumGenerator.BuildFromAllAudioDataAssets(force: false);
        }
    }
}
