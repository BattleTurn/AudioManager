using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    public struct FadeParameter : IParameterizable
    {
        [SerializeField] private float _duration;

        public float Duration => _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FadeParameter"/> struct with the specified duration.
        /// </summary>
        /// <param name="duration">The duration of the fade effect in seconds.</param>
        public FadeParameter(float duration)
        {
            _duration = duration;
        }
    }
}