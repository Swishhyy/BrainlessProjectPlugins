namespace Brainless.Internal
{
    [TuningSection]
    public sealed class EffectAudioTuning
    {
        [Tune(0, 10)]
        public float Volume { get; set; } = AudioRules.EffectVolume;

        [Tune(.1, 100)]
        public float FullVolumeRange { get; set; } = AudioRules.FullVolumeRange;

        [Tune(.1, 100)]
        public float Range { get; set; } = AudioRules.EffectRange;
    }
}
