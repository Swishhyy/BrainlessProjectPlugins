namespace Brainless.ScpProximityChat
{
    internal static class VoiceAudio
    {
        // Speaker playback is separate from normal human voice. Keep its gain
        // local to this plugin so raising speech never changes item/weapon cues.
        internal const float DefaultVolume = 10;
        internal const float MaximumVolume = 20;
        internal const float DefaultFullVolumeRange = 12;
        internal const int Revision = 3;
        internal static float UpgradeVolume(float value, int revision) => revision < 2 && (value == 1 || value == 3.5f) ? DefaultVolume : value;
        internal static float UpgradeFullVolumeRange(float value, int revision) => revision < Revision && (value == 6 || (revision < 2 && value == 2)) ? DefaultFullVolumeRange : value;
    }
}
