using System;
using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AudioNameDropdownAttribute : PropertyAttribute
    {
        public string CategoryFieldName { get; }
        public string AudioTypeFieldName { get; }

        public AudioNameDropdownAttribute(string categoryFieldName, string audioTypeFieldName = "audioType")
        {
            CategoryFieldName = categoryFieldName;
            AudioTypeFieldName = audioTypeFieldName;
        }
    }
}