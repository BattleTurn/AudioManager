using NaughtyAttributes;
using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    public abstract class AudioCategoryBaseSO : ScriptableObject, IAudioCategory
    {
        [OnValueChanged(nameof(Rename))]
        [Invalid(typeof(CSharpKeywords), nameof(CSharpKeywords.All), autoFix: false)]
        [NoSpace, SerializeField] private string _name = "New Category";

        public string Name => _name;
        public abstract AudioContentBaseSO[] AudioClips { get; }

        public abstract AudioClip this[string audioName] { get; }

        public abstract bool TryGetClip(string audioName, out AudioClip clip);

        private void Rename()
        {
            var safeName = GetSafeAssetName(_name);
            _name = safeName;

#if UNITY_EDITOR
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var currentAssetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (!string.Equals(currentAssetName, safeName, System.StringComparison.Ordinal))
                {
                    name = currentAssetName;

                    var renameError = UnityEditor.AssetDatabase.RenameAsset(assetPath, safeName);
                    if (!string.IsNullOrEmpty(renameError))
                    {
                        _name = currentAssetName;
                        Debug.LogWarning($"Failed to rename asset file: {renameError}", this);
                        UnityEditor.EditorUtility.SetDirty(this);
                        return;
                    }
                }

                UnityEditor.AssetDatabase.SaveAssets();
            }

            name = safeName;
            UnityEditor.EditorUtility.SetDirty(this);
#else
            name = safeName;
#endif
        }

        private static string GetSafeAssetName(string rawName)
        {
            var safeName = string.IsNullOrWhiteSpace(rawName) ? "New Category" : rawName.Trim();

            foreach (var invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            return safeName;
        }
    }
}