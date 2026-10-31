namespace Brainless.ScpProximityChat
{
    internal static class VoiceAudio
    {
        // Speaker playback is separate from normal human voice. Keep its gain
        // local to this plugin so raising speech never changes item/weapon cues.
        internal const float DefaultVolume = 10;
        internal const int Revision = 2;
        internal static float UpgradeVolume(float value, int revision) => revision < Revision && (value == 1 || value == 3.5f) ? DefaultVolume : value;
        internal static float UpgradeFullVolumeRange(float value, int revision) => revision < Revision && value == 2 ? 6 : value;
    }
}
