using System;
using System.Collections.Generic;

namespace BattleTurn.AudioManager.Runtime
{
    internal class Pool<T> : IDisposable
    {
        public delegate T OnCreate();

        public event OnCreate OnCreateInstance;
        public event Action<T> OnGetInstance;
        public event Action<T> OnReleaseInstance;

        private readonly Queue<T> _pool = new();

        public Pool(OnCreate onCreate, Action<T> onRelease, Action<T> onGet)
        {
            OnCreateInstance += onCreate;
            OnReleaseInstance += onRelease;
            OnGetInstance += onGet;
        }

        public void Initialize(byte initialSize)
        {
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Enqueue(OnCreateInstance.Invoke());
            }
        }

        public void Initialize(ushort initialSize)
        {
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Enqueue(OnCreateInstance.Invoke());
            }
        }

        public void Initialize(uint initialSize)
        {
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Enqueue(OnCreateInstance.Invoke());
            }
        }

        public T Get()
        {
            if (_pool.Count > 0)
            {
                if (OnGetInstance == null)
                {
                    throw new InvalidOperationException("No method subscribed to OnGetInstance event.");
                }

                var item = _pool.Dequeue();
                OnGetInstance.Invoke(item);

                return item;
            }
            else
            {
                if (OnCreateInstance == null)
                {
                    throw new InvalidOperationException("No method subscribed to OnCreateInstance event.");
                }
                return OnCreateInstance.Invoke();
            }
        }

        public void Release(T list)
        {
            if (OnReleaseInstance == null)
            {
                throw new InvalidOperationException("No method subscribed to OnReleaseInstance event.");
            }
            OnReleaseInstance.Invoke(list);
            _pool.Enqueue(list);
        }

        public void Dispose()
        {
            _pool.Clear();
            OnCreateInstance = null;
            OnGetInstance = null;
            OnReleaseInstance = null;
        }
    }
}