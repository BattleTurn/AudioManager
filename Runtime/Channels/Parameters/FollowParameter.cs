using System;
using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    [Serializable]
    public struct FollowParameter : ISetterParameterizable
    {
        [SerializeField] private Transform _target;
        [SerializeField] private bool _isZeroLocalPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="FollowParameter"/> struct with the specified target and zero local position option.
        /// </summary>
        /// <param name="target">The Transform target to follow.</param>
        /// <param name="isZeroLocalPosition">Whether to set the local position to zero when following the target.</param>
        public FollowParameter(Transform target, bool isZeroLocalPosition = true)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "FollowParameter requires a non-null Transform target.");
            }

            _target = target;
            _isZeroLocalPosition = isZeroLocalPosition;
        }

        public AudioSource SetValue(AudioSource source)
        {
            source.transform.parent = _target;
            source.transform.localPosition = _isZeroLocalPosition ? Vector3.zero : source.transform.localPosition;
            return source;
        }
    }
}