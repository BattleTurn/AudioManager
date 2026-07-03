using System;
using UnityEngine;

namespace BattleTurn.AudioManagement.Runtime
{
    /// <summary>
    /// Draws a dropdown for a string field, populated with parameter names
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StringDropdownAttribute : PropertyAttribute
    {
        public Type ConstantType { get; }
        public FieldType FieldType { get; }

        public StringDropdownAttribute(Type constantType, FieldType fieldType = FieldType.All)
        {
            ConstantType = constantType;
            FieldType = fieldType;
        }

    }

    public enum FieldType
    {
        ReadOnlyStaticFields,
        ConstantFields,
        All,
    }
}