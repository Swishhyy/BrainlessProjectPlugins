using System;
using System.Collections.Generic;

namespace Brainless.Roles
{
    // Keep pre-death identity across the native change to Spectator. Only the
    // confirmed Died event consumes a capture; canceled deaths expire separately.
    internal sealed class DeathCapture<TPlayer, TState>
        where TState : class
    {
        private readonly Dictionary<TPlayer, TState> pending = new Dictionary<TPlayer, TState>();
        internal void Capture(TPlayer player, TState state) => pending[player] = state;
        internal void Forget(TPlayer player) => pending.Remove(player);
        internal void ForgetIfSame(TPlayer player, TState state)
        {
            if (pending.TryGetValue(player, out var current) && ReferenceEquals(current, state))
                pending.Remove(player);
        }

        internal bool Confirm(TPlayer player, Func<TState, bool> allowed, out TState state)
        {
            if (!pending.TryGetValue(player, out state))
                return false;
            pending.Remove(player);
            if (allowed(state))
                return true;
            state = null;
            return false;
        }

        internal void Clear() => pending.Clear();
    }
}
