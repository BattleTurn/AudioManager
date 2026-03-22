using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public sealed class AudioPlayer : MonoBehaviour
{
    private AudioSource _source;
    private AudioMixerGroup _mixerGroup;

    public AudioSource Source
    {
        get
        {
            if (_source == null)
                _source = GetComponent<AudioSource>();
            return _source;
        }
    }

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        if (_source.outputAudioMixerGroup != null)
            _mixerGroup = _source.outputAudioMixerGroup;
    }

    [Button(nameof(Play))]
    private void Play()
    {
        _source.Play();
    }

    [Button(nameof(PlayOneShot))]
    private void PlayOneShot()
    {
        _mixerGroup.audioMixer.SetFloat("SFX Lowpass Frequency", Random.Range(10, 22000f));
        _source.PlayOneShot(_source.clip);
    }
}