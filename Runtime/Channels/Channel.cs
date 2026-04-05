using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Runtime
{
    [Serializable]
    /// <summary>
    /// This will act as an AudioSource factory and manager for a specific audio category. It holds a pool of AudioSources and handles playing clips from its assigned category with optional mix parameters.
    /// </summary>
    internal sealed class Channel
    {
        [SerializeField] private string _name;
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 1f;
        [Range(0.1f, 3f)]
        [SerializeField] private float _pitch = 1f;
        [SerializeField] private bool _mute = false;

        [SerializeField] private AudioMixerGroup _mixerGroup;

        private readonly Transform _transform;
        private readonly IAudioCategory _category;
        private readonly Pool<AudioSource> _pool;
        private readonly HashSet<AudioSource> _activeSources = new();
        private readonly Dictionary<AudioSource, uint> _playbackTokens = new();
        private readonly Dictionary<AudioSource, short> _loopingSources = new();
        private readonly Dictionary<AudioSource, OneShotParameter> _oneShotSources = new();
        private readonly Dictionary<AudioSource, DelayParameter> _delayedSources = new();

        public string Name => _name;
        public IAudioCategory Category => _category;
        public IReadOnlyCollection<AudioSource> ActiveSources => _activeSources;

        public Channel(byte prewarm, AudioMixerGroup mixerGroup, IAudioCategory category, Transform root)
        {
            _name = category.Name;
            _mixerGroup = mixerGroup;
            _category = category;
            _pool = new Pool<AudioSource>(CreateNewSource, Release, ActivateChannel);
            _transform = root;
            _pool.Initialize(prewarm);
        }

        public Channel SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            foreach (var source in _activeSources)
            {
                if (source != null)
                    source.volume = _volume;
            }
            return this;
        }

        public Channel SetMute(bool isMuted)
        {
            _mute = isMuted;
            foreach (var source in _activeSources)
            {
                if (source != null)
                    source.mute = _mute;
            }
            return this;
        }

        public void Stop(AudioSource source)
        {
            if (source == null)
                throw new NullReferenceException("AudioSource pool returned null");

            ReleaseAudioSource(source);
        }

        public void Stop(AudioSource source, float fadeDuration)
        {
            if (source == null)
                throw new NullReferenceException("AudioSource pool returned null");

            if (fadeDuration <= 0f)
            {
                ReleaseAudioSource(source);
            }
            else
            {
                if (TryGetPlaybackToken(source, out uint playbackToken))
                {
                    FadeOutAndStopAsync(source, fadeDuration, playbackToken).Forget();
                }
            }
        }

        public void StopAll()
        {
            ReleaseAllActive();
        }

        public void StopAll(float fadeDuration)
        {
            if (fadeDuration <= 0f)
            {
                StopAll();
                return;
            }

            if (_activeSources.Count == 0)
                return;

            List<AudioSource> activeSources = new(_activeSources);
            foreach (AudioSource source in activeSources)
            {
                if (TryGetPlaybackToken(source, out uint playbackToken))
                {
                    FadeOutAndStopAsync(source, fadeDuration, playbackToken).Forget();
                }
            }
        }

        public AudioSource Play(string audioName, params IParameterizable[] audioParameters)
        {
            return PlayAudio(audioName, audioParameters);
        }

        public AudioSource Play(string audioName, IEnumerable<IParameterizable> audioParameters)
        {
            return PlayAudio(audioName, audioParameters);
        }

        private void Release(AudioSource source)
        {
            if (source == null)
                return;

            _activeSources.Remove(source);
            ResetChannel(source);
            ResetLocal(source.transform, _transform);
            source.gameObject.SetActive(false);
        }

        private void ResetChannel(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.pitch = 1f;
            source.volume = _volume;
            source.mute = _mute;
        }

        private void ResetLocal(Transform child, Transform parent)
        {
            child.SetParent(parent, worldPositionStays: false);
            child.localPosition = Vector3.zero;
        }

        private void ActivateChannel(AudioSource source)
        {
            source.gameObject.SetActive(true);
            _activeSources.Add(source);
        }

        private AudioSource CreateNewSource()
        {
            var go = new GameObject("PooledMusicSource");
            go.transform.SetParent(_transform, worldPositionStays: false);
            go.SetActive(true);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        private AudioSource PlayAudio(string audioName, IEnumerable<IParameterizable> audioParameters)
        {
            AudioClip audioClip = GetAudioClip(audioName);
            AudioSource source = GetSource(audioParameters, out uint playbackToken);
            source.clip = audioClip;

            bool hasDelay = _delayedSources.TryGetValue(source, out DelayParameter delayParameter);
            bool hasOneShot = _oneShotSources.ContainsKey(source);
            DelayParameter? activeDelay = hasDelay ? delayParameter : null;

            if (_loopingSources.TryGetValue(source, out short loopCount))
            {
                StartPlayLoop(source, loopCount, hasOneShot, activeDelay, playbackToken);
            }
            else
            {
                bool applyDelay = ShouldApplyDelay(activeDelay, isInitialPlayback: true);
                source.Play(hasOneShot, _volume, activeDelay, applyDelay);
                ReleaseWhenFinishedAsync(source, playbackToken, GetAppliedDelaySeconds(activeDelay, applyDelay)).Forget();
            }

            return source;
        }

        private AudioSource GetSource(IEnumerable<IParameterizable> audioParameters, out uint playbackToken)
        {
            var source = _pool.Get();
            if (source == null)
            {
                throw new NullReferenceException("AudioSource pool returned null");
            }

            playbackToken = NextPlaybackToken(source);
            source.playOnAwake = false;
            source.outputAudioMixerGroup = _mixerGroup;
            ApplyChannelState(source);

            if (audioParameters == null)
            {
                return source;
            }

            source.SetParameters(ConfigAudioSource, audioParameters);
            return source;
        }

        private void ApplyChannelState(AudioSource source)
        {
            source.volume = _volume;
            source.pitch = _pitch;
            source.mute = _mute;
        }

        private void ConfigAudioSource(AudioSource source, IParameterizable audioParameter)
        {
            switch (audioParameter)
            {
                case LoopParameter loopParam:
                    if (_loopingSources.ContainsKey(source))
                    {
                        _loopingSources[source] = loopParam.Value;
                    }
                    else
                    {
                        _loopingSources.Add(source, loopParam.Value);
                    }
                    break;
                case DelayParameter delayParam:
                    if (_delayedSources.ContainsKey(source))
                    {
                        _delayedSources[source] = delayParam;
                    }
                    else
                    {
                        _delayedSources.Add(source, delayParam);
                    }
                    break;
                case OneShotParameter oneShotParam:
                    if (_oneShotSources.ContainsKey(source))
                    {
                        _oneShotSources[source] = oneShotParam;
                    }
                    else
                    {
                        _oneShotSources.Add(source, oneShotParam);
                    }
                    break;
            }
        }

        private AudioClip GetAudioClip(string audioName)
        {
            if (_category.TryGetClip(audioName, out var clip))
            {
                return clip;
            }
            else
            {
                throw new ArgumentException($"Audio clip '{audioName}' not found in category '{_category.Name}'");
            }
        }

        private void ReleaseAllActive()
        {
            if (_activeSources.Count == 0)
                return;

            List<AudioSource> activeSources = new(_activeSources);
            foreach (AudioSource source in activeSources)
            {
                ReleaseAudioSource(source);
            }
        }

        private void StartPlayLoop(AudioSource source, short loopCount, bool hasOneShot, DelayParameter? delayParameter, uint playbackToken)
        {
            if (source == null)
                return;

            bool shouldUseManagedLoop = loopCount >= 0
                                        || hasOneShot
                                        || HasEveryLoopDelay(delayParameter);

            source.loop = loopCount < 0 && !shouldUseManagedLoop;

            bool applyInitialDelay = ShouldApplyDelay(delayParameter, isInitialPlayback: true);
            float initialDelaySeconds = GetAppliedDelaySeconds(delayParameter, applyInitialDelay);

            source.Play(hasOneShot, _volume, delayParameter, applyInitialDelay);

            if (source.loop)
            {
                return;
            }

            if (loopCount > 0)
            {
                ReleaseAfterLoopCountAsync(source, loopCount, hasOneShot, delayParameter, playbackToken, initialDelaySeconds).Forget();
            }
            else if (loopCount == 0)
            {
                ReleaseWhenFinishedAsync(source, playbackToken, initialDelaySeconds).Forget();
            }
            else
            {
                LoopUntilStoppedAsync(source, hasOneShot, delayParameter, playbackToken, initialDelaySeconds).Forget();
            }
        }

        private async UniTask FadeOutAndStopAsync(AudioSource source, float fadeDuration, uint playbackToken)
        {
            fadeDuration = Mathf.Max(0.0001f, fadeDuration);

            float elapsed = 0f;
            float startVolume = source.volume;

            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(source, playbackToken) && elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!IsValid(source, playbackToken))
                return;

            source.volume = 0f;
            ReleaseAudioSource(source);
        }

        private async UniTask ReleaseAfterLoopCountAsync(AudioSource source, short loopCount, bool hasOneShot, DelayParameter? delayParameter, uint playbackToken, float initialDelaySeconds)
        {
            short remainingLoops = Math.Max((short)0, loopCount);

            await AwaitPlaybackCompletionAsync(source, playbackToken, initialDelaySeconds);

            while (IsValid(source, playbackToken) && remainingLoops > 0)
            {
                bool applyReplayDelay = ShouldApplyDelay(delayParameter, isInitialPlayback: false);
                float replayDelaySeconds = GetAppliedDelaySeconds(delayParameter, applyReplayDelay);
                source.Play(hasOneShot, _volume, delayParameter, applyReplayDelay);
                remainingLoops--;

                await AwaitPlaybackCompletionAsync(source, playbackToken, replayDelaySeconds);
            }

            ReleaseAudioSource(source);
        }

        private async UniTask LoopUntilStoppedAsync(AudioSource source, bool hasOneShot, DelayParameter? delayParameter, uint playbackToken, float initialDelaySeconds)
        {
            await AwaitPlaybackCompletionAsync(source, playbackToken, initialDelaySeconds);

            while (IsValid(source, playbackToken))
            {
                bool applyReplayDelay = ShouldApplyDelay(delayParameter, isInitialPlayback: false);
                float replayDelaySeconds = GetAppliedDelaySeconds(delayParameter, applyReplayDelay);
                source.Play(hasOneShot, _volume, delayParameter, applyReplayDelay);

                await AwaitPlaybackCompletionAsync(source, playbackToken, replayDelaySeconds);
            }
        }

        private async UniTask ReleaseWhenFinishedAsync(AudioSource source, uint playbackToken, float initialDelaySeconds)
        {
            await AwaitPlaybackCompletionAsync(source, playbackToken, initialDelaySeconds);

            if (!IsValid(source, playbackToken))
                return;

            ReleaseAudioSource(source);
        }

        private async UniTask AwaitPlaybackCompletionAsync(AudioSource source, uint playbackToken, float scheduledDelaySeconds)
        {
            if (scheduledDelaySeconds > 0f)
                await UniTask.WaitForSeconds(scheduledDelaySeconds);

            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(source, playbackToken) && !source.isPlaying)
                await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(source, playbackToken) && source.isPlaying)
                await UniTask.Yield(PlayerLoopTiming.Update);
        }

        private void ReleaseAudioSource(AudioSource source)
        {
            if (IsValid(source))
            {
                _pool.Release(source);
            }

            if (_oneShotSources.ContainsKey(source))
            {
                _oneShotSources.Remove(source);
            }

            if (_loopingSources.ContainsKey(source))
            {
                _loopingSources.Remove(source);
            }

            if (_delayedSources.ContainsKey(source))
            {
                _delayedSources.Remove(source);
            }
        }

        private uint NextPlaybackToken(AudioSource source)
        {
            if (_playbackTokens.TryGetValue(source, out uint currentToken))
            {
                currentToken++;
                if (currentToken == 0)
                    currentToken = 1;

                _playbackTokens[source] = currentToken;
                return currentToken;
            }

            _playbackTokens[source] = 1;
            return 1;
        }

        private bool TryGetPlaybackToken(AudioSource source, out uint playbackToken)
        {
            return _playbackTokens.TryGetValue(source, out playbackToken)
                   && _activeSources.Contains(source);
        }

        private static bool HasEveryLoopDelay(DelayParameter? delayParameter)
        {
            return delayParameter.HasValue
                   && delayParameter.Value.DelayType == DelayType.EveryLoop
                   && delayParameter.Value.DelayTime > 0f;
        }

        private static bool ShouldApplyDelay(DelayParameter? delayParameter, bool isInitialPlayback)
        {
            if (!delayParameter.HasValue || delayParameter.Value.DelayTime <= 0f)
                return false;

            if (delayParameter.Value.DelayType == DelayType.EveryLoop)
                return true;

            return isInitialPlayback && delayParameter.Value.DelayType == DelayType.OnStart;
        }

        private static float GetAppliedDelaySeconds(DelayParameter? delayParameter, bool applyDelay)
        {
            return applyDelay && delayParameter.HasValue
                ? Mathf.Max(0f, delayParameter.Value.DelayTime)
                : 0f;
        }

        private bool IsValid(AudioSource source)
        {
            return source != null
                   && _activeSources.Contains(source);
        }

        private bool IsValid(AudioSource source, uint playbackToken)
        {
            return source != null
                   && _activeSources.Contains(source)
                   && _playbackTokens.TryGetValue(source, out uint currentToken)
                   && currentToken == playbackToken;
        }
    }
}