using System.Collections.Generic;
using BattleTurn.AudioManagement.Runtime;
using UnityEditor;

namespace BattleTurn.AudioManagement.Editor
{
    [CustomPropertyDrawer(typeof(CategoryNameDropdownAttribute))]
    internal sealed class CategoryNameDropdownAttributeDrawer : StringDropdownDrawer
    {
        internal override string ClassTypeName => nameof(CategoryNameDropdownAttribute);

        protected override List<string> GetOptions(SerializedProperty property)
        {
            var dropdownAttribute = attribute as CategoryNameDropdownAttribute;
            return AudioDataDropdownUtil.GetCategoryNames(property, dropdownAttribute?.AudioTypeFieldName ?? "audioType");
        }
    }
}