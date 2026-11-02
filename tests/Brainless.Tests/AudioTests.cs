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
        check(items.UpgradeAudio(0) == 1 && items.UpgradeAudio(1) == 1, "the item audio revision marker advances without the removed Echo Canister section");
        var weapons = new WeaponTuning();
        check(weapons.UpgradeAudio(0) == 1 && weapons.UpgradeAudio(1) == 1, "the weapon audio revision marker advances without a removed Rattler section");
        weapons.Audio.Volume = 10;
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

        check(Valid(), "operators can amplify general weapon cues up to tenfold");
        weapons.Audio.Volume = 10.1f;
        check(!Valid(), "invalid audio gain is rejected before applying weapon settings");
        weapons.Audio = null;
        check(!Valid(), "a missing shared audio section is rejected before weapon sounds can access it");
    }
}
