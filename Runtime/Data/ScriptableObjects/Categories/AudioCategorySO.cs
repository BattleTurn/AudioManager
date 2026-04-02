using UnityEngine;
using NaughtyAttributes;

namespace BattleTurn.AudioManager.Runtime
{
    [CreateAssetMenu(fileName = "New " + TYPE_NAME, menuName = CREATE_ASSET_PATH)]
    public class AudioCategorySO : ScriptableObject
    {
        private const string CREATE_ASSET_PATH = ScriptableConstants.ASSET_MENU_PATH + TYPE_NAME;
        private const string TYPE_NAME = nameof(AudioCategorySO);

        [Invalid(typeof(CSharpKeywords), nameof(CSharpKeywords.All), autoFix: false)]
        [SerializeField] private string _name = "New " + TYPE_NAME;
        [SerializeField] private AudioDataBaseSO[] _audioData;

        #region PROPERTIES
        public string Name => _name;
        public AudioDataBaseSO[] AudioData => _audioData;
        #endregion

        [Button]
        private void Rename()
        {
            var safeName = GetSafeAssetName(_name);
            _name = safeName;
            name = safeName;

#if UNITY_EDITOR
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var renameError = UnityEditor.AssetDatabase.RenameAsset(assetPath, safeName);
                if (!string.IsNullOrEmpty(renameError))
                {
                    Debug.LogWarning($"Failed to rename asset file: {renameError}", this);
                }
                else
                {
                    UnityEditor.AssetDatabase.SaveAssets();
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private static string GetSafeAssetName(string rawName)
        {
            var safeName = string.IsNullOrWhiteSpace(rawName) ? TYPE_NAME : rawName.Trim();

            foreach (var invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            return safeName;
        }
    }
}