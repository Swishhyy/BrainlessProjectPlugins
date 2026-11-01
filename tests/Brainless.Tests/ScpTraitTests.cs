using System;
using Brainless.Roles;

internal static class ScpTraitTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(Math.Abs(ScpTraitRules.Vigor(.4f, .5f, 1.5f) - .55f) < .0001f && Math.Abs(ScpTraitRules.Vigor(.4f, .5f, .5f) - .45f) < .0001f, "Wormhole and Hyper scale only positive vigor gains");
        check(ScpTraitRules.Vigor(.8f, .3f, 4) == .3f && ScpTraitRules.Vigor(.9f, 1, 2) == 1, "vigor costs stay native and gains cannot exceed the native cap");
        float total = 0;
        for (int tick = 1; tick <= 40; tick++)
            total += ScpTraitRules.PoisonDamage(tick * .2f, (tick - 1) * .2f, 5, 4);
        check(Math.Abs(total - 20) < .001f, "Poisonous deals exactly its configured 20 damage across five seconds without an extra expiry tick");
        check(Math.Abs(ScpTraitRules.PoisonDamage(10, 4.9f, 5, 4) - .4f) < .001f && ScpTraitRules.PoisonDamage(10, 6, 5, 4) == 0, "a delayed update cannot overrun a poison's remaining lifetime or refund health");
#if NATIVE_CONFIG_TESTS
        var config = new Config(); config.Prepare();
        check(config.Roles.Count == 53, "all fourteen previously missing SCP variants are present alongside the original profiles");
        check(config.RoleHudEdgePaddingPercent == .5f && config.RoleHudLayoutRevision == 2, "a new HUD starts next to the display edge");
        config.RoleHudEdgePaddingPercent = 3; config.RoleHudLayoutRevision = 1; config.Prepare();
        check(config.RoleHudEdgePaddingPercent == .5f, "old side-HUD YAML receives the closer edge inset");
        config.RoleHudEdgePaddingPercent = 1.25f; config.RoleHudLayoutRevision = 1; config.Prepare();
        check(config.RoleHudEdgePaddingPercent == 1.25f, "a deliberate custom HUD inset survives migration");
        var legacy = new Config(); legacy.Roles.RemoveAll(r => r.BaseRole == PlayerRoles.RoleTypeId.Scp106 || r.BaseRole == PlayerRoles.RoleTypeId.Scp173 || r.BaseRole == PlayerRoles.RoleTypeId.Scp939 || r.BaseRole == PlayerRoles.RoleTypeId.Scp096);
        legacy.Prepare();
        check(legacy.Roles.Count == 53 && legacy.RoleCatalogRevision == 2, "an existing YAML role list gains missing SCP profiles in one migration");
        legacy.Roles.RemoveAll(r => r.Id == "L01"); legacy.Prepare();
        check(!legacy.Roles.Exists(r => r.Id == "L01"), "deliberate profile deletion after migration is retained");
        var custom = new Config(); custom.Roles.RemoveAll(r => r.BaseRole == PlayerRoles.RoleTypeId.Scp106);
        custom.Roles.Add(new RoleDefinition { Id = "Q02", Name = "Custom 106", BaseRole = PlayerRoles.RoleTypeId.Scp106, ChancePercent = 90 });
        custom.Prepare();
        check(custom.Roles.Find(r => r.Id == "Q02").ChancePercent == 90 && custom.Roles.TrueForAll(r => r.BaseRole != PlayerRoles.RoleTypeId.Scp106 || r.Id == "Q02" || r.ChancePercent == 0), "adding new variants cannot overfill or redistribute an operator's existing SCP chance pool");
        check(config.Roles.Find(r => r.Id == "P02").Scale > 1 && config.Roles.Find(r => r.Id == "Z01").Scale > 1 && config.Roles.Find(r => r.Id == "Z02").Scale < 1, "Brute, Giant and Muncher now carry their advertised model sizes");
#endif
    }
}
