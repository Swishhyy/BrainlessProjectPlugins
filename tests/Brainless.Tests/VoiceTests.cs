using System;
using Brainless.ScpProximityChat;

internal static class VoiceTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(VoiceAudio.UpgradeVolume(1, 0) == 10 && VoiceAudio.UpgradeVolume(3.5f, 1) == 10, "both older stock SCP speech gains upgrade even after the previous migration already ran");
        check(VoiceAudio.UpgradeVolume(0, 1) == 0 && VoiceAudio.UpgradeVolume(4.5f, 1) == 4.5f, "voice migration preserves explicitly muted and custom speech gains");
        check(VoiceAudio.UpgradeVolume(1, 2) == 1 && VoiceAudio.UpgradeVolume(3.5f, 3) == 3.5f, "operators can pin a quieter speech preset with the current revision marker");
        check(VoiceAudio.UpgradeFullVolumeRange(2, 1) == 12 && VoiceAudio.UpgradeFullVolumeRange(4, 1) == 4 && VoiceAudio.UpgradeFullVolumeRange(2, 2) == 2, "voice distance migration upgrades stock falloff without changing custom or pinned distances");
#if NATIVE_CONFIG_TESTS
        var config = new Config { Volume = 3.5f, FullVolumeRange = 2, AudioBalanceRevision = 1 };
        config.UpgradeAudio();
        check(config.Volume == 10 && config.FullVolumeRange == 12 && config.AudioBalanceRevision == 3, "existing voice YAML receives the stronger speech preset and advances its migration revision");
        config.Volume = 1;
        config.UpgradeAudio();
        check(config.Volume == 1, "a deliberate volume edit after migration survives later plugin starts");
#endif
        check(VoiceAudio.UpgradeFullVolumeRange(6, 2) == 12 && VoiceAudio.UpgradeFullVolumeRange(6, 3) == 6, "the wider audible area upgrades stock revision-2 distances while preserving explicitly pinned falloff");
        var state = new VoiceState();
        void Input(bool down)
        {
            if (state.KeyPressed(down))
                state.Select(!state.Local);
        }

        check(!state.Local, "SCP voice begins on ordinary team chat");
        Input(true);
        Input(true);
        check(state.Local, "one key press selects local voice without repeat-report toggling");
        Input(false);
        check(state.Local, "toggle mode keeps local voice after key release");
        Input(true);
        check(!state.Local, "the next press returns to team chat");
        Input(false);
        Input(true);
        Input(false);
        check(state.Local, "tap and release keeps nearby chat enabled without a hold mode");
        state.Select(true);
        Input(false);
        check(state.Local, "a console-selected route survives release reports");
        Input(true);
        state.ResetInput();
        check(!state.Local, "a role or round reset cannot leave local voice active");
        check(state.KeyPressed(true) && !state.Local, "a press requests a toggle without committing a route before speaker allocation");
        check(!state.KeyPressed(true) && !state.Local, "failed allocation does not retry or claim enabled on duplicate press reports");
        state.KeyPressed(false);
        check(state.KeyPressed(true), "a new press can retry after an allocation failure");
        state.ResetInput();
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
