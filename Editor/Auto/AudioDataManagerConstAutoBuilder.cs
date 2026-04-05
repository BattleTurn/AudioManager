using BattleTurn.AudioManager.Runtime;
using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    internal sealed class AudioDataManagerConstAutoBuilder : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return paths;

            foreach (var path in paths)
            {
                var manager = AssetDatabase.LoadAssetAtPath<AudioDataManagerSO>(path);
                if (manager == null || !EditorUtility.IsDirty(manager))
                    continue;

                AudioDataManagerConstGenerator.Build(manager);
            }

            return paths;
        }
    }
}