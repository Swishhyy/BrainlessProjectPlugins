using System.Collections.Generic;
using System.ComponentModel;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;
using PlayerRoles;

namespace Brainless.ScpSwap
{
    public enum CustomRoleMode
    {
        FollowScp,
        Reroll,
        None
    }

    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();

        [Description("Seconds after round start during which SCPs may swap with .scpswap. 0 disables player swaps (staff can still use bpscpswap).")]
        public float SwapWindowSeconds { get; set; } = 60;

        [Description("Seconds the other SCP has to answer a swap request.")]
        public float RequestTimeoutSeconds { get; set; } = 20;

        [Description("Swaps each player may make per round. 0 = unlimited.")]
        public int MaxSwapsPerPlayer { get; set; } = 1;

        [Description("Allow switching to an SCP nobody is playing (no request needed).")]
        public bool AllowUnplayedScps { get; set; } = true;

        [Description("SCPs that can be swapped to or from. SCP-049-2 is never swappable.")]
        public List<RoleTypeId> SwappableScps { get; set; } = new List<RoleTypeId>
        {
            RoleTypeId.Scp049,
            RoleTypeId.Scp079,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173,
            RoleTypeId.Scp939,
        };

        [Description("Brainless Roles SCP variants after a swap. FollowScp: the variant stays with its SCP, so you take over whatever variant (e.g. Boom Boom) was on it; an unplayed SCP gets a normal variant roll. Reroll: everyone who swaps gets a fresh normal variant roll. None: swapped SCPs stay plain.")]
        public CustomRoleMode CustomRoleMode { get; set; } = CustomRoleMode.FollowScp;

        [Description("Broadcast to SCPs at round start explaining .scpswap. Empty disables it.")]
        public string RoundStartBroadcast { get; set; } = "<size=30>You can swap SCPs for {seconds}s: open the console (~) and type <b>.scpswap list</b></size>";
        public ushort BroadcastSeconds { get; set; } = 8;
    }
}
