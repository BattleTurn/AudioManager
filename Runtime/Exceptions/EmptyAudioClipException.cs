
using System;

namespace BattleTurn.AudioManager.Runtime
{
    public sealed class EmptyAudioClipException : Exception
    {
        public EmptyAudioClipException(string audioName)
            : base($"Audio clip for '{audioName}' is empty.")
        {
        }
    }
}