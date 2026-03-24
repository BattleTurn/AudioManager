using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;

namespace BattleTurn.AudioManager.Runtime
{
	public sealed class SoundManager : MonoBehaviour
	{
		private const string VOLUME_KEY = "SFX.Volume";
		private static readonly FloatReactiveProperty _volume = new FloatReactiveProperty(1f);
		private static SoundManager _instance;

		[SerializeField] private byte _prewarmCount = 8;

		[Expandable]
		[SerializeField] private AudioManager _audioManager;

		private readonly ListPool<AudioSource> _listPool = new();
		private readonly Queue<AudioSource> _pool = new();
		private readonly HashSet<AudioSource> _active = new();
		private readonly Dictionary<string, float> _originalMixerValues = new();
		private readonly Dictionary<AudioSource, int> _sourceVersion = new();
		private readonly Dictionary<AudioSource, float> _sourceVolumeFactor = new();

		private AudioData _sfxData;
		private bool _isDefault;
		private bool _volumeInitialized;

		#region Properties
		public static float Volume
		{
			get => _volume.Value;
			set => SetVolume(value);
		}

		public static IReadOnlyReactiveProperty<float> VolumeRx => _volume;

		public static SoundManager Instance
		{
			get
			{
				if (_instance != null)
					return _instance;

				_instance = FindFirstObjectByType<SoundManager>();
				if (_instance != null)
					return _instance;

				var go = new GameObject(nameof(SoundManager));
				_instance = go.AddComponent<SoundManager>();
				DontDestroyOnLoad(go);
				return _instance;
			}
		}

		#endregion

		#region Unity Callbacks
		private void Awake()
		{
			_sfxData = _audioManager?.SFXData;
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

		#endregion

		public static void StopInstance()
		{
			if (_instance == null)
				return;

			_instance.StopAll();
		}

		private static void SetVolume(float value)
		{
			var clamped = Mathf.Clamp01(value);
			if (Mathf.Approximately(_volume.Value, clamped))
				return;

			_volume.Value = clamped;
			PlayerPrefs.SetFloat(VOLUME_KEY, clamped);
		}

		private static void SetParentAndResetLocal(Transform child, Transform parent)
		{
			child.SetParent(parent, worldPositionStays: false);
			child.localPosition = Vector3.zero;
		}

		private void SetupVolume()
		{
			if (!_volumeInitialized)
			{
				_volumeInitialized = true;
				// 0..1 float, default 1.
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

		private List<AudioSource> RentActiveSnapshot()
		{
			var tmp = _listPool.Get();
			tmp.AddRange(_active);
			return tmp;
		}

		private void ReturnActiveSnapshot(List<AudioSource> tmp)
		{
			_listPool.Release(tmp);
		}

		private void CacheOriginalMixerValues()
		{
			_originalMixerValues.Clear();

			var mixer = _sfxData?.MixerGroup?.audioMixer;
			if (mixer == null)
				return;

			foreach (var parameterName in GetAllExposedParameterNames())
			{
				if (string.IsNullOrWhiteSpace(parameterName))
					continue;

				if (mixer.GetFloat(parameterName, out var value))
					_originalMixerValues[parameterName] = value;
			}
		}

		private static IEnumerable<string> GetAllExposedParameterNames()
		{
			const string fullTypeName = "BattleTurn.AudioManager.Runtime.AudioMixerExposedParameter";
			var type = ResolveType(fullTypeName);
			if (type == null)
				yield break;

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

			foreach (var field in fields)
			{
				if (field.FieldType != typeof(string))
					continue;

				if (!field.IsLiteral || field.IsInitOnly)
					continue;

				if (field.GetRawConstantValue() is string value)
					yield return value;
			}
		}

		private static Type ResolveType(string fullTypeName)
		{
			// Package assemblies cannot directly reference types compiled under Assets.
			// Resolve at runtime by scanning loaded assemblies.
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				var t = asm.GetType(fullTypeName, throwOnError: false);
				if (t != null)
					return t;
			}

			return null;
		}

		#region Public API
		public void Prewarm(int count)
		{
			for (var i = 0; i < count; i++)
			{
				Release(CreateNewSource());
			}
		}

		public AudioSource Play<T>(T audioName, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return Play(audioName, loopCount: 0, mixParameters);
		}

		public AudioSource Play<T>(T audioName, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			return Play(audioName, loopCount: 0, mixParameters);
		}

		public AudioSource Play<T>(T audioName, sbyte loopCount, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return Play(audioName, loopCount, (IEnumerable<AudioMixParameter>)mixParameters);
		}

		public AudioSource Play<T>(T audioName, sbyte loopCount, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			var clip = FindClip(audioName);

			AudioSource src = Get();
			ConfigureSource(src, clip, loopCount, null, mixParameters);
			src.Play();

			StartReleaseRoutine(src, loopCount);

			return src;
		}

		public AudioSource PlayOneShot<T>(T audioName, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return PlayOneShot(audioName, (IEnumerable<AudioMixParameter>)mixParameters);
		}

		public AudioSource PlayOneShot<T>(T audioName, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			var clip = FindClip(audioName);

			AudioSource src = Get();
			// OneShot is always a single play.
			ConfigureSource(src, clip, loopCount: 0, follow: null, mixParameters);
			src.PlayOneShot(clip);
			StartReleaseRoutine(src, loopCount: 0);
			return src;
		}

		public AudioSource PlayLoop<T>(T audioName, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return Play(audioName, loopCount: -1, mixParameters);
		}

		public AudioSource PlayLoop<T>(T audioName, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			return Play(audioName, loopCount: -1, mixParameters);
		}

		public AudioSource PlayAt<T>(T audioName, Vector3 position, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return PlayAt(audioName, position, loopCount: 0, mixParameters);
		}

		public AudioSource PlayAt<T>(T audioName, Vector3 position, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			return PlayAt(audioName, position, loopCount: 0, mixParameters);
		}

		public AudioSource PlayAt<T>(T audioName, Vector3 position, sbyte loopCount, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return PlayAt(audioName, position, loopCount, (IEnumerable<AudioMixParameter>)mixParameters);
		}

		public AudioSource PlayAt<T>(T audioName, Vector3 position, sbyte loopCount, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			var clip = FindClip(audioName);

			AudioSource src = Get();
			src.transform.position = position;
			ConfigureSource(src, clip, loopCount, null, mixParameters);
			src.Play();

			StartReleaseRoutine(src, loopCount);

			return src;
		}

		public AudioSource PlayFollow<T>(T audioName, Transform follow, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return PlayFollow(audioName, follow, loopCount: 0, mixParameters);
		}

		public AudioSource PlayFollow<T>(T audioName, Transform follow, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			return PlayFollow(audioName, follow, loopCount: 0, mixParameters);
		}

		public AudioSource PlayFollow<T>(T audioName, Transform follow, sbyte loopCount, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return PlayFollow(audioName, follow, loopCount, (IEnumerable<AudioMixParameter>)mixParameters);
		}

		public AudioSource PlayFollow<T>(T audioName, Transform follow, sbyte loopCount, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			var clip = FindClip(audioName);

			AudioSource src = Get();
			ConfigureSource(src, clip, loopCount, follow, mixParameters);
			src.Play();

			StartReleaseRoutine(src, loopCount);

			return src;
		}

		public AudioSource PlayLoopFollow<T>(T audioName, Transform follow, params AudioMixParameter[] mixParameters) where T : Enum
		{
			return PlayFollow(audioName, follow, loopCount: -1, mixParameters);
		}

		public AudioSource PlayLoopFollow<T>(T audioName, Transform follow, IEnumerable<AudioMixParameter> mixParameters) where T : Enum
		{
			return PlayFollow(audioName, follow, loopCount: -1, mixParameters);
		}

		public void Stop(AudioSource source)
		{
			if (source == null)
				return;

			if (_active.Contains(source))
				Release(source);
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

		public void StopAll()
		{
			ReleaseAllActive();
		}

		#endregion

		private AudioClip FindClip<T>(T audioName) where T : Enum
		{
			if (_sfxData == null)
				throw new NullReferenceException("SoundManager: AudioData is not assigned.");

			return _sfxData[audioName.ToString()];
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
			_sourceVolumeFactor[src] = 1f;
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

		private void ConfigureSource(AudioSource src, AudioClip clip, sbyte loopCount, Transform follow, IEnumerable<AudioMixParameter> mixParameters)
		{
			if (src == null)
				return;

			src.clip = clip;
			src.pitch = 1f;
			SetSourceVolumeFactor(src, 1f);
			src.loop = loopCount < 0;
			src.playOnAwake = false;

			if (_sfxData?.MixerGroup != null)
				src.outputAudioMixerGroup = _sfxData.MixerGroup;

			if (follow != null)
				SetParentAndResetLocal(src.transform, follow);
			else
				SetParentAndResetLocal(src.transform, transform);

			HandleMixerParameters(mixParameters);
		}

		private void HandleMixerParameters(IEnumerable<AudioMixParameter> mixParameters)
		{
			var mixer = _sfxData?.MixerGroup?.audioMixer;
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
				return; // infinite loop, manual Stop() required.

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
			var go = new GameObject("PooledAudioSource");
			go.transform.SetParent(transform, worldPositionStays: false);
			go.SetActive(true);

			var src = go.AddComponent<AudioSource>();
			src.playOnAwake = false;
			return src;
		}
	}
}

