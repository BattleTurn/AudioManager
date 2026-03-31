using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    [CreateAssetMenu(fileName = TYPE_NAME, menuName = CREATE_ASSET_PATH)]
    public class AudioDataSO : AudioDataBaseSO
    {
        private const string CREATE_ASSET_PATH = ScriptableConstants.ASSET_MENU_PATH + TYPE_NAME;
        private const string TYPE_NAME = nameof(AudioDataSO);

        [SerializeField] private string _name;

        #region PROPERTIES
        public override string Name => _name;
        #endregion
    }
}