using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleTurn.AudioManager.Runtime
{
	public sealed class SoundManager : MonoBehaviour
	{
		private static SoundManager _instance;

		[SerializeField] private int prewarmCount = 8;

		private readonly Queue<AudioSource> _pool = new();
		private readonly HashSet<AudioSource> _active = new();

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

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}

			_instance = this;
			DontDestroyOnLoad(gameObject);

			if (prewarmCount > 0)
				Prewarm(prewarmCount);
		}

		public void Prewarm(int count)
		{
			for (var i = 0; i < count; i++)
			{
				var src = CreateNewSource();
				Release(src);
			}
		}

		public AudioSource Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false, Transform follow = null)
		{
			if (clip == null)
			{
				Debug.LogWarning("SoundManager: Tried to play a null AudioClip.");
				return null;
			}

			var src = Get();
			ConfigureSource(src, clip, volume, pitch, loop, follow);
			src.Play();

			if (!loop)
				StartCoroutine(ReleaseWhenFinished(src));

			return src;
		}

		public AudioSource Play(AudioData audioData, string audioName, float volume = 1f, float pitch = 1f, bool loop = false, Transform follow = null)
		{
			if (audioData == null)
			{
				Debug.LogWarning("SoundManager: AudioData is null.");
				return null;
			}

			var clip = FindClip(audioData, audioName);
			if (clip == null)
			{
				Debug.LogWarning($"SoundManager: Audio clip '{audioName}' not found in AudioData '{audioData.name}'.");
				return null;
			}

			return Play(clip, volume, pitch, loop, follow);
		}

		private static AudioClip FindClip(AudioData audioData, string audioName)
		{
			if (audioData == null || string.IsNullOrEmpty(audioName) || audioData.audioContents == null)
				return null;

			foreach (var content in audioData.audioContents)
			{
				if (content == null)
					continue;

				if (content.name == audioName)
					return content.clip;
			}

			return null;
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
			if (_active.Count == 0)
				return;

			var tmp = ListPool<AudioSource>.Get();
			tmp.AddRange(_active);
			foreach (var src in tmp)
				Release(src);
			ListPool<AudioSource>.Release(tmp);
		}

		private AudioSource Get()
		{
			while (_pool.Count > 0)
			{
				var src = _pool.Dequeue();
				if (src == null)
					continue;

				src.gameObject.SetActive(true);
				_active.Add(src);
				return src;
			}

			var created = CreateNewSource();
			created.gameObject.SetActive(true);
			_active.Add(created);
			return created;
		}

		private void Release(AudioSource src)
		{
			if (src == null)
				return;

			_active.Remove(src);

			src.Stop();
			src.clip = null;
			src.loop = false;
			src.pitch = 1f;
			src.volume = 1f;
			src.transform.SetParent(transform, worldPositionStays: false);
			src.transform.localPosition = Vector3.zero;
			src.gameObject.SetActive(false);

			_pool.Enqueue(src);
		}

		private void ConfigureSource(AudioSource src, AudioClip clip, float volume, float pitch, bool loop, Transform follow)
		{
			src.clip = clip;
			src.volume = Mathf.Clamp01(volume);
			src.pitch = Mathf.Clamp(pitch, -3f, 3f);
			src.loop = loop;
			src.playOnAwake = false;

			if (follow != null)
			{
				src.transform.SetParent(follow, worldPositionStays: false);
				src.transform.localPosition = Vector3.zero;
			}
			else
			{
				src.transform.SetParent(transform, worldPositionStays: false);
				src.transform.localPosition = Vector3.zero;
			}
		}

		private IEnumerator ReleaseWhenFinished(AudioSource src)
		{
			// Wait at least 1 frame so isPlaying updates.
			yield return null;

			while (src != null && src.isPlaying)
				yield return null;

			// Stop()/StopAll() may have already released this source.
			if (src != null && _active.Contains(src))
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

		private static class ListPool<T>
		{
			private static readonly Stack<List<T>> Pool = new();

			public static List<T> Get()
			{
				return Pool.Count > 0 ? Pool.Pop() : new List<T>();
			}

			public static void Release(List<T> list)
			{
				list.Clear();
				Pool.Push(list);
			}
		}
	}
}

