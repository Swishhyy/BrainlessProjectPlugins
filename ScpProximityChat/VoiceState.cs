using System;
using System.Collections.Generic;

namespace Brainless.ScpProximityChat
{
    // Independent from Unity so input transitions and recipient decisions can be exercised headlessly.
    internal sealed class VoiceState
    {
        internal bool HoldMode { get; private set; }
        internal bool Local { get; private set; }
        internal bool HearNearby { get; set; } = true;

        internal readonly HashSet<string> MutedUsers = new HashSet<string>(StringComparer.Ordinal);
        private bool pressed;
        internal VoiceState(bool holdMode) => HoldMode = holdMode;
        internal void SetMode(bool holdMode)
        {
            HoldMode = holdMode;
            ResetInput();
        }

        internal void Input(bool down)
        {
            if (HoldMode)
                Local = down;
            else if (down && !pressed)
                Local = !Local;
            pressed = down;
        }

        internal void Select(bool local) => Local = local;
        internal void ResetInput()
        {
            Local = false;
            pressed = false;
        }

        internal bool CanHear(string speakerUserId, bool alive, bool self, bool nativeAllowed, bool groupMuted, float squaredDistance, float range) => HearNearby && !MutedUsers.Contains(speakerUserId ?? "") && alive && !self && nativeAllowed && !groupMuted && !float.IsNaN(squaredDistance) && !float.IsInfinity(squaredDistance) && squaredDistance >= 0 && squaredDistance <= range * range;
    }
}
