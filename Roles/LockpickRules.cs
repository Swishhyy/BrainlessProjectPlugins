using System;
using System.Collections.Generic;

namespace Brainless.Roles
{
    // No game objects: the same timing/access policy is exercised by headless tests.
    internal sealed class LockpickRules
    {
        internal const float DefaultDuration = 15;
        internal const float DefaultCooldown = 120;
        private readonly Dictionary<string, float> started = new Dictionary<string, float>();
        private readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
        internal static bool IsLevelThree(int required, int containmentThree, int armoryThree) => (required & (containmentThree | armoryThree)) != 0;
        internal float Remaining(string user, float now) => readyAt.TryGetValue(user, out float time) ? Math.Max(0, time - now) : 0;
        internal float Elapsed(string user, float now) => started.TryGetValue(user, out float time) ? Math.Max(0, now - time) : 0;
        internal bool TryBegin(string user, float now)
        {
            if (started.ContainsKey(user) || Remaining(user, now) > 0)
                return false;
            started[user] = now;
            return true;
        }

        internal bool CanFinish(string user, float now, float duration) => started.ContainsKey(user) && Elapsed(user, now) >= duration;
        internal bool Succeed(string user, float now, float duration, float cooldown)
        {
            if (!CanFinish(user, now, duration))
                return false;
            started.Remove(user);
            readyAt[user] = now + cooldown;
            return true;
        }

        // Interruptions/role changes never erase an already earned cooldown.
        internal void Cancel(string user) => started.Remove(user);
        internal void Clear()
        {
            started.Clear();
            readyAt.Clear();
        }
    }
}
