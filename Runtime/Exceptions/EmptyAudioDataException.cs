
using System;

namespace BattleTurn.AudioManager.Runtime
{
    internal sealed class EmptyAudioDataException : Exception
    {
        public EmptyAudioDataException(string audioName)
            : base($"Audio data for '{audioName}' is empty.")
        {
        }
    }
}