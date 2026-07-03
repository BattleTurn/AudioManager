using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManagement.Runtime
{
    public static class AudioSourceExtension
    {
        public static void SetParameters(this AudioMixerGroup mixerGroup, params IAudioMixable[] parameters)
        {
            foreach (var param in parameters)
            {
                mixerGroup.audioMixer.SetFloat(param.Name, param.Value);
            }
        }

        internal static void SetParameters(this AudioSource source, Action<AudioSource, IParameterizable> parameterCallback, IEnumerable<IParameterizable> parameters)
        {
            foreach (var param in parameters)
            {
                switch (param)
                {
                    case WorldPositionParameter worldPosParam:
                        worldPosParam.SetValue(source);
                        parameterCallback?.Invoke(source, worldPosParam);
                        break;
                    case LocalPositionParameter localPosParam:
                        localPosParam.SetValue(source);
                        parameterCallback?.Invoke(source, localPosParam);
                        break;
                    case FollowParameter followParam:
                        followParam.SetValue(source);
                        parameterCallback?.Invoke(source, followParam);
                        break;
                    case DelayParameter delayParam:
                        parameterCallback?.Invoke(source, delayParam);
                        break;
                    case LoopParameter loopParam:
                        loopParam.SetValue(source);
                        parameterCallback?.Invoke(source, loopParam);
                        break;
                    case OneShotParameter oneShotParam:
                        parameterCallback?.Invoke(source, oneShotParam);
                        break;
                }
            }
        }

        internal static void Play(this AudioSource source, bool hasOneShot, float volume, DelayParameter? delayParameter, bool applyDelay)
        {
            source.volume = volume;

            if (applyDelay && delayParameter.HasValue && delayParameter.Value.DelayTime > 0f)
            {
                source.PlayDelayed(delayParameter.Value.DelayTime);
            }
            else if (hasOneShot && !source.loop)
            {
                source.PlayOneShot(source.clip, volume);
            }
            else
            {
                source.Play();
            }
        }
    }
}