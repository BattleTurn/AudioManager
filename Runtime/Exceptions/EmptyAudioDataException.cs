
using System;

namespace BattleTurn.AudioManagement.Runtime
{
    internal sealed class EmptyAudioDataException : Exception
    {
        public EmptyAudioDataException(string audioName)
            : base($"Audio data for '{audioName}' is empty.")
        {
        }
    }
}