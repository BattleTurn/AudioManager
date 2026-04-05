using System.Collections.Generic;
using BattleTurn.AudioManager.Runtime;
using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    [CustomPropertyDrawer(typeof(AudioNameDropdownAttribute))]
    internal sealed class AudioNameDropdownAttributeDrawer : StringDropdownDrawer
    {
        internal override string ClassTypeName => nameof(AudioNameDropdownAttribute);

        protected override List<string> GetOptions(SerializedProperty property)
        {
            var dropdownAttribute = attribute as AudioNameDropdownAttribute;
            if (dropdownAttribute == null)
                return new List<string>();

            return AudioDataDropdownUtil.GetAudioNames(property, dropdownAttribute.AudioTypeFieldName, dropdownAttribute.CategoryFieldName);
        }
    }
}