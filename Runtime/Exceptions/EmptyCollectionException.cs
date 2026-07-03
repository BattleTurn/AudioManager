
using System;

namespace BattleTurn.AudioManagement.Runtime
{
    internal sealed class EmptyCollectionException : Exception
    {
        public EmptyCollectionException(string audioName)
            : base($"Audio clip collection for '{audioName}' is empty.")
        {
        }
    }
}