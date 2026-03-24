using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    public static class Util
    {
        public static void EnsureFolderExists(string unityFolderPath)
        {
            if (AssetDatabase.IsValidFolder(unityFolderPath))
                return;

            var parts = unityFolderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}