using System;
using Brainless.ScpProximityChat;

internal static class VoiceTests
{
    internal static void Run(Action<bool, string> check)
    {
        var state = new VoiceState(false);
        check(!state.Local, "SCP voice begins on ordinary team chat");
        state.Input(true);
        state.Input(true);
        check(state.Local, "one key press selects local voice without repeat-report toggling");
        state.Input(false);
        check(state.Local, "toggle mode keeps local voice after key release");
        state.Input(true);
        check(!state.Local, "the next press returns to team chat");
        state.SetMode(true);
        state.Input(true);
        check(state.Local, "hold mode selects local while held");
        state.Input(false);
        check(!state.Local, "releasing hold immediately restores team chat");
        state.Select(true);
        state.SetMode(false);
        check(!state.Local, "a mode change clears any active local route");
        state.Input(true);
        state.ResetInput();
        check(!state.Local, "a role or round reset cannot leave local voice active");
        bool Hear(bool alive = true, bool self = false, bool nativeAllowed = true, bool groupMuted = false, float d2 = 9) => state.CanHear("speaker@steam", alive, self, nativeAllowed, groupMuted, d2, 16);
        check(Hear(), "a nearby living native-approved listener can hear local SCP voice");
        check(!Hear(alive: false), "spectators cannot hear the local channel");
        check(!Hear(self: true), "local voice never echoes back to its speaker");
        check(!Hear(nativeAllowed: false), "native recipient restrictions cannot be bypassed by a radius check");
        check(!Hear(groupMuted: true), "the native alive-player group mute blocks local voice");
        check(Hear(d2: 256) && !Hear(d2: 257), "the maximum hearing distance has a strict boundary");
        check(!Hear(d2: float.NaN) && !Hear(d2: float.PositiveInfinity) && !Hear(d2: -1), "invalid positions cannot make a voice packet audible");
        state.MutedUsers.Add("speaker@steam");
        check(!Hear(), "individual nearby mutes block the selected user's stream");
        check(state.CanHear("another@steam", true, false, true, false, 9, 16), "muting one speaker does not mute another SCP");
        state.ResetInput();
        check(!Hear(), "individual listener mutes survive a role or round input reset");
        state.MutedUsers.Remove("speaker@steam");
        state.HearNearby = false;
        check(!Hear(), "the receive switch mutes all nearby SCP streams");
        state.HearNearby = true;
        check(Hear(), "unmuting reception restores an individually unmuted speaker");
    }
}
