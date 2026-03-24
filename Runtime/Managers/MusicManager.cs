
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace BattleTurn.AudioManager.Runtime
{
    public sealed class MusicManager : MonoBehaviour
    {
        private const string VOLUME_KEY = "Music.Volume";

        private static readonly FloatReactiveProperty _volume = new FloatReactiveProperty(1f);
        private static MusicManager _instance;

        [SerializeField] private byte _prewarmCount = 2;
        [SerializeField] private AudioManager _audioManager;

        private readonly Queue<AudioSource> _pool = new();
        private readonly HashSet<AudioSource> _active = new();
        private readonly ListPool<AudioSource> _audioSourceListPool = new();
        private readonly Dictionary<AudioSource, int> _sourceVersion = new();
        private readonly Dictionary<AudioSource, float> _sourceVolumeFactor = new();

        private AudioData _musicData;
        private bool _isDefault;

        private readonly ListPool<string> _listPool = new();
        private readonly Dictionary<string, float> _originalMixerValues = new();

        private bool _volumeInitialized;

        public static float Volume
        {
            get => _volume.Value;
            set => SetVolume(value);
        }

        public static IReadOnlyReactiveProperty<float> VolumeRx => _volume;

        private static void SetVolume(float value)
        {
            var clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(_volume.Value, clamped))
                return;

            _volume.Value = clamped;
            PlayerPrefs.SetFloat(VOLUME_KEY, clamped);
        }

        public static MusicManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<MusicManager>();
                if (_instance != null)
                    return _instance;

                var go = new GameObject(nameof(MusicManager));
                _instance = go.AddComponent<MusicManager>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        public static void StopInstance()
        {
            if (_instance == null)
                return;

            _instance.StopAll();
        }

        private void Awake()
        {
            _musicData = _audioManager?.MusicData;

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            SetupVolume();

            CacheOriginalMixerValues();
            _isDefault = true;
            if (_prewarmCount > 0)
                Prewarm(_prewarmCount);
        }

        private float GetSourceVolumeFactor(AudioSource src)
        {
            return _sourceVolumeFactor.TryGetValue(src, out var factor) ? factor : 1f;
        }

        private void SetSourceVolumeFactor(AudioSource src, float factor)
        {
            factor = Mathf.Clamp01(factor);
            _sourceVolumeFactor[src] = factor;
            src.volume = _volume.Value * factor;
        }

        private void SetupVolume()
        {
            if (!_volumeInitialized)
            {
                _volumeInitialized = true;
                _volume.Value = Mathf.Clamp01(PlayerPrefs.GetFloat(VOLUME_KEY, 1f));
            }

            _volume
                .DistinctUntilChanged()
                .Subscribe(ApplyVolumeToActive)
                .AddTo(this);
        }

        private void ApplyVolumeToActive(float value)
        {
            if (_active.Count == 0)
                return;

            var tmp = RentActiveSnapshot();
            foreach (var src in tmp)
            {
                if (src != null)
                    src.volume = value * GetSourceVolumeFactor(src);
            }
            ReturnActiveSnapshot(tmp);
        }

        private List<AudioSource> RentActiveSnapshot()
        {
            var tmp = _audioSourceListPool.Get();
            tmp.AddRange(_active);
            return tmp;
        }

        private void ReturnActiveSnapshot(List<AudioSource> tmp)
        {
            _audioSourceListPool.Release(tmp);
        }

        public void Prewarm(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Release(CreateNewSource());
            }
        }

        private void CacheOriginalMixerValues()
        {
            _originalMixerValues.Clear();

            var mixer = _musicData?.MixerGroup?.audioMixer;
            if (mixer == null)
                return;

            var parameterNames = _listPool.Get();
            try
            {
                FillAllExposedParameterNames(parameterNames);
                foreach (var parameterName in parameterNames)
                {
                    if (string.IsNullOrWhiteSpace(parameterName))
                        continue;

                    if (mixer.GetFloat(parameterName, out var value))
                        _originalMixerValues[parameterName] = value;
                }
            }
            finally
            {
                _listPool.Release(parameterNames);
            }
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

        private static System.Type ResolveType(string fullTypeName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullTypeName, throwOnError: false);
                if (t != null)
                    return t;
            }

            return null;
        }

        public AudioSource Play<T>(T audioName, params AudioMixParameter[] mixParameters) where T : System.Enum
        {
            return Play(audioName, loopCount: 0, mixParameters);
        }

        public AudioSource Play<T>(T audioName, IEnumerable<AudioMixParameter> mixParameters) where T : System.Enum
        {
            return Play(audioName, loopCount: 0, mixParameters);
        }

        public AudioSource Play<T>(T audioName, sbyte loopCount, params AudioMixParameter[] mixParameters) where T : System.Enum
        {
            return Play(audioName, loopCount, (IEnumerable<AudioMixParameter>)mixParameters);
        }

        public AudioSource Play<T>(T audioName, sbyte loopCount, IEnumerable<AudioMixParameter> mixParameters) where T : System.Enum
        {
            var clip = FindClip(audioName);
            if (clip == null)
            {
                Debug.LogWarning("MusicManager: Tried to play a null AudioClip.");
                return null;
            }

            // MusicManager plays a single music track at a time.
            StopAll();

            var src = Get();
            ConfigureSource(src, clip, loopCount, mixParameters);
            src.Play();
            StartReleaseRoutine(src, loopCount);
            return src;
        }

        public AudioSource PlayOneShot<T>(T audioName, params AudioMixParameter[] mixParameters) where T : System.Enum
        {
            return PlayOneShot(audioName, (IEnumerable<AudioMixParameter>)mixParameters);
        }

        public AudioSource PlayOneShot<T>(T audioName, IEnumerable<AudioMixParameter> mixParameters) where T : System.Enum
        {
            var clip = FindClip(audioName);
            if (clip == null)
            {
                Debug.LogWarning("MusicManager: Tried to play a null AudioClip.");
                return null;
            }

            var src = Get();
            ConfigureSource(src, clip, loopCount: 0, mixParameters);
            src.PlayOneShot(clip);
            StartReleaseRoutine(src, loopCount: 0);
            return src;
        }

        public AudioSource PlayLoop<T>(T audioName, params AudioMixParameter[] mixParameters) where T : System.Enum
        {
            return Play(audioName, loopCount: -1, mixParameters);
        }

        public AudioSource PlayLoop<T>(T audioName, IEnumerable<AudioMixParameter> mixParameters) where T : System.Enum
        {
            return Play(audioName, loopCount: -1, mixParameters);
        }

        private AudioClip FindClip<T>(T audioName) where T : System.Enum
        {
            if (_musicData == null)
                return null;

            return _musicData[audioName.ToString()];
        }

        public void Stop(AudioSource source)
        {
            if (source == null)
                return;

            if (_active.Contains(source))
                Release(source);
        }

        public void StopAll()
        {
            ReleaseAllActive();
        }

        private void ReleaseAllActive()
        {
            if (_active.Count == 0)
                return;

            var tmp = RentActiveSnapshot();
            foreach (var src in tmp)
                Release(src);
            ReturnActiveSnapshot(tmp);
        }

        private AudioSource Get()
        {
            while (_pool.Count > 0)
            {
                var src = _pool.Dequeue();
                if (src == null)
                    continue;

                return ActivateAndTrack(src);
            }

            var created = CreateNewSource();
            return ActivateAndTrack(created);
        }

        private AudioSource ActivateAndTrack(AudioSource src)
        {
            src.gameObject.SetActive(true);
            _active.Add(src);
            MarkSourceUsed(src);
            return src;
        }

        private int MarkSourceUsed(AudioSource src)
        {
            _sourceVersion.TryGetValue(src, out var version);
            version++;
            _sourceVersion[src] = version;
            return version;
        }

        private bool IsValid(AudioSource src, int version)
        {
            return src != null
                   && _active.Contains(src)
                   && _sourceVersion.TryGetValue(src, out var v)
                   && v == version;
        }

        private void Release(AudioSource src)
        {
            if (src == null)
                return;

            // Invalidate any pending async routines for this source.
            MarkSourceUsed(src);


            _active.Remove(src);
            ResetAndDeactivate(src);

            _pool.Enqueue(src);
        }

        private void ResetAndDeactivate(AudioSource src)
        {
            src.Stop();
            src.clip = null;
            src.loop = false;
            src.pitch = 1f;
            _sourceVolumeFactor.Remove(src);
            src.volume = _volume.Value;
            SetParentAndResetLocal(src.transform, transform);
            src.gameObject.SetActive(false);
        }

        private static void SetParentAndResetLocal(Transform child, Transform parent)
        {
            child.SetParent(parent, worldPositionStays: false);
            child.localPosition = Vector3.zero;
        }

        private void ConfigureSource(AudioSource src, AudioClip clip, sbyte loopCount, IEnumerable<AudioMixParameter> mixParameters)
        {
            if (src == null)
                return;

            src.clip = clip;
            src.pitch = 1f;
            SetSourceVolumeFactor(src, 1f);
            src.loop = loopCount < 0;
            src.playOnAwake = false;

            if (_musicData?.MixerGroup != null)
                src.outputAudioMixerGroup = _musicData.MixerGroup;

            HandleMixerParameters(mixParameters);
        }

        public void Stop(float fadeDuration)
        {
            if (fadeDuration <= 0f)
            {
                StopAll();
                return;
            }

            if (_active.Count == 0)
                return;

            var tmp = RentActiveSnapshot();
            foreach (var src in tmp)
            {
                if (src == null)
                    continue;

                // Invalidate auto-release routines then fade.
                var version = MarkSourceUsed(src);
                FadeOutAndStopAsync(src, version, fadeDuration).Forget();
            }
            ReturnActiveSnapshot(tmp);
        }

        private async UniTask FadeOutAndStopAsync(AudioSource src, int version, float fadeDuration)
        {
            fadeDuration = Mathf.Max(0.0001f, fadeDuration);

            var elapsed = 0f;
            var startFactor = GetSourceVolumeFactor(src);

            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(src, version) && elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / fadeDuration);
                SetSourceVolumeFactor(src, Mathf.Lerp(startFactor, 0f, t));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!IsValid(src, version))
                return;

            SetSourceVolumeFactor(src, 0f);
            Release(src);
        }

        private void HandleMixerParameters(IEnumerable<AudioMixParameter> mixParameters)
        {
            var mixer = _musicData?.MixerGroup?.audioMixer;
            if (mixer == null)
                return;

            var hasAny = false;
            if (mixParameters != null)
            {
                if (mixParameters is ICollection<AudioMixParameter> collection && collection.Count == 0)
                {
                    hasAny = false;
                }
                else
                {
                    foreach (var param in mixParameters)
                    {
                        hasAny = true;
                        mixer.SetFloat(param.Name, param.Value);
                    }
                }
            }

            if (hasAny)
            {
                _isDefault = false;
                return;
            }

            if (!_isDefault)
            {
                foreach (var kvp in _originalMixerValues)
                    mixer.SetFloat(kvp.Key, kvp.Value);

                _isDefault = true;
            }
        }

        private void StartReleaseRoutine(AudioSource src, sbyte loopCount)
        {
            if (src == null)
                return;

            if (loopCount < 0)
                return; // infinite loop, manual StopAll() required.

            var version = MarkSourceUsed(src);
            if (loopCount > 0)
            {
                // loopCount is the number of extra loops after the first play.
                ReleaseAfterLoopCountAsync(src, version, loopCount).Forget();
                return;
            }

            ReleaseWhenFinishedAsync(src, version).Forget();
        }

        private async UniTask ReleaseAfterLoopCountAsync(AudioSource src, int version, int loopCount)
        {
            // loopCount is the number of extra loops after the first play.
            var remainingLoops = Mathf.Max(0, loopCount);

            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(src, version))
            {
                while (IsValid(src, version) && src.isPlaying)
                    await UniTask.Yield(PlayerLoopTiming.Update);

                if (!IsValid(src, version))
                    return;

                if (remainingLoops <= 0)
                    break;

                remainingLoops--;
                src.Play();
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (IsValid(src, version))
                Release(src);
        }

        private async UniTask ReleaseWhenFinishedAsync(AudioSource src, int version)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);

            while (IsValid(src, version) && src.isPlaying)
                await UniTask.Yield(PlayerLoopTiming.Update);

            if (IsValid(src, version))
                Release(src);
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
    }
}