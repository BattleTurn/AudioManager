using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Runtime
{
    public abstract class AudioManager<T> : MonoBehaviour where T : Enum
    {
        private static readonly string VOLUME_KEY = typeof(T).Name;

        private static readonly FloatReactiveProperty _volume = new FloatReactiveProperty(1f);

        [SerializeField] private byte _prewarmCount = 2;
        [Expandable]
        [SerializeField] protected AudioDataManagerSO audioManager;

        private Pool<AudioSource> _pool;

        protected abstract AudioDataBaseSO audioData { get; }

        private readonly List<string> _parameterNamesCache = new();
        private readonly HashSet<AudioSource> _channel = new();
        private readonly Dictionary<AudioSource, float> _audioVolumeFactor = new();
        private readonly Dictionary<string, float> _originalMixerValues = new();

        #region PROPERTIES
        public static float Volume
        {
            get => _volume.Value;
            set => SetVolume(value);
        }
        #endregion

        #region UNITY
        protected virtual void Awake()
        {
            _pool = new Pool<AudioSource>(CreateNewSource, Release, ActivateChannel);

            SetupVolume();

            CacheOriginalMixerValues();
            if (_prewarmCount > 0)
                Prewarm(_prewarmCount);
        }

        private void CacheOriginalMixerValues()
        {
            _originalMixerValues.Clear();

            AudioMixer mixer = audioData?.MixerGroup?.audioMixer;
            if (mixer == null)
                return;

            try
            {
                FillAllExposedParameterNames(_parameterNamesCache);
                foreach (var parameterName in _parameterNamesCache)
                {
                    if (string.IsNullOrWhiteSpace(parameterName))
                        continue;

                    if (mixer.GetFloat(parameterName, out var value))
                        _originalMixerValues[parameterName] = value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to cache original mixer values: {ex}");
            }
        }

        private void SetupVolume()
        {
            _volume.Value = Mathf.Clamp01(PlayerPrefs.GetFloat(VOLUME_KEY, 1f));

            _volume
                .DistinctUntilChanged()
                .Subscribe(ApplyVolumeToActive)
                .AddTo(this);
        }

        private void ApplyVolumeToActive(float value)
        {
            if (_channel.Count == 0)
                return;

            foreach (var src in _channel)
            {
                if (src != null)
                    src.volume = value * GetSourceVolumeFactor(src);
            }
        }
        #endregion

        #region PUBLIC METHODS
        public void Prewarm(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Release(CreateNewSource());
            }
        }

        public void StopAll()
        {
            ReleaseAllActive();
        }

        public void StopAllWithFade(float fadeDuration)
        {
            if (fadeDuration <= 0f)
            {
                StopAll();
                return;
            }

            if (_channel.Count == 0)
                return;

            foreach (AudioSource source in _channel)
            {
                FadeOutAndStopAsync(source, fadeDuration).Forget();
            }
        }

        public void Stop(AudioSource source)
        {
            if (source == null)
                return;

            if (_channel.Contains(source))
                _pool.Release(source);
        }

        public AudioSource Play(T audioName, params AudioMixParameter[] mixParameters)
        {
            return Play(audioName, loopCount: 0, mixParameters);
        }

        public AudioSource Play(T audioName, IEnumerable<AudioMixParameter> mixParameters)
        {
            return Play(audioName, loopCount: 0, mixParameters);
        }

        public AudioSource Play(T audioName, byte loopCount, params AudioMixParameter[] mixParameters)
        {
            return Play(audioName, loopCount, (IEnumerable<AudioMixParameter>)mixParameters);
        }

        public AudioSource Play(T audioName, short loopCount, IEnumerable<AudioMixParameter> mixParameters)
        {
            var clip = FindClip(audioName);

            // AudioManager plays a single audio track at a time.
            StopAll();

            AudioSource src = _pool.Get();
            ConfigureSource(src, clip, loopCount, follow: null, mixParameters);
            src.Play();
            StartPlayLoop(src, loopCount);
            return src;
        }

        public AudioSource PlayLoop(T audioName, params AudioMixParameter[] mixParameters)
        {
            return Play(audioName, loopCount: -1, mixParameters);
        }

        public AudioSource PlayLoop(T audioName, IEnumerable<AudioMixParameter> mixParameters)
        {
            return Play(audioName, loopCount: -1, mixParameters);
        }

        public AudioSource PlayOneShot(T audioName, params AudioMixParameter[] mixParameters)
        {
            return PlayOneShot(audioName, (IEnumerable<AudioMixParameter>)mixParameters);
        }

        public AudioSource PlayOneShot(T audioName, IEnumerable<AudioMixParameter> mixParameters)
        {
            var clip = FindClip(audioName);

            AudioSource source = _pool.Get();
            ConfigureSource(source, clip, loopCount: 0, follow: null, mixParameters);
            source.PlayOneShot(clip);
            StartPlayLoop(source, loopCount: 0);
            return source;
        }

        public AudioSource PlayAt(T audioName, Vector3 position, params AudioMixParameter[] mixParameters)
        {
            return PlayAt(audioName, position, loopCount: 0, mixParameters);
        }

        public AudioSource PlayAt(T audioName, Vector3 position, IEnumerable<AudioMixParameter> mixParameters)
        {
            return PlayAt(audioName, position, loopCount: 0, mixParameters);
        }

        public AudioSource PlayAt(T audioName, Vector3 position, byte loopCount, params AudioMixParameter[] mixParameters)
        {
            return PlayAt(audioName, position, loopCount, (IEnumerable<AudioMixParameter>)mixParameters);
        }

        private AudioSource PlayAt(T audioName, Vector3 position, short loopCount, IEnumerable<AudioMixParameter> mixParameters)
        {
            var clip = FindClip(audioName);
            StopAll();

            AudioSource source = _pool.Get();
            source.transform.position = position;
            ConfigureSource(source, clip, loopCount, follow: null, mixParameters);
            source.Play();
            StartPlayLoop(source, loopCount);
            return source;
        }

        public AudioSource PlayFollow(T audioName, Transform follow, params AudioMixParameter[] mixParameters)
        {
            return PlayFollow(audioName, follow, loopCount: 0, mixParameters);
        }

        public AudioSource PlayFollow(T audioName, Transform follow, IEnumerable<AudioMixParameter> mixParameters)
        {
            return PlayFollow(audioName, follow, loopCount: 0, mixParameters);
        }

        public AudioSource PlayFollow(T audioName, Transform follow, byte loopCount, params AudioMixParameter[] mixParameters)
        {
            return PlayFollow(audioName, follow, loopCount, (IEnumerable<AudioMixParameter>)mixParameters);
        }

        private AudioSource PlayFollow(T audioName, Transform follow, short loopCount, IEnumerable<AudioMixParameter> mixParameters)
        {
            var clip = FindClip(audioName);
            StopAll();

            AudioSource source = _pool.Get();
            ConfigureSource(source, clip, loopCount, follow, mixParameters);
            source.Play();
            StartPlayLoop(source, loopCount);
            return source;
        }

        public AudioSource PlayLoopFollow(T audioName, Transform follow, params AudioMixParameter[] mixParameters)
        {
            return PlayFollow(audioName, follow, loopCount: -1, mixParameters);
        }

        public AudioSource PlayLoopFollow(T audioName, Transform follow, IEnumerable<AudioMixParameter> mixParameters)
        {
            return PlayFollow(audioName, follow, loopCount: -1, mixParameters);
        }
        #endregion

        private async UniTask FadeOutAndStopAsync(AudioSource src, float fadeDuration)
        {
            fadeDuration = Mathf.Max(0.0001f, fadeDuration);

            var elapsed = 0f;
            var startFactor = GetSourceVolumeFactor(src);

            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(src) && elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / fadeDuration);
                SetSourceVolumeFactor(src, Mathf.Lerp(startFactor, 0f, t));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!IsValid(src))
                return;

            SetSourceVolumeFactor(src, 0f);
            _pool.Release(src);
        }

        private AudioClip FindClip(T audioName)
        {
            if (audioData == null)
                throw new NullReferenceException("AudioData is not assigned.");

            return audioData[audioName.ToString()];
        }

        private void StartPlayLoop(AudioSource source, short loopCount)
        {
            if (source == null)
                return;

            if (loopCount > 0)
            {
                // loopCount is the number of extra loops after the first play.
                ReleaseAfterLoopCountAsync(source, loopCount).Forget();
                return;
            }
            else if (loopCount < 0)
            {
                // Negative loopCount indicates infinite looping, so we don't need to track it for release.
                return;
            }

            ReleaseWhenFinishedAsync(source).Forget();
        }

        private async UniTask ReleaseAfterLoopCountAsync(AudioSource source, short loopCount)
        {
            // loopCount is the number of extra loops after the first play.
            var remainingLoops = Mathf.Max(0, loopCount);

            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(source))
            {
                while (IsValid(source) && source.isPlaying)
                    await UniTask.Yield(PlayerLoopTiming.Update);

                if (!IsValid(source))
                    return;

                if (remainingLoops <= 0)
                    break;

                remainingLoops--;
                source.Play();
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (IsValid(source))
                _pool.Release(source);
        }

        private async UniTask ReleaseWhenFinishedAsync(AudioSource source)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(source) && source.isPlaying)
                await UniTask.Yield(PlayerLoopTiming.Update);

            if (IsValid(source))
                _pool.Release(source);
        }

        private bool IsValid(AudioSource source)
        {
            return source != null
                   && _channel.Contains(source);
        }

        private void ConfigureSource(AudioSource source, AudioClip clip, short loopCount, Transform follow, IEnumerable<AudioMixParameter> mixParameters)
        {
            if (source == null)
                return;

            source.clip = clip;
            source.pitch = 1f;
            SetSourceVolumeFactor(source, 1f);
            source.loop = loopCount < 0;
            source.playOnAwake = false;

            if (audioData?.MixerGroup != null)
                source.outputAudioMixerGroup = audioData.MixerGroup;

            if (follow != null)
                ResetLocal(source.transform, follow);
            else
                ResetLocal(source.transform, transform);

            if (mixParameters.Count() <= 0)
            {
                foreach (KeyValuePair<string, float> originalParameterPair in _originalMixerValues)
                    audioData?.MixerGroup?.audioMixer?.SetFloat(originalParameterPair.Key, originalParameterPair.Value);
                return;
            }
            HandleMixerParameters(mixParameters);
        }

        private float GetSourceVolumeFactor(AudioSource src)
        {
            return _audioVolumeFactor.TryGetValue(src, out var factor) ? factor : 1f;
        }

        private void SetSourceVolumeFactor(AudioSource source, float factor)
        {
            factor = Mathf.Clamp01(factor);
            _audioVolumeFactor[source] = factor;
            source.volume = _volume.Value * factor;
        }

        private void HandleMixerParameters(IEnumerable<AudioMixParameter> mixParameters)
        {
            AudioMixer mixer = audioData?.MixerGroup?.audioMixer;
            if (mixer == null)
                return;

            foreach (var param in mixParameters)
            {
                mixer.SetFloat(param.Name, param.Value);
            }
        }

        private void ReleaseAllActive()
        {
            if (_channel.Count == 0)
                return;

            foreach (AudioSource source in _channel)
            {
                _pool.Release(source);
            }
            _channel.Clear();
        }

        private void Release(AudioSource source)
        {
            if (source == null)
                return;

            _channel.Remove(source);
            ResetChannel(source);
            ResetLocal(source.transform, transform);
            source.gameObject.SetActive(false);
        }

        private void ResetChannel(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.pitch = 1f;
            _audioVolumeFactor.Remove(source);
            source.volume = _volume.Value;
        }

        private void ActivateChannel(AudioSource source)
        {
            source.gameObject.SetActive(true);
            _channel.Add(source);
        }

        private AudioSource CreateNewSource()
        {
            var go = new GameObject("PooledMusicSource");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.SetActive(true);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        private static void ResetLocal(Transform child, Transform parent)
        {
            child.SetParent(parent, worldPositionStays: false);
            child.localPosition = Vector3.zero;
        }

        private static void SetVolume(float value)
        {
            var clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(_volume.Value, clamped))
                return;

            _volume.Value = clamped;
            PlayerPrefs.SetFloat(VOLUME_KEY, clamped);
        }

        private static void FillAllExposedParameterNames(List<string> buffer)
        {
            const string fullTypeName = "BattleTurn.AudioManager.Runtime.AudioMixerExposedParameter";
            var type = ResolveType(fullTypeName);
            if (type == null)
                return;

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (field.FieldType != typeof(string))
                    continue;

                if (!field.IsLiteral || field.IsInitOnly)
                    continue;

                if (field.GetRawConstantValue() is string value)
                    buffer.Add(value);
            }
        }

        private static Type ResolveType(string fullTypeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullTypeName, throwOnError: false);
                if (t != null)
                    return t;
            }

            return null;
        }
    }
}