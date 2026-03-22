using BattleTurn.AudioManager.Runtime;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioManager", menuName = "BattleTurn/Audio/AudioManager")]
public sealed class AudioManager : ScriptableObject
{
    [SerializeField] private AudioData sfxData;
    [SerializeField] private AudioData musicData;
    [SerializeField] private AudioMixer audioMixer;
}