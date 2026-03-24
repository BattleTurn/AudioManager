using System.Collections.Generic;

namespace BattleTurn.AudioManager.Runtime
{
    internal class ListPool<T>
    {
        private readonly Stack<List<T>> Pool = new();

        public List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>();
        }

        public void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}