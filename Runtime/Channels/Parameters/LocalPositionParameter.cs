using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    public struct LocalPositionParameter : ISetterParameterizable
    {
        [SerializeField] private Vector3 _localPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalPositionParameter"/> struct with the specified local position.
        /// </summary>
        /// <param name="localPosition">The local position to set for the AudioSource.</param>
        public LocalPositionParameter(Vector3 localPosition)
        {
            _localPosition = localPosition;
        }

        public AudioSource SetValue(AudioSource source)
        {
            source.transform.localPosition = _localPosition;
            return source;
        }
    }
}