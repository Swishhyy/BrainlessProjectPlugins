using System;
using Brainless.Internal;
using Brainless.Items;
using Brainless.Weapons;

internal static class AudioTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(AudioRules.EffectVolume > 3 * .65f && AudioRules.VoiceVolume > 3 && AudioRules.FullVolumeRange == 6, "custom sound and voice presets raise gain and keep nearby listeners inside a wider full-volume area");
        check(AudioRules.FullVolumeDistance(6, 3) == 3 && AudioRules.FullVolumeDistance(6, 20) == 6, "short custom hearing ranges cannot put the full-volume distance beyond the speaker range");
        check(AudioRules.FullVolumeDistance(float.NaN, 16) == 6 && AudioRules.FullVolumeDistance(float.PositiveInfinity, 16) == 6 && AudioRules.FullVolumeDistance(-1, 16) == .1f, "invalid full-volume distances have finite native-safe bounds");
        var items = new ItemTuning();
        items.EchoCanister.AudioVolume = .65f;
        items.EchoCanister.AudioRange = 9;
        check(items.UpgradeAudio(0) == 1 && items.EchoCanister.AudioVolume == 2.5f && items.EchoCanister.AudioRange == 9, "old stock echo volume upgrades without changing a custom hearing radius");
        items.EchoCanister.AudioVolume = 0;
        items.UpgradeAudio(0);
        check(items.EchoCanister.AudioVolume == 0, "an explicitly muted effect stays muted during preset migration");
        items.EchoCanister.AudioVolume = .65f;
        items.UpgradeAudio(1);
        check(items.EchoCanister.AudioVolume == .65f, "the audio revision marker preserves a deliberately retained old preset");
        var weapons = new WeaponTuning();
        weapons.Rattler.AudioVolume = .65f;
        check(weapons.UpgradeAudio(0) == 1 && weapons.Rattler.AudioVolume == 2.5f && weapons.Rattler.AudioFullVolumeRange == 6, "old Rattler YAML gains a stronger tag and a wider full-volume area");
        weapons.Rattler.AudioVolume = .4f;
        weapons.UpgradeAudio(0);
        check(weapons.Rattler.AudioVolume == .4f, "a custom Rattler gain is not overwritten by the stock audio upgrade");
        weapons.Audio.Volume = 10;
        weapons.Rattler.AudioVolume = 10;
        bool Valid()
        {
            try
            {
                Tuning.Validate(weapons, "weapons");
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        check(Valid(), "operators can independently amplify general weapon cues and Rattler audio up to tenfold");
        weapons.Audio.Volume = 10.1f;
        check(!Valid(), "invalid audio gain is rejected before applying weapon settings");
        weapons.Audio = null;
        check(!Valid(), "a missing shared audio section is rejected before weapon sounds can access it");
    }
}
