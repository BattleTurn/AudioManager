using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.AudioManager.Editor
{
    internal abstract class StringDropdownDrawer : PropertyDrawer
    {
        private static readonly GUIContent NoneOption = new GUIContent("<None>");
        private static readonly GUIContent NoOptionsOption = new GUIContent("<No options found>");

        internal abstract string ClassTypeName { get; }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return EditorGUIUtility.singleLineHeight * 2f;

            List<string> options = GetOptions();
            if (options.Count == 0)
                return EditorGUIUtility.singleLineHeight * 2f + 2f;

            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, $"❌ {ClassTypeName}: works only on string fields.");
                return;
            }

            List<string> options = GetOptions();

            var showHelp = options.Count == 0;
            if (showHelp)
                options.Add(string.Empty);

            var current = property.stringValue ?? string.Empty;
            var selectedIndex = 0; // None

            for (var i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i], current, StringComparison.Ordinal))
                {
                    selectedIndex = i + 1; // +1 because of None
                    break;
                }
            }

            EditorGUI.BeginProperty(position, label, property);

            var displayOptions = new GUIContent[options.Count + 1];
            displayOptions[0] = NoneOption;
            for (var i = 0; i < options.Count; i++)
            {
                displayOptions[i + 1] = showHelp
                    ? NoOptionsOption
                    : new GUIContent(options[i]);
            }

            var newIndex = EditorGUI.Popup(position, label, selectedIndex, displayOptions);
            if (newIndex != selectedIndex)
            {
                property.stringValue = newIndex == 0 ? string.Empty : options[newIndex - 1];
            }

            if (showHelp)
            {
                var helpRect = position;
                helpRect.y += EditorGUIUtility.singleLineHeight + 2f;
                helpRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.HelpBox(helpRect,
                    $"⚠ No options found. Ensure the source provides options or assign a valid source for {ClassTypeName}.",
                    MessageType.Info);
            }

            EditorGUI.EndProperty();
        }

        protected abstract List<string> GetOptions();
    }
}