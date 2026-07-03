using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTurn.AudioManagement.Runtime;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManagement.Editor
{
    [CustomPropertyDrawer(typeof(StringRuleAttribute), true)]
    public sealed class StringRuleAttributeDrawer : PropertyDrawer
    {
        private const string WrongTypeMessage = "attribute only for string";
        private const float WarningIconWidth = 18f;
        private const string DuplicateWarningTooltip = "Duplicate audio name. Auto-fix on end edit (Enter or blur).";
        private const string DuplicateMessageFormat = "Duplicate audio name: '{0}'. Auto-fix on end edit (Enter or blur).";

        private static readonly Dictionary<(Type type, string memberName), HashSet<string>> ProviderCache = new();

        private readonly struct RuleSet
        {
            public RuleSet(bool hasNoSpace, InvalidAttribute[] invalidRules)
            {
                HasNoSpace = hasNoSpace;
                InvalidRules = invalidRules;
            }

            public bool HasNoSpace { get; }
            public InvalidAttribute[] InvalidRules { get; }
        }

        private readonly struct DuplicateNameState
        {
            public DuplicateNameState(bool hasDuplicate, HashSet<string> siblingValues)
            {
                HasDuplicate = hasDuplicate;
                SiblingValues = siblingValues;
            }

            public bool HasDuplicate { get; }
            public HashSet<string> SiblingValues { get; }
        }

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
            var ruleSet = CreateRuleSet(rules);

            var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var shouldCheckDuplicateName = ShouldCheckDuplicateName(property);
            var duplicateState = GetDuplicateNameState(property, property.stringValue);
            var showDuplicateWarning = duplicateState.HasDuplicate;
            EditorGUI.BeginProperty(lineRect, label, property);
            var fieldRect = EditorGUI.PrefixLabel(lineRect, label);

            if (shouldCheckDuplicateName)
                fieldRect.width -= WarningIconWidth;

            var controlName = $"StringRule_{property.propertyPath}";
            var currentEvent = Event.current;
            var shouldForceAutoFixByEnter = ShouldForceAutoFixByEnter(controlName, showDuplicateWarning, currentEvent);

            GUI.SetNextControlName(controlName);
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUI.DelayedTextField(fieldRect, GUIContent.none, property.stringValue);
            var valueChanged = EditorGUI.EndChangeCheck();
            if (valueChanged)
            {
                newValue = NormalizeValue(property, newValue, shouldCheckDuplicateName, ruleSet);

                property.stringValue = newValue;

                duplicateState = GetDuplicateNameState(property, newValue);
                showDuplicateWarning = duplicateState.HasDuplicate;
            }

            if (showDuplicateWarning && (shouldForceAutoFixByEnter || valueChanged))
            {
                var uniqueValue = MakeUniqueValue(property.stringValue, duplicateState.SiblingValues);
                uniqueValue = ApplyInvalidAutoFix(uniqueValue, ruleSet.InvalidRules);

                if (!string.Equals(uniqueValue, property.stringValue, StringComparison.Ordinal))
                {
                    property.stringValue = uniqueValue;
                    duplicateState = GetDuplicateNameState(property, uniqueValue);
                    showDuplicateWarning = duplicateState.HasDuplicate;

                    if (shouldForceAutoFixByEnter)
                    {
                        currentEvent.Use();
                        GUI.changed = true;
                    }
                }
                else if (shouldForceAutoFixByEnter)
                {
                    currentEvent.Use();
                    GUI.changed = true;
                }
            }

            if (showDuplicateWarning)
            {
                DrawDuplicateWarningIcon(lineRect);
            }

            EditorGUI.EndProperty();

            DrawValidationMessages(position, property, ruleSet.InvalidRules);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return EditorGUIUtility.singleLineHeight;

            var invalidRules = CreateRuleSet(GetAllRules()).InvalidRules;
            var extraLines = GetValidationMessageCount(property, invalidRules);

            if (extraLines > 0)
                return EditorGUIUtility.singleLineHeight +
                       (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * extraLines;

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

        private static RuleSet CreateRuleSet(StringRuleAttribute[] rules)
        {
            return new RuleSet(
                rules.OfType<NoSpaceAttribute>().Any(),
                rules.OfType<InvalidAttribute>().ToArray());
        }

        private static bool ShouldForceAutoFixByEnter(string controlName, bool showDuplicateWarning, Event currentEvent)
        {
            return showDuplicateWarning &&
                   GUI.GetNameOfFocusedControl() == controlName &&
                   currentEvent.type == EventType.KeyDown &&
                   (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter);
        }

        private static string NormalizeValue(SerializedProperty property, string value, bool shouldCheckDuplicateName, RuleSet ruleSet)
        {
            if (ruleSet.HasNoSpace && !string.IsNullOrEmpty(value) && value.IndexOf(' ') >= 0)
                value = value.Replace(" ", string.Empty);

            value = ApplyInvalidAutoFix(value, ruleSet.InvalidRules);

            if (!shouldCheckDuplicateName || string.IsNullOrWhiteSpace(value))
                return value;

            if (!TryGetSiblingValues(property, out var siblingValues) || !siblingValues.Contains(value))
                return value;

            value = MakeUniqueValue(value, siblingValues);
            return ApplyInvalidAutoFix(value, ruleSet.InvalidRules);
        }

        private static void DrawDuplicateWarningIcon(Rect lineRect)
        {
            var warningRect = new Rect(
                lineRect.xMax - WarningIconWidth,
                lineRect.y,
                WarningIconWidth,
                EditorGUIUtility.singleLineHeight);

            var warningContent = EditorGUIUtility.IconContent("console.warnicon.sml");
            warningContent.tooltip = DuplicateWarningTooltip;
            GUI.Label(warningRect, warningContent);
        }

        private static void DrawValidationMessages(Rect position, SerializedProperty property, InvalidAttribute[] invalidRules)
        {
            var messages = GetValidationMessages(property, invalidRules);
            if (messages.Count == 0)
                return;

            var messageY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            foreach (var message in messages)
            {
                var errorRect = new Rect(
                    position.x,
                    messageY,
                    position.width,
                    EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(errorRect, GUIContent.none, message);
                EditorGUI.EndDisabledGroup();

                messageY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private static List<string> GetValidationMessages(SerializedProperty property, InvalidAttribute[] invalidRules)
        {
            var messages = new List<string>(2);

            var duplicateErrorMessage = GetDuplicateErrorMessage(property);
            if (!string.IsNullOrEmpty(duplicateErrorMessage))
                messages.Add(duplicateErrorMessage);

            var invalidErrorMessage = GetInvalidErrorMessage(property.stringValue, invalidRules);
            if (!string.IsNullOrEmpty(invalidErrorMessage))
                messages.Add(invalidErrorMessage);

            return messages;
        }

        private static int GetValidationMessageCount(SerializedProperty property, InvalidAttribute[] invalidRules)
        {
            return GetValidationMessages(property, invalidRules).Count;
        }

        private static DuplicateNameState GetDuplicateNameState(SerializedProperty property, string value)
        {
            if (!ShouldCheckDuplicateName(property) || string.IsNullOrWhiteSpace(value))
                return default;

            if (!TryGetSiblingValues(property, out var siblingValues))
                return default;

            return new DuplicateNameState(siblingValues.Contains(value), siblingValues);
        }

        private static string MakeUniqueValue(string value, HashSet<string> siblingValues)
        {
            if (string.IsNullOrWhiteSpace(value) || siblingValues == null || siblingValues.Count == 0)
                return value;

            if (!siblingValues.Contains(value))
                return value;

            for (var suffix = 1; suffix < int.MaxValue; suffix++)
            {
                var candidate = $"{value}_{suffix}";
                if (!siblingValues.Contains(candidate))
                    return candidate;
            }

            return value;
        }

        private static bool ShouldCheckDuplicateName(SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.String || property.name != "name")
                return false;

            return TryGetParentArrayProperty(property, out var arrayProperty) &&
                   arrayProperty != null &&
                   IsAudioContentsPropertyPath(arrayProperty.propertyPath);
        }

        private static bool IsAudioContentsPropertyPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return false;

            return string.Equals(propertyPath, "audioContents", StringComparison.Ordinal) ||
                   propertyPath.EndsWith(".audioContents", StringComparison.Ordinal);
        }

        private static string GetDuplicateErrorMessage(SerializedProperty property)
        {
            if (!ShouldCheckDuplicateName(property))
                return null;

            var value = property.stringValue;
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!TryGetSiblingValues(property, out var siblingValues))
                return null;

            return siblingValues.Contains(value)
                ? string.Format(DuplicateMessageFormat, value)
                : null;
        }

        private static bool TryGetSiblingValues(SerializedProperty property, out HashSet<string> siblingValues)
        {
            siblingValues = null;

            if (!TryGetParentArrayProperty(property, out var arrayProperty) || arrayProperty == null || !arrayProperty.isArray)
                return false;

            siblingValues = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < arrayProperty.arraySize; index++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(index);
                if (element == null)
                    continue;

                var siblingProperty = element.FindPropertyRelative(property.name);
                if (siblingProperty == null || siblingProperty.propertyPath == property.propertyPath)
                    continue;

                var siblingValue = siblingProperty.stringValue;
                if (!string.IsNullOrWhiteSpace(siblingValue))
                    siblingValues.Add(siblingValue);
            }

            return true;
        }

        private static bool TryGetParentArrayProperty(SerializedProperty property, out SerializedProperty arrayProperty)
        {
            arrayProperty = null;

            var propertyPath = property?.propertyPath;
            if (string.IsNullOrEmpty(propertyPath))
                return false;

            var arraySegmentIndex = propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            if (arraySegmentIndex < 0)
                return false;

            var arrayPath = propertyPath.Substring(0, arraySegmentIndex);
            if (string.IsNullOrEmpty(arrayPath))
                return false;

            arrayProperty = property.serializedObject.FindProperty(arrayPath);
            return arrayProperty != null;
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
