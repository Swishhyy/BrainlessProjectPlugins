using System.Collections.Generic;
using System.ComponentModel;
using Brainless.Internal;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;
using PlayerRoles;
using UnityEngine;

namespace Brainless.ScpProxChat
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();

        [Description("IDs base through base+3 are reserved for this plugin's native Server Specific Settings. Change if another plugin uses them.")]
        public int SettingsIdBase { get; set; } = 58000;
        public KeyCode SuggestedKey { get; set; } = KeyCode.N;
        public bool ShowHints { get; set; } = true;
        public float HintDurationSeconds { get; set; } = 2;
        public string EnabledHint { get; set; } = "Proximity chat enabled";
        public string DisabledHint { get; set; } = "Proximity chat disabled";

        [Description("Maximum nearby voice radius in metres (5-30). Native receiver visibility checks still apply.")]
        public float VoiceRange { get; set; } = 16;

        [Description("Distance in metres before nearby voice starts fading. Clamped to voice_range.")]
        public float FullVolumeRange { get; set; } = VoiceAudio.DefaultFullVolumeRange;

        [Description("Native speaker gain (0-20). Default 10 amplifies nearby speech. bpscpchat volume changes it live; 0 mutes it.")]
        public float Volume { get; set; } = VoiceAudio.DefaultVolume;

        [Description("Stock preset migration: revision 2 raises old gains; revision 3 extends full-volume speech to 12m. Set 3 to pin old distances.")]
        public int AudioBalanceRevision { get; set; }

        internal void UpgradeAudio()
        {
            Volume = VoiceAudio.UpgradeVolume(Volume, AudioBalanceRevision);
            FullVolumeRange = VoiceAudio.UpgradeFullVolumeRange(FullVolumeRange, AudioBalanceRevision);
            AudioBalanceRevision = System.Math.Max(VoiceAudio.Revision, AudioBalanceRevision);
        }

        [Description("SCP-939 talking in nearby mode is heard from its mimic point while one is placed, instead of from its body.")]
        public bool Scp939VoiceFromMimicPoint { get; set; } = true;

        [Description("Show the SCP chat mode (team / nearby / mimic point) on the Brainless Roles HUD, under the role. Needs the Roles DLL.")]
        public bool ShowHudStatus { get; set; } = true;

        [Description("Only these living roles can use the alternate nearby voice channel. SCP-079 and SCP-3114 keep their native voice mechanics.")]
        public List<RoleTypeId> AllowedRoles { get; set; } = new List<RoleTypeId>
        {
            RoleTypeId.Scp049,
            RoleTypeId.Scp0492,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173,
            RoleTypeId.Scp939,
        };
    }
}
