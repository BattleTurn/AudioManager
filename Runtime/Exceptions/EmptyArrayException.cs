
using System;

namespace BattleTurn.AudioManagement.Runtime
{
    internal sealed class EmptyArrayException : Exception
    {
        public EmptyArrayException(string audioName)
            : base($"Audio clip array for '{audioName}' is empty.")
        {
        }
    }
}