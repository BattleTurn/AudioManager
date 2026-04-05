using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    public interface ISetterParameterizable : IParameterizable
    {
        AudioSource SetValue(AudioSource source);
    }
}