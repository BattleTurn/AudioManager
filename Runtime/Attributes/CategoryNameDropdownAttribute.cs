using System;
using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CategoryNameDropdownAttribute : PropertyAttribute
    {
        public string AudioTypeFieldName { get; }

        public CategoryNameDropdownAttribute(string audioTypeFieldName = "audioType")
        {
            AudioTypeFieldName = audioTypeFieldName;
        }
    }
}