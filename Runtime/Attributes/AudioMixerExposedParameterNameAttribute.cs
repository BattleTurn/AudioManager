using System;
using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    /// <summary>
    /// Draws a dropdown for a string field, populated with exposed parameter names
    /// from AudioManager.AudioMixer (editor-only drawer).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AudioMixerExposedParameterNameAttribute : PropertyAttribute
    {
    }
}
