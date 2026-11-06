using System;
using System.Collections.Generic;

namespace Brainless.ScpProxChat
{
    // Independent from Unity so input transitions and recipient decisions can be exercised headlessly.
    internal sealed class VoiceState
    {
        internal bool Local { get; private set; }
        internal bool HearNearby { get; set; } = true;

        internal readonly HashSet<string> MutedUsers = new HashSet<string>(StringComparer.Ordinal);
        private bool pressed;
        internal bool KeyPressed(bool down)
        {
            bool toggle = down && !pressed;
            pressed = down;
            return toggle;
        }

        internal long RouteChecks { get; set; }
        internal long AudioSends { get; set; }

        internal void Select(bool local) => Local = local;
        internal void ResetInput()
        {
            Local = false;
            pressed = false;
            RouteChecks = AudioSends = 0;
        }

        internal bool CanHear(string speakerUserId, bool alive, bool self, bool nativeAllowed, bool groupMuted, float squaredDistance, float range) => HearNearby && !MutedUsers.Contains(speakerUserId ?? "") && alive && !self && nativeAllowed && !groupMuted && !float.IsNaN(squaredDistance) && !float.IsInfinity(squaredDistance) && squaredDistance >= 0 && squaredDistance <= range * range;
    }
}
