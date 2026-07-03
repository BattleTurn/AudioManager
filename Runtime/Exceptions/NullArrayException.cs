
using System;

namespace BattleTurn.AudioManagement.Runtime
{
    internal sealed class NullArrayException : Exception
    {
        public NullArrayException(string audioName)
            : base($"Audio clip array for '{audioName}' is null.")
        {
        }
    }
}