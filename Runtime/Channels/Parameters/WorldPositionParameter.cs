using System;
using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    [Serializable]
    public struct WorldPositionParameter : ISetterParameterizable
    {
        [SerializeField] private Vector3 _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldPositionParameter"/> struct with the specified position.
        /// </summary>
        /// <param name="position">The world position to set for the AudioSource.</param>
        public WorldPositionParameter(Vector3 position)
        {
            _position = position;
        }

        public AudioSource SetValue(AudioSource source)
        {
            source.transform.position = _position;
            return source;
        }
    }
}