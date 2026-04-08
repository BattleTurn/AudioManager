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
    public abstract class AudioManager : MonoBehaviour
    {
        private readonly FloatReactiveProperty _volume = new FloatReactiveProperty(1f);

        [SerializeField] private byte _prewarmAudioSourceAmount = 2;
        [Expandable]
        [SerializeField] protected AudioDataManagerSO audioManager;

        public abstract string Key { get; }

        protected abstract AudioAlbumBaseSO audioData { get; }

        private readonly List<string> _parameterNamesCache = new();
        private readonly Dictionary<string, Channel> _channelMap = new();
        private readonly Dictionary<string, string> _categoryByAudioName = new();
        private readonly Dictionary<string, float> _originalMixerValues = new();

        #region PROPERTIES
        public float Volume
        {
            get => _volume.Value;
            set => SetVolume(value);
        }

        public string AudioDataName => audioData?.Name;
        #endregion

        #region UNITY
        protected virtual void Awake()
        {
            SetupVolume();

            CacheOriginalMixerValues();
        }

        private void CacheOriginalMixerValues()
        {
            _originalMixerValues.Clear();
            _parameterNamesCache.Clear();

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
            _volume.Value = Mathf.Clamp01(PlayerPrefs.GetFloat(Key, 1f));

            _volume
                .DistinctUntilChanged()
                .Subscribe(ApplyVolumeToActive)
                .AddTo(this);
        }

        private void ApplyVolumeToActive(float value)
        {
            if (_channelMap.Count == 0)
                return;

            foreach (Channel channel in _channelMap.Values)
            {
                if (channel != null)
                {
                    channel.SetVolume(value);
                }
            }
        }
        #endregion

        #region PUBLIC METHODS

        public void StopAll()
        {
            foreach (Channel channel in _channelMap.Values)
            {
                channel.StopAll();
            }
        }

        public void StopAll(float fadeDuration)
        {
            if (fadeDuration <= 0f)
            {
                StopAll();
                return;
            }

            if (_channelMap.Count == 0)
                return;

            foreach (Channel channel in _channelMap.Values)
            {
                channel.StopAll(fadeDuration);
            }
        }

        public void Stop(AudioSource source)
        {
            if (source == null)
                return;

            foreach (Channel channel in _channelMap.Values)
            {
                if (channel.ActiveSources.Contains(source))
                {
                    channel.Stop(source);
                    break;
                }
            }
        }

        public void Stop(AudioSource source, float fadeDuration)
        {
            if (source == null)
                return;

            foreach (Channel channel in _channelMap.Values)
            {
                if (channel.ActiveSources.Contains(source))
                {
                    channel.Stop(source, fadeDuration);
                    break;
                }
            }
        }

        public AudioSource Play(string audioName, params AudioMixParameter[] mixParameters)
        {
            return Play(audioName, (IEnumerable<IParameterizable>)null, mixParameters);
        }

        public AudioSource Play(string audioName, IEnumerable<IParameterizable> audioParameters, params AudioMixParameter[] mixParameters)
        {
            string categoryName = ResolveCategoryName(audioName);
            return Play(categoryName, audioName, audioParameters, mixParameters);
        }

        public AudioSource Play(string categoryName, string audioName, IEnumerable<IParameterizable> audioParameters, params AudioMixParameter[] mixParameters)
        {
            return Play(categoryName, audioName, audioParameters, (IEnumerable<AudioMixParameter>)mixParameters);
        }

        public AudioSource Play<T>(string audioName, IEnumerable<IParameterizable> audioParameters, params AudioMixParameter[] mixParameters)
        {
            return Play(typeof(T).Name, audioName, audioParameters, mixParameters);
        }

        public AudioSource PlayOneShot(string audioName, params AudioMixParameter[] mixParameters)
        {
            return Play(audioName, new IParameterizable[] { new OneShotParameter() }, mixParameters);
        }

        public AudioSource PlayAt(string audioName, Vector3 position, params AudioMixParameter[] mixParameters)
        {
            return Play(audioName, new IParameterizable[] { new WorldPositionParameter(position) }, mixParameters);
        }

        public AudioSource PlayFollow(string audioName, Transform follow, params AudioMixParameter[] mixParameters)
        {
            return Play(audioName, new IParameterizable[] { new FollowParameter(follow) }, mixParameters);
        }
        #endregion

        private AudioSource Play(string categoryName, string audioName, IEnumerable<IParameterizable> audioParameters, IEnumerable<AudioMixParameter> mixParameters)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryName));

            if (string.IsNullOrWhiteSpace(audioName))
                throw new ArgumentException("Audio name cannot be null or empty.", nameof(audioName));

            HandleMixerParameters(mixParameters);

            if (!_channelMap.TryGetValue(categoryName, out Channel channel))
            {
                channel = CreateChannel(categoryName);
            }

            return channel.Play(audioName, audioParameters);
        }

        private Channel CreateChannel(string categoryName)
        {
            AudioCategoryBaseSO category = audioData[categoryName];
            Channel channel = new Channel(_prewarmAudioSourceAmount, audioData.MixerGroup, category, transform);
            _channelMap[categoryName] = channel;
            return channel;
        }

        private string ResolveCategoryName(string audioName)
        {
            if (string.IsNullOrWhiteSpace(audioName))
                throw new ArgumentException("Audio name cannot be null or empty.", nameof(audioName));

            EnsureAudioNameMap();

            if (_categoryByAudioName.TryGetValue(audioName, out string categoryName))
                return categoryName;

            throw new ArgumentOutOfRangeException(nameof(audioName), $"Audio '{audioName}' was not found in '{audioData?.Name}'.");
        }

        private void EnsureAudioNameMap()
        {
            if (_categoryByAudioName.Count > 0)
                return;

            AudioCategoryBaseSO[] categories = audioData?.AudioCategories;
            if (categories == null)
                return;

            foreach (AudioCategoryBaseSO category in categories)
            {
                if (category?.AudioClips == null)
                    continue;

                foreach (AudioContentBaseSO audioContent in category.AudioClips)
                {
                    if (audioContent == null || string.IsNullOrWhiteSpace(audioContent.Name))
                        continue;

                    if (_categoryByAudioName.TryGetValue(audioContent.Name, out string existingCategoryName))
                    {
                        if (!string.Equals(existingCategoryName, category.Name, StringComparison.Ordinal))
                        {
                            Debug.LogWarning($"Duplicate audio name '{audioContent.Name}' found in categories '{existingCategoryName}' and '{category.Name}'. Using '{existingCategoryName}'.", this);
                        }

                        continue;
                    }

                    _categoryByAudioName[audioContent.Name] = category.Name;
                }
            }
        }

        private void HandleMixerParameters(IEnumerable<AudioMixParameter> mixParameters)
        {
            AudioMixer mixer = audioData?.MixerGroup?.audioMixer;
            if (mixer == null || mixParameters == null)
                return;

            foreach (var param in mixParameters)
            {
                mixer.SetFloat(param.Name, param.Value);
            }
        }

        private void SetVolume(float value)
        {
            var clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(_volume.Value, clamped))
                return;

            _volume.Value = clamped;
            PlayerPrefs.SetFloat(Key, clamped);
        }

        private void FillAllExposedParameterNames(List<string> buffer)
        {
            const string fullTypeName = NameSpaceConstants.AUDIO_MANAGER + ".AudioMixerExposedParameter";
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

        private Type ResolveType(string fullTypeName)
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