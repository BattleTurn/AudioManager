using System;
using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
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