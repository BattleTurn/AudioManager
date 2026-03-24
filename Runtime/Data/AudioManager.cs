using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleTurn.AudioManager.Runtime
{
    [CreateAssetMenu(fileName = "AudioManager", menuName = "BattleTurn/Audio/AudioManager")]
    public sealed class AudioManager : ScriptableObject
    {
        [SerializeField] private AudioData _sfxData;
        [SerializeField] private AudioData _musicData;

        [Foldout("DEBUG")]
        [ReadOnly]
        [SerializeField]
        private AudioMixer _audioMixer;
        [Foldout("DEBUG")]
        [ReadOnly]
        [SerializeField]
        private AudioMixerGroup _masterGroup;

        public AudioData SFXData => _sfxData;
        public AudioData MusicData => _musicData;
        public AudioMixer AudioMixer => _audioMixer;
        public AudioMixerGroup MasterGroup => _masterGroup;

    }
}