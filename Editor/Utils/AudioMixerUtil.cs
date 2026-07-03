using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManagement.Editor
{
    public static class AudioMixerUtil
    {
        public static List<string> GetExposedParams(AudioMixer mixer, bool debug = false)
        {
            if (mixer == null)
                return new List<string>();

            List<string> paramNames = new List<string>();
            var so = new SerializedObject(mixer);
            so.Update();
            var exposedParams = so.FindProperty("m_ExposedParameters");

            if (exposedParams == null)
            {
                Debug.LogError("❌ Cannot find m_ExposedParameters");
                return paramNames;
            }

            for (int i = 0; i < exposedParams.arraySize; i++)
            {
                var element = exposedParams.GetArrayElementAtIndex(i);

                var nameProp = element.FindPropertyRelative("name");

                string name = nameProp.stringValue;

                if (debug)
                {
                    Debug.Log($"🎚 Exposed: {name}");
                }
                paramNames.Add(name);
            }

            return paramNames;
        }
    }
}