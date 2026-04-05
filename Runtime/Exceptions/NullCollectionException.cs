
using System;

namespace BattleTurn.AudioManager.Runtime
{
    internal sealed class NullCollectionException : Exception
    {
        public NullCollectionException(string audioName)
            : base($"Audio clip collection for '{audioName}' is null.")
        {
        }
    }
}