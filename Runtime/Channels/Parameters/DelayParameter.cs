using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    public enum DelayType
    {
        OnStart,
        EveryLoop,
    }

    public struct DelayParameter : IParameterizable
    {
        [SerializeField] private float _delayTime;
        [SerializeField] private DelayType _delayType;

        public float DelayTime => _delayTime;
        public DelayType DelayType => _delayType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayParameter"/> struct with the specified delay time and delay type.
        /// </summary>
        /// <param name="delayTime">The delay time in seconds.</param>
        /// <param name="delayType">The type of delay to apply.</param>
        public DelayParameter(float delayTime, DelayType delayType = DelayType.OnStart)
        {
            _delayTime = delayTime;
            _delayType = delayType;
        }
    }
}