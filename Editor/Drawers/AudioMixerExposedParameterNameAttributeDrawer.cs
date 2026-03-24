using System;
using System.Collections.Generic;
using System.Linq;
using BattleTurn.AudioManager.Runtime;
using UnityEditor;
using UnityEngine;

using RuntimeAudioManager = BattleTurn.AudioManager.Runtime.AudioManager;

namespace BattleTurn.AudioManager.Editor
{
    [CustomPropertyDrawer(typeof(AudioMixerExposedParameterNameAttribute))]
    internal sealed class AudioMixerExposedParameterNameAttributeDrawer : PropertyDrawer
    {
        private static readonly GUIContent NoneOption = new GUIContent("<None>");
        private static readonly GUIContent NoOptionsOption = new GUIContent("<No exposed parameters found>");
        private const string DefaultGameMixerUnityPath = "Assets/Plugins/BattleTurn/Generated/AudioManager/GameMixer.mixer";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return EditorGUIUtility.singleLineHeight * 2f;

            var options = GetExposedParamOptions();
            if (options.Count == 0)
                return EditorGUIUtility.singleLineHeight * 2f + 2f;

            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "AudioMixerExposedParameterName works only on string fields.");
                return;
            }

            var options = GetExposedParamOptions();

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
                    "No exposed parameters found. Ensure GameMixer exists and has exposed parameters, or assign a mixer to an AudioManager asset.",
                    MessageType.Info);
            }

            EditorGUI.EndProperty();
        }

        private static List<string> GetExposedParamOptions()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            // Prefer AudioManager assets as the source of truth.
            var guids = AssetDatabase.FindAssets($"t:{nameof(RuntimeAudioManager)}");
            if (guids != null)
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var audioManager = AssetDatabase.LoadAssetAtPath<RuntimeAudioManager>(path);
                    if (audioManager == null)
                        continue;

                    var mixer = audioManager.AudioMixer;
                    if (mixer == null)
                        continue;

                    foreach (var n in AudioMixerUtil.GetExposedParams(mixer, debug: false))
                    {
                        if (!string.IsNullOrWhiteSpace(n))
                            names.Add(n);
                    }
                }
            }

            // Fallback: use the generated GameMixer even if there is no AudioManager asset yet.
            if (names.Count == 0)
            {
                var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(DefaultGameMixerUnityPath);
                if (mixer != null)
                {
                    foreach (var n in AudioMixerUtil.GetExposedParams(mixer, debug: false))
                    {
                        if (!string.IsNullOrWhiteSpace(n))
                            names.Add(n);
                    }
                }
            }

            return names.OrderBy(n => n, StringComparer.Ordinal).ToList();
        }
    }
}
