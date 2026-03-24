using System;
using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    [Serializable]
    public struct AudioMixParameter
    {
        [SerializeField, AudioMixerExposedParameterName] private string _name;
        [SerializeField] private float _value;

        public string Name => _name;
        public float Value => _value;

        public AudioMixParameter(string name, float value)
        {
            _name = name;
            _value = value;
        }
    }
}