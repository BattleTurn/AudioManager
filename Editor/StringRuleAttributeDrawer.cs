using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTurn.AudioManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    [CustomPropertyDrawer(typeof(StringRuleAttribute), true)]
    public sealed class StringRuleAttributeDrawer : PropertyDrawer
    {
        private const string WrongTypeMessage = "attribute only for string";

        private static readonly Dictionary<(Type type, string memberName), HashSet<string>> ProviderCache = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(position, label, WrongTypeMessage);
                EditorGUI.EndDisabledGroup();
                return;
            }

            var rules = GetAllRules();

            var hasNoSpace = rules.OfType<NoSpaceAttribute>().Any();
            var invalidRules = rules.OfType<InvalidAttribute>().ToArray();

            var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(lineRect, label, property);
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUI.TextField(lineRect, label, property.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (hasNoSpace && !string.IsNullOrEmpty(newValue) && newValue.IndexOf(' ') >= 0)
                    newValue = newValue.Replace(" ", string.Empty);

                newValue = ApplyInvalidAutoFix(newValue, invalidRules);
                property.stringValue = newValue;
            }
            EditorGUI.EndProperty();

            // Show error textbox when any InvalidAttribute matches and autoFix is disabled.
            var current = property.stringValue;
            var errorMessage = GetInvalidErrorMessage(current, invalidRules);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                var errorRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(errorRect, GUIContent.none, errorMessage);
                EditorGUI.EndDisabledGroup();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return EditorGUIUtility.singleLineHeight;

            var rules = GetAllRules();
            var invalidRules = rules.OfType<InvalidAttribute>().ToArray();

            var errorMessage = GetInvalidErrorMessage(property.stringValue, invalidRules);
            if (!string.IsNullOrEmpty(errorMessage))
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight;
        }

        private StringRuleAttribute[] GetAllRules()
        {
            // Collect ALL StringRuleAttribute-derived attributes on the field.
            // This is the key to supporting multiple rules even though Unity uses only one PropertyDrawer.
            return fieldInfo
                .GetCustomAttributes(typeof(StringRuleAttribute), inherit: true)
                .OfType<StringRuleAttribute>()
                .ToArray();
        }

        private static string ApplyInvalidAutoFix(string value, InvalidAttribute[] invalidRules)
        {
            if (invalidRules == null || invalidRules.Length == 0)
                return value;

            foreach (var rule in invalidRules)
            {
                if (rule == null || !rule.AutoFix)
                    continue;

                var reserved = GetReservedSet(rule);
                if (reserved != null && reserved.Contains(value))
                    value = "_" + value;
            }

            return value;
        }

        private static string GetInvalidErrorMessage(string value, InvalidAttribute[] invalidRules)
        {
            if (invalidRules == null || invalidRules.Length == 0)
                return null;

            foreach (var rule in invalidRules)
            {
                if (rule == null || rule.AutoFix)
                    continue;

                var reserved = GetReservedSet(rule);
                if (reserved != null && reserved.Contains(value))
                    return $"Reserved word: '{value}'";
            }

            return null;
        }

        private static HashSet<string> GetReservedSet(InvalidAttribute attr)
        {
            if (attr == null)
                return null;

            // Inline words take precedence if provided.
            if (attr.InlineWords != null && attr.InlineWords.Length > 0)
                return new HashSet<string>(attr.InlineWords.Where(w => !string.IsNullOrWhiteSpace(w)), StringComparer.Ordinal);

            if (attr.ProviderType == null || string.IsNullOrEmpty(attr.MemberName))
                return null;

            var key = (attr.ProviderType, attr.MemberName);
            if (ProviderCache.TryGetValue(key, out var cached))
                return cached;

            var set = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

                var member = attr.ProviderType.GetMember(attr.MemberName, flags).FirstOrDefault();
                object value = null;

                if (member is FieldInfo fi)
                {
                    value = fi.GetValue(null);
                }
                else if (member is PropertyInfo pi)
                {
                    value = pi.GetValue(null, null);
                }
                else if (member is MethodInfo mi)
                {
                    if (mi.GetParameters().Length == 0)
                        value = mi.Invoke(null, null);
                }

                if (value is IEnumerable<string> enumerable)
                {
                    foreach (var w in enumerable)
                    {
                        if (!string.IsNullOrWhiteSpace(w))
                            set.Add(w);
                    }
                }
                else if (value is IEnumerable nonGeneric)
                {
                    foreach (var item in nonGeneric)
                    {
                        if (item is string s && !string.IsNullOrWhiteSpace(s))
                            set.Add(s);
                    }
                }
            }
            catch
            {
                // drawer should never hard-fail the inspector
            }

            ProviderCache[key] = set;
            return set;
        }
    }
}
