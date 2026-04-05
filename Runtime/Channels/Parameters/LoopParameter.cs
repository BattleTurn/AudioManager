using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    public struct LoopParameter : ISetterParameterizable
    {
        [SerializeField] private short _loopAmount;

        public short Value => _loopAmount;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopParameter"/> struct with the specified value.
        /// </summary>
        /// <param name="amount">
        /// The loop amount. Represents the number of times the audio should loop.
        /// A value of 0 means the audio will play once, 1 means it will loop once (play twice), and so on.
        /// A value of -1 can be used to indicate infinite looping.
        /// </param>
        public LoopParameter(short amount)
        {
            if (amount < -1)
                Debug.LogWarning($"Loop amount cannot be less than -1. Clamping to -1.");
            amount = (short)Mathf.Clamp(amount, -1, short.MaxValue);
            _loopAmount = amount;
        }

        public AudioSource SetValue(AudioSource source)
        {
            if (source != null)
                source.loop = _loopAmount < 0;

            return source;
        }
    }
}