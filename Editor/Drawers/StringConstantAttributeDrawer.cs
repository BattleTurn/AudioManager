using System.Collections.Generic;
using System.Reflection;
using BattleTurn.AudioManager.Runtime;
using UnityEditor;

namespace BattleTurn.AudioManager.Editor
{
    [CustomPropertyDrawer(typeof(StringDropdownAttribute))]
    internal sealed class StringConstantAttributeDrawer : StringDropdownDrawer
    {
        internal override string ClassTypeName => nameof(StringDropdownAttribute);

        protected override List<string> GetOptions()
        {
            var dropdownAttribute = attribute as StringDropdownAttribute;
            if (dropdownAttribute == null || dropdownAttribute.ConstantType == null)
                return new List<string>();

            var results = new List<string>();
            var unique = new HashSet<string>();

            var currentType = dropdownAttribute.ConstantType;
            while (currentType != null)
            {
                var fields = currentType.GetFields(BindingFlags.Public |
                                                   BindingFlags.NonPublic |
                                                   BindingFlags.Static |
                                                   BindingFlags.DeclaredOnly);

                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (field.FieldType != typeof(string))
                        continue;

                    var include = dropdownAttribute.FieldType switch
                    {
                        FieldType.ReadOnlyStaticFields => field.IsStatic && field.IsInitOnly && !field.IsLiteral,
                        FieldType.ConstantFields => field.IsLiteral && !field.IsInitOnly,
                        FieldType.All => (field.IsStatic && field.IsInitOnly && !field.IsLiteral) ||
                                         (field.IsLiteral && !field.IsInitOnly),
                        _ => false
                    };

                    if (!include)
                        continue;

                    string value;
                    if (field.IsLiteral)
                    {
                        value = field.GetRawConstantValue() as string;
                    }
                    else
                    {
                        value = field.GetValue(null) as string;
                    }

                    if (value == null || !unique.Add(value))
                        continue;

                    results.Add(value);
                }

                currentType = currentType.BaseType;
            }

            return results;
        }
    }
}