using System;
using System.Collections.Generic;
using System.Linq;
using BattleTurn.AudioManager.Runtime;
using UnityEditor;
using UnityEngine.Audio;

using RuntimeAudioManager = BattleTurn.AudioManager.Runtime.AudioAlbumManagerSO;

namespace BattleTurn.AudioManager.Editor
{
    [CustomPropertyDrawer(typeof(AudioMixerExposedParameterNameAttribute))]
    internal sealed class AudioMixerExposedParameterNameAttributeDrawer : StringDropdownDrawer
    {
        private const string DEFAULT_GAME_MIXER_PATH = CodeGenerationUtils.GENERATED_FOLDER_PATH + "/GameMixer.mixer";

        internal override string ClassTypeName => nameof(AudioMixerExposedParameterNameAttribute);

        protected override List<string> GetOptions(SerializedProperty property)
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
                var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(DEFAULT_GAME_MIXER_PATH);
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
