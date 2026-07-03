using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    public interface ISetterParameterizable : IParameterizable
    {
        AudioSource SetValue(AudioSource source);
    }
}