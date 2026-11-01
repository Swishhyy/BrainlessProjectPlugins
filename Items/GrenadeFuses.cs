using System;
using System.Collections.Generic;

namespace Brainless.Items
{
    // One payload per native projectile. Both EXILED explosion callbacks may
    // observe the same fuse; consume before dispatch, including cancelled fuses.
    internal sealed class GrenadeFuses<T>
        where T : class
    {
        private readonly Dictionary<int, T> pending = new Dictionary<int, T>();
        internal IEnumerable<T> Values => pending.Values;

        internal void Arm(int serial, T payload) => pending[serial] = payload;
        internal bool TryGet(int serial, out T payload) => pending.TryGetValue(serial, out payload);
        internal bool Resolve(int serial, bool allowed, Action<T> detonate, Action<T> retire)
        {
            if (!pending.TryGetValue(serial, out T payload))
                return false;
            pending.Remove(serial);
            try
            {
                if (allowed)
                    detonate(payload);
            }
            finally
            {
                retire(payload);
            }

            return true;
        }

        internal void Clear() => pending.Clear();
    }
}
