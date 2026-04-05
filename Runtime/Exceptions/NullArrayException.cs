
using System;

namespace BattleTurn.AudioManager.Runtime
{
    internal sealed class NullArrayException : Exception
    {
        public NullArrayException(string audioName)
            : base($"Audio clip array for '{audioName}' is null.")
        {
        }
    }
}